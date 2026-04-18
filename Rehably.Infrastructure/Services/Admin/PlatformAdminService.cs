using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rehably.Application.DTOs.Admin;
using Rehably.Application.Services.Admin;
using Rehably.Application.Common;
using Rehably.Domain.Entities.Identity;

namespace Rehably.Infrastructure.Services.Admin;

public class PlatformAdminService : IPlatformAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public PlatformAdminService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<Result<PagedResult<PlatformAdminResponse>>> GetAllAdminsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _userManager.Users
                .Include(u => u.UserRoles)
                .Where(u => u.TenantId == null);

            var totalCount = await query.CountAsync();

            var adminUsers = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new List<PlatformAdminResponse>();

            foreach (var user in adminUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var roleName = roles.FirstOrDefault();
                var role = !string.IsNullOrEmpty(roleName)
                    ? await _roleManager.FindByNameAsync(roleName)
                    : null;

                var roleResponse = new PlatformRoleResponse();
                if (role != null)
                {
                    var claims = await _roleManager.GetClaimsAsync(role);
                    var permissions = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList();
                    var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name ?? string.Empty);

                    roleResponse = new PlatformRoleResponse
                    {
                        Id = role.Id,
                        Name = role.Name ?? string.Empty,
                        Description = role.Description,
                        Permissions = permissions,
                        UserCount = usersInRole.Count(u => u.IsActive),
                        CreatedAt = role.CreatedAt
                    };
                }

                response.Add(new PlatformAdminResponse
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    IsActive = user.IsActive,
                    Role = roleResponse,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                });
            }

            var pagedResult = new PagedResult<PlatformAdminResponse>(response, totalCount, page, pageSize);
            return Result<PagedResult<PlatformAdminResponse>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            return Result<PagedResult<PlatformAdminResponse>>.Failure($"Failed to retrieve admin users: {ex.Message}");
        }
    }

    public async Task<Result<PlatformAdminResponse>> GetAdminByIdAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<PlatformAdminResponse>.Failure($"Admin user with ID '{userId}' not found");
            }

            if (user.TenantId != null)
            {
                return Result<PlatformAdminResponse>.Failure("This is not a platform admin user");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault();
            var role = !string.IsNullOrEmpty(roleName)
                ? await _roleManager.FindByNameAsync(roleName)
                : null;

            var roleResponse = new PlatformRoleResponse();
            if (role != null)
            {
                var claims = await _roleManager.GetClaimsAsync(role);
                var permissions = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList();
                var userCount = await _userManager.GetUsersInRoleAsync(role.Name ?? string.Empty);

                roleResponse = new PlatformRoleResponse
                {
                    Id = role.Id,
                    Name = role.Name ?? string.Empty,
                    Description = role.Description,
                    Permissions = permissions,
                    UserCount = userCount.Count,
                    CreatedAt = role.CreatedAt
                };
            }

            var response = new PlatformAdminResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                IsActive = user.IsActive,
                Role = roleResponse,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            return Result<PlatformAdminResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<PlatformAdminResponse>.Failure($"Failed to retrieve admin user: {ex.Message}");
        }
    }
}
