using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Admin;
using Rehably.Application.Services.Admin;
using Rehably.Domain.Constants;
using Rehably.Domain.Entities.Identity;

namespace Rehably.Infrastructure.Services.Admin;

public class PlatformRoleService : IPlatformRoleService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public PlatformRoleService(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<Result<List<PlatformRoleResponse>>> GetAllRolesAsync()
    {
        try
        {
            var roles = await _roleManager.Roles
                .Where(r => r.TenantId == null)
                .ToListAsync();

            var response = new List<PlatformRoleResponse>();

            foreach (var role in roles)
            {
                var claims = await _roleManager.GetClaimsAsync(role);
                var permissions = claims
                    .Where(c => c.Type == "Permission")
                    .Select(c => c.Value ?? string.Empty)
                    .ToList();

                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name ?? string.Empty);
                var roleUsers = usersInRole.Where(u => u.IsActive).Select(u => new RoleUserDto
                {
                    Id = u.Id,
                    Name = $"{u.FirstName} {u.LastName}".Trim(),
                    Email = u.Email ?? string.Empty
                }).ToList();

                response.Add(new PlatformRoleResponse
                {
                    Id = role.Id,
                    Name = role.Name ?? string.Empty,
                    Description = role.Description,
                    Permissions = permissions,
                    UserCount = roleUsers.Count,
                    Users = roleUsers,
                    CreatedAt = role.CreatedAt
                });
            }

            return Result<List<PlatformRoleResponse>>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<List<PlatformRoleResponse>>.Failure($"Failed to retrieve roles: {ex.Message}");
        }
    }

    public async Task<Result<PlatformRoleResponse>> GetRoleByIdAsync(string roleId)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return Result<PlatformRoleResponse>.Failure($"Role with ID '{roleId}' not found");
            }

            if (role.TenantId != null)
            {
                return Result<PlatformRoleResponse>.Failure("This is not a platform-level role");
            }

            var claims = await _roleManager.GetClaimsAsync(role);
            var permissions = claims
                .Where(c => c.Type == "Permission")
                .Select(c => c.Value ?? string.Empty)
                .ToList();

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
                Permissions = permissions,
                UserCount = users.Count,
                Users = users,
                CreatedAt = role.CreatedAt
            };

            return Result<PlatformRoleResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<PlatformRoleResponse>.Failure($"Failed to retrieve role: {ex.Message}");
        }
    }

    public Task<Result<PlatformPermissionMatrixResponse>> GetAvailablePermissionsAsync()
    {
        try
        {
            var response = new PlatformPermissionMatrixResponse
            {
                Resources = PlatformPermissions.Resources.Select(r => new PermissionResourceDto
                {
                    Resource = r.Key,
                    NameEn = r.NameEn,
                    NameAr = r.NameAr,
                    Actions = r.Actions.Select(a => new PermissionActionDto
                    {
                        Action = a.Key,
                        Permission = $"{r.Key}.{a.Key}",
                        NameEn = a.NameEn,
                        NameAr = a.NameAr
                    }).ToList()
                }).ToList()
            };

            return Task.FromResult(Result<PlatformPermissionMatrixResponse>.Success(response));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<PlatformPermissionMatrixResponse>.Failure($"Failed to get permissions: {ex.Message}"));
        }
    }
}
