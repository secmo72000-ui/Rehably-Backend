using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.Contexts;
using Rehably.Application.DTOs.Role;
using Rehably.Application.Services.Clinic;
using Rehably.Application.Services.Auth;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Entities.Identity;
using Rehably.Infrastructure.Data;
using PermissionDto = Rehably.Application.DTOs.Role.PermissionDto;
using System.Security.Claims;

namespace Rehably.Infrastructure.Services.Clinic;

public class RoleManagementService : IRoleManagementService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPlanPermissionService _planPermissionService;
    private readonly IPermissionService _permissionService;
    private readonly ITenantContext _tenantContext;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RoleManagementService> _logger;

    public RoleManagementService(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IPlanPermissionService planPermissionService,
        IPermissionService permissionService,
        ITenantContext tenantContext,
        ApplicationDbContext dbContext,
        IMemoryCache cache,
        ILogger<RoleManagementService> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _planPermissionService = planPermissionService;
        _permissionService = permissionService;
        _tenantContext = tenantContext;
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<List<RoleDto>>> GetRolesAsync(Guid? clinicId, CancellationToken cancellationToken = default)
    {
        var tenantId = clinicId ?? _tenantContext.TenantId;
        if (!tenantId.HasValue)
        {
            return Result<List<RoleDto>>.Success(new List<RoleDto>());
        }

        var roles = await _roleManager.Roles
            .Where(r => r.TenantId == tenantId.Value)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        var roleIds = roles.Select(r => r.Id).ToList();

        var allRoleClaims = await _dbContext.RoleClaims
            .Where(rc => roleIds.Contains(rc.RoleId))
            .ToListAsync(cancellationToken);

        var claimsByRoleId = allRoleClaims
            .GroupBy(rc => rc.RoleId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = roles.Select(role =>
        {
            var claims = claimsByRoleId.GetValueOrDefault(role.Id, []);
            var permissions = claims
                .Where(c => c.ClaimType == "Permission" && c.ClaimValue != null)
                .Select(c => ParsePermissionDto(c.ClaimValue!))
                .Where(p => p != null)
                .ToList()!;

            return new RoleDto
            {
                Name = role.Name ?? string.Empty,
                Description = role.Description,
                IsCustom = role.IsCustom,
                Permissions = permissions,
                CreatedAt = role.CreatedAt
            };
        }).ToList();

        return Result<List<RoleDto>>.Success(result);
    }

    public async Task<Result<RoleDto>> GetRoleAsync(Guid clinicId, string roleName, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null || role.TenantId != clinicId)
        {
            return Result<RoleDto>.Failure($"Role '{roleName}' not found");
        }

        return Result<RoleDto>.Success(await MapToRoleDtoAsync(role));
    }

    public async Task<Result<RoleDto>> CreateRoleAsync(Guid clinicId, CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var existingRole = await _roleManager.FindByNameAsync(request.Name);
        if (existingRole != null)
        {
            return Result<RoleDto>.Failure($"Role '{request.Name}' already exists");
        }

        var availablePermissions = await _planPermissionService.GetAvailablePermissionsAsync(clinicId, cancellationToken);

        var invalidPermissions = request.Permissions
            .Where(p => !availablePermissions.Contains(p, StringComparer.OrdinalIgnoreCase) &&
                        !availablePermissions.Any(ap => _planPermissionService.PermissionMatchesPattern(p, ap)))
            .ToList();

        if (invalidPermissions.Any())
        {
            return Result<RoleDto>.Failure($"Permissions not allowed by subscription plan: {string.Join(", ", invalidPermissions)}");
        }

        var role = new ApplicationRole
        {
            Name = request.Name,
            TenantId = clinicId,
            Description = request.Description,
            IsCustom = true
        };

        var identityResult = await _roleManager.CreateAsync(role);
        if (!identityResult.Succeeded)
        {
            return Result<RoleDto>.Failure(string.Join(", ", identityResult.Errors.Select(e => e.Description)));
        }

        foreach (var permission in request.Permissions)
        {
            await _roleManager.AddClaimAsync(role, new Claim("Permission", permission));
        }

        InvalidatePermissionCache();

        _logger.LogInformation("Created custom role {RoleName} for clinic {ClinicId} with {PermissionCount} permissions",
            request.Name, clinicId, request.Permissions.Count);

        return Result<RoleDto>.Success(await MapToRoleDtoAsync(role));
    }

    public async Task<Result<RoleDto>> UpdateRoleAsync(Guid clinicId, string roleName, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            return Result<RoleDto>.Failure($"Role '{roleName}' not found");
        }

        if (role.TenantId != clinicId)
        {
            return Result<RoleDto>.Failure("Role does not belong to this clinic");
        }

        if (!role.IsCustom)
        {
            return Result<RoleDto>.Failure("Cannot modify built-in roles");
        }

        if (request.Description != null)
        {
            role.Description = request.Description;
            await _roleManager.UpdateAsync(role);
        }

        if (request.Permissions != null)
        {
            var availablePermissions = await _planPermissionService.GetAvailablePermissionsAsync(clinicId, cancellationToken);

            var invalidPermissions = request.Permissions
                .Where(p => !availablePermissions.Contains(p, StringComparer.OrdinalIgnoreCase) &&
                            !availablePermissions.Any(ap => _planPermissionService.PermissionMatchesPattern(p, ap)))
                .ToList();

            if (invalidPermissions.Any())
            {
                return Result<RoleDto>.Failure($"Permissions not allowed by subscription plan: {string.Join(", ", invalidPermissions)}");
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

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name ?? string.Empty);
            foreach (var user in usersInRole)
            {
                await _permissionService.InvalidateUserPermissionsAsync(user.Id, clinicId.ToString());
            }

            InvalidatePermissionCache();

            _logger.LogInformation("Updated role {RoleName} for clinic {ClinicId} with {PermissionCount} permissions",
                roleName, clinicId, request.Permissions.Count);
        }

        return Result<RoleDto>.Success(await MapToRoleDtoAsync(role));
    }

    public async Task<Result> DeleteRoleAsync(Guid clinicId, string roleName, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            return Result.Failure($"Role '{roleName}' not found");
        }

        if (role.TenantId != clinicId)
        {
            return Result.Failure("Role does not belong to this clinic");
        }

        if (!role.IsCustom)
        {
            return Result.Failure("Cannot delete built-in roles");
        }

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name ?? string.Empty);
        if (usersInRole.Count > 0)
        {
            return Result.Failure($"Cannot delete role with {usersInRole.Count} assigned users. Reassign users first.");
        }

        var identityResult = await _roleManager.DeleteAsync(role);
        if (!identityResult.Succeeded)
        {
            return Result.Failure(string.Join(", ", identityResult.Errors.Select(e => e.Description)));
        }

        InvalidatePermissionCache();

        _logger.LogInformation("Deleted role {RoleName} from clinic {ClinicId}", roleName, clinicId);
        return Result.Success();
    }

    public async Task<Result<List<PermissionDto>>> GetAvailablePermissionsAsync(Guid clinicId, CancellationToken cancellationToken = default)
    {
        var allPermissions = await _permissionService.GetAllPermissionsAsync(cancellationToken);
        var allowedPatterns = await _planPermissionService.GetAvailablePermissionsAsync(clinicId, cancellationToken);

        var result = new List<PermissionDto>();
        foreach (var permission in allPermissions)
        {
            var permissionString = $"{permission.Resource}.{permission.Action}";
            var isAllowed = allowedPatterns.Any(pattern =>
                _planPermissionService.PermissionMatchesPattern(permissionString, pattern));

            if (isAllowed)
            {
                result.Add(new PermissionDto
                {
                    Name = permissionString,
                    Resource = permission.Resource,
                    Action = permission.Action
                });
            }
        }

        return Result<List<PermissionDto>>.Success(result);
    }

    public async Task<Result> AssignPermissionToRoleAsync(Guid clinicId, string roleName, string permission, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            return Result.Failure($"Role '{roleName}' not found");
        }

        if (role.TenantId != clinicId)
        {
            return Result.Failure("Role does not belong to this clinic");
        }

        var planAllows = await _planPermissionService.CanClinicUsePermissionAsync(clinicId, permission, cancellationToken);
        if (!planAllows)
        {
            return Result.Failure($"Permission '{permission}' is not allowed by subscription plan");
        }

        var existingClaims = await _roleManager.GetClaimsAsync(role);
        if (existingClaims.Any(c => c.Type == "Permission" && c.Value == permission))
        {
            return Result.Success();
        }

        await _roleManager.AddClaimAsync(role, new Claim("Permission", permission));

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name ?? string.Empty);
        foreach (var user in usersInRole)
        {
            await _permissionService.InvalidateUserPermissionsAsync(user.Id, clinicId.ToString());
        }

        InvalidatePermissionCache();

        _logger.LogInformation("Assigned permission {Permission} to role {RoleName} in clinic {ClinicId}", permission, roleName, clinicId);
        return Result.Success();
    }

    public async Task<Result> RemovePermissionFromRoleAsync(Guid clinicId, string roleName, string permission, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            return Result.Failure($"Role '{roleName}' not found");
        }

        if (role.TenantId != clinicId)
        {
            return Result.Failure("Role does not belong to this clinic");
        }

        var existingClaims = await _roleManager.GetClaimsAsync(role);
        var claimToRemove = existingClaims.FirstOrDefault(c => c.Type == "Permission" && c.Value == permission);

        if (claimToRemove == null)
        {
            return Result.Success();
        }

        await _roleManager.RemoveClaimAsync(role, claimToRemove);

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name ?? string.Empty);
        foreach (var user in usersInRole)
        {
            await _permissionService.InvalidateUserPermissionsAsync(user.Id, clinicId.ToString());
        }

        InvalidatePermissionCache();

        _logger.LogInformation("Removed permission {Permission} from role {RoleName} in clinic {ClinicId}", permission, roleName, clinicId);
        return Result.Success();
    }

    private void InvalidatePermissionCache()
    {
        _cache.Remove("PermissionCacheVersion");
    }

    private async Task<RoleDto> MapToRoleDtoAsync(ApplicationRole role)
    {
        var claims = await _roleManager.GetClaimsAsync(role);
        var permissions = claims
            .Where(c => c.Type == "Permission")
            .Select(c => ParsePermissionDto(c.Value))
            .Where(p => p != null)
            .ToList()!;

        return new RoleDto
        {
            Name = role.Name ?? string.Empty,
            Description = role.Description,
            IsCustom = role.IsCustom,
            Permissions = permissions,
            CreatedAt = role.CreatedAt
        };
    }

    private static PermissionDto? ParsePermissionDto(string permission)
    {
        var parts = permission.Split('.');
        if (parts.Length != 2)
        {
            return null;
        }

        return new PermissionDto
        {
            Name = permission,
            Resource = parts[0],
            Action = parts[1]
        };
    }
}
