using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Rehably.Application.DTOs.Permission;
using Rehably.Application.Services.Auth;
using Rehably.Domain.Entities.Identity;
using System.Security.Claims;

namespace Rehably.Infrastructure.Services.Auth;

public class PermissionService : IPermissionService
{
    private readonly IMemoryCache _cache;
    private readonly RoleManager<ApplicationRole> _roleManager;

    private static readonly PermissionDto[] AllPermissions = new PermissionDto[]
    {
        new() { Resource = "clinics", Action = "view" },
        new() { Resource = "clinics", Action = "create" },
        new() { Resource = "clinics", Action = "update" },
        new() { Resource = "clinics", Action = "delete" },
        new() { Resource = "patients", Action = "view" },
        new() { Resource = "patients", Action = "create" },
        new() { Resource = "patients", Action = "update" },
        new() { Resource = "patients", Action = "delete" },
        new() { Resource = "appointments", Action = "view" },
        new() { Resource = "appointments", Action = "create" },
        new() { Resource = "appointments", Action = "update" },
        new() { Resource = "appointments", Action = "delete" },
        new() { Resource = "platform", Action = "manage_features" },
        new() { Resource = "platform", Action = "manage_packages" },
        new() { Resource = "platform", Action = "manage_subscriptions" },
        new() { Resource = "platform", Action = "manage_feature_categories" },
        new() { Resource = "platform", Action = "view_usage_stats" },
        new() { Resource = "platform", Action = "manage_clinics" },
    };

    public PermissionService(IMemoryCache cache, RoleManager<ApplicationRole> roleManager)
    {
        _cache = cache;
        _roleManager = roleManager;
    }

    public Task InvalidateUserPermissionsAsync(string userId, string tenantId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"UserPermissions:{tenantId}:{userId}";
        _cache.Remove(cacheKey);
        return Task.CompletedTask;
    }

    public Task<PermissionDto[]> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AllPermissions);
    }

    public async Task<RolePermissionDto[]> GetRolePermissionsAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            return Array.Empty<RolePermissionDto>();
        }

        var claims = await _roleManager.GetClaimsAsync(role);
        return claims
            .Where(c => c.Type == "Permission")
            .Select(c => ParsePermission(c.Value))
            .Where(p => p != null)
            .Select(p => new RolePermissionDto
            {
                RoleName = roleName,
                Resource = p!.Resource,
                Action = p.Action
            })
            .ToArray()!;
    }

    public async Task AssignPermissionToRoleAsync(string roleName, string permission, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            throw new InvalidOperationException($"Role '{roleName}' not found.");
        }

        var existingClaims = await _roleManager.GetClaimsAsync(role);
        if (existingClaims.Any(c => c.Type == "Permission" && c.Value == permission))
        {
            return;
        }

        await _roleManager.AddClaimAsync(role, new Claim("Permission", permission));
    }

    public async Task RemovePermissionFromRoleAsync(string roleName, string permission, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            throw new InvalidOperationException($"Role '{roleName}' not found.");
        }

        var existingClaims = await _roleManager.GetClaimsAsync(role);
        var claimToRemove = existingClaims.FirstOrDefault(c => c.Type == "Permission" && c.Value == permission);

        if (claimToRemove != null)
        {
            await _roleManager.RemoveClaimAsync(role, claimToRemove);
        }
    }

    private static PermissionDto? ParsePermission(string permission)
    {
        var parts = permission.Split('.');
        if (parts.Length != 2)
        {
            return null;
        }

        return new PermissionDto
        {
            Resource = parts[0],
            Action = parts[1]
        };
    }
}
