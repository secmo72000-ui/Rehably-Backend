using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Admin;
using Rehably.Application.Services.Admin;
using Rehably.Domain.Constants;
using Rehably.Domain.Entities.Identity;
using System.Security.Claims;

namespace Rehably.Infrastructure.Services.Admin;

public class PlatformRoleManagementService : IPlatformRoleManagementService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDistributedCache _cache;
    private readonly ILogger<PlatformRoleManagementService> _logger;

    public PlatformRoleManagementService(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IDistributedCache cache,
        ILogger<PlatformRoleManagementService> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<PlatformRoleResponse>> CreateRoleAsync(CreatePlatformRoleRequest request, string currentUserId)
    {
        try
        {
            foreach (var permission in request.Permissions)
            {
                if (!PlatformPermissions.IsValidPermission(permission))
                {
                    return Result<PlatformRoleResponse>.Failure($"Invalid permission: '{permission}'");
                }
            }

            var currentUserPermissions = await GetUserPermissionsAsync(currentUserId);
            foreach (var permission in request.Permissions)
            {
                if (!HasPermissionWithWildcard(currentUserPermissions, permission))
                {
                    return Result<PlatformRoleResponse>.Failure(
                        $"Cannot grant permission '{permission}' that you don't have");
                }
            }

            var existingRole = await _roleManager.FindByNameAsync(request.Name);
            if (existingRole != null)
            {
                return Result<PlatformRoleResponse>.Failure($"Role with name '{request.Name}' already exists");
            }

            var role = new ApplicationRole
            {
                Name = request.Name,
                NormalizedName = request.Name.ToUpperInvariant(),
                Description = request.Description,
                TenantId = null,
                IsCustom = true
            };

            var createResult = await _roleManager.CreateAsync(role);
            if (!createResult.Succeeded)
            {
                return Result<PlatformRoleResponse>.Failure(
                    $"Failed to create role: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }

            foreach (var permission in request.Permissions)
            {
                await _roleManager.AddClaimAsync(role, new Claim("Permission", permission));
            }

            role = await _roleManager.FindByNameAsync(request.Name);

            var response = new PlatformRoleResponse
            {
                Id = role!.Id,
                Name = role.Name ?? string.Empty,
                Description = role.Description,
                Permissions = request.Permissions,
                UserCount = 0,
                Users = new List<RoleUserDto>(),
                CreatedAt = role.CreatedAt
            };

            _logger.LogInformation("Role '{RoleName}' created by user {UserId} with {PermissionCount} permissions",
                role.Name, currentUserId, request.Permissions.Count);

            return Result<PlatformRoleResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<PlatformRoleResponse>.Failure($"Failed to create role: {ex.Message}");
        }
    }

    public async Task<Result<PlatformRoleResponse>> UpdateRoleAsync(string roleId, UpdatePlatformRoleRequest request, string currentUserId)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return Result<PlatformRoleResponse>.Failure($"Role with ID '{roleId}' not found");
            }

            if (!role.IsCustom)
            {
                return Result<PlatformRoleResponse>.Failure("Cannot modify built-in roles");
            }

            foreach (var permission in request.Permissions)
            {
                if (!PlatformPermissions.IsValidPermission(permission))
                {
                    return Result<PlatformRoleResponse>.Failure($"Invalid permission: '{permission}'");
                }
            }

            var currentUserPermissions = await GetUserPermissionsAsync(currentUserId);
            foreach (var permission in request.Permissions)
            {
                if (!HasPermissionWithWildcard(currentUserPermissions, permission))
                {
                    return Result<PlatformRoleResponse>.Failure(
                        $"Cannot grant permission '{permission}' that you don't have");
                }
            }

            if (request.Description != null)
            {
                role.Description = request.Description;
                await _roleManager.UpdateAsync(role);
            }

            var existingClaims = await _roleManager.GetClaimsAsync(role);
            var permissionClaims = existingClaims.Where(c => c.Type == "Permission").ToList();

            foreach (var claim in permissionClaims)
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }

            foreach (var permission in request.Permissions)
            {
                await _roleManager.AddClaimAsync(role, new Claim("Permission", permission));
            }

            await InvalidateCacheForRoleUsersAsync(roleId);

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name ?? string.Empty);
            var users = usersInRole.Where(u => u.IsActive).Select(u => new RoleUserDto
            {
                Id = u.Id,
                Name = $"{u.FirstName} {u.LastName}".Trim(),
                Email = u.Email ?? string.Empty
            }).ToList();

            var response = new PlatformRoleResponse
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                Description = role.Description,
                Permissions = request.Permissions,
                UserCount = users.Count,
                Users = users,
                CreatedAt = role.CreatedAt
            };

            _logger.LogInformation("Role '{RoleName}' updated by user {UserId}", role.Name, currentUserId);

            return Result<PlatformRoleResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<PlatformRoleResponse>.Failure($"Failed to update role: {ex.Message}");
        }
    }

    public async Task<Result> DeleteRoleAsync(string roleId)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return Result.Failure($"Role with ID '{roleId}' not found");
            }

            if (!role.IsCustom)
            {
                return Result.Failure("Cannot delete built-in roles");
            }

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name ?? string.Empty);
            if (usersInRole.Any())
            {
                return Result.Failure($"Cannot delete role '{role.Name}' because {usersInRole.Count} user(s) are still assigned to it");
            }

            var deleteResult = await _roleManager.DeleteAsync(role);
            if (!deleteResult.Succeeded)
            {
                return Result.Failure(
                    $"Failed to delete role: {string.Join(", ", deleteResult.Errors.Select(e => e.Description))}");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete role: {ex.Message}");
        }
    }

    private async Task<List<string>> GetUserPermissionsAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return new List<string>();

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = new HashSet<string>();

        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var claims = await _roleManager.GetClaimsAsync(role);
                foreach (var claim in claims.Where(c => c.Type == "Permission"))
                {
                    permissions.Add(claim.Value);
                }
            }
        }

        return permissions.ToList();
    }

    private static bool HasPermissionWithWildcard(IEnumerable<string> userPermissions, string targetPermission)
    {
        var permList = userPermissions.ToList();

        if (permList.Contains("*.*") || permList.Contains("*"))
            return true;

        if (permList.Contains(targetPermission))
            return true;

        var parts = targetPermission.Split('.');
        if (parts.Length == 2)
        {
            var resourceWildcard = $"{parts[0]}.*";
            if (permList.Contains(resourceWildcard))
                return true;
        }

        return false;
    }

    private async Task InvalidateCacheForRoleUsersAsync(string roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return;

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name ?? string.Empty);

        foreach (var user in usersInRole)
        {
            await _cache.RemoveAsync($"PlatformAdminPermissions:{user.Id}");
        }

        _logger.LogInformation("Cleared permission cache for {Count} users in role {RoleId}",
            usersInRole.Count, roleId);
    }
}
