using Microsoft.AspNetCore.Authorization;
using Rehably.Application.Services.Auth;
using Rehably.Application.Services.Platform;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

namespace Rehably.API.Authorization;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPlanPermissionService _planPermissionService;
    private readonly IPermissionLookupService _permissionLookupService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionHandler> _logger;

    public PermissionHandler(
        IPlanPermissionService planPermissionService,
        IPermissionLookupService permissionLookupService,
        IMemoryCache cache,
        ILogger<PermissionHandler> logger)
    {
        _planPermissionService = planPermissionService;
        _permissionLookupService = permissionLookupService;
        _cache = cache;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("PermissionHandler: No NameIdentifier claim found");
            return;
        }

        var jwtPermissions = context.User.FindAll("Permission")
            .Select(c => c.Value).ToHashSet();

        if (HasPermissionWithWildcard(jwtPermissions, requirement.Permission))
        {
            _logger.LogInformation("PermissionHandler: Permission {Permission} granted to user {UserId} via JWT claims",
                requirement.Permission, userId);
            context.Succeed(requirement);
            return;
        }

        var tenantIdClaim = context.User.FindFirst("TenantId")?.Value;
        var rolesClaim = context.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

        _logger.LogInformation("PermissionHandler: UserId: {UserId}, TenantIdClaim: {TenantIdClaim}, RolesFromClaims: [{Roles}], RequiredPermission: {Permission}",
            userId, tenantIdClaim ?? "null", string.Join(",", rolesClaim), requirement.Permission);

        var isPlatformAdmin = rolesClaim.Contains("PlatformAdmin");

        if (string.IsNullOrEmpty(tenantIdClaim))
        {
            if (!isPlatformAdmin)
            {
                _logger.LogWarning("PermissionHandler: No TenantId claim found for non-admin user {UserId}", userId);
                return;
            }

            var cacheKey = $"PlatformAdminPermissions:{userId}";
            if (!_cache.TryGetValue(cacheKey, out HashSet<string>? platformPermissions))
            {
                platformPermissions = await _permissionLookupService.GetPermissionsForRolesAsync(rolesClaim);
                _cache.Set(cacheKey, platformPermissions, TimeSpan.FromMinutes(30));
            }

            if (platformPermissions != null && HasPermissionWithWildcard(platformPermissions, requirement.Permission))
            {
                _logger.LogInformation("PermissionHandler: Permission {Permission} granted to platform admin {UserId} via DB lookup",
                    requirement.Permission, userId);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("PermissionHandler: Platform admin {UserId} does not have permission {Permission}",
                    userId, requirement.Permission);
            }
            return;
        }

        if (!Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            _logger.LogWarning("PermissionHandler: Invalid TenantId format for user {UserId}", userId);
            return;
        }

        var tenantCacheKey = $"UserPermissions:{tenantIdClaim}:{userId}";
        HashSet<string>? userPermissions = null;

        if (!_cache.TryGetValue(tenantCacheKey, out HashSet<string>? cachedPermissions))
        {
            userPermissions = await _permissionLookupService.GetPermissionsForRolesAsync(rolesClaim);
            _cache.Set(tenantCacheKey, userPermissions, TimeSpan.FromMinutes(30));
        }
        else
        {
            userPermissions = cachedPermissions;
        }

        if (userPermissions == null || !userPermissions.Contains(requirement.Permission))
        {
            _logger.LogWarning("PermissionHandler: User {UserId} does not have permission {Permission} in role claims",
                userId, requirement.Permission);
            return;
        }

        var planAllowsPermission = await _planPermissionService.CanClinicUsePermissionAsync(tenantId, requirement.Permission);
        if (!planAllowsPermission)
        {
            _logger.LogWarning("PermissionHandler: Clinic {ClinicId}'s subscription plan does not allow permission {Permission}",
                tenantId, requirement.Permission);
            return;
        }

        _logger.LogInformation("PermissionHandler: Permission {Permission} granted to user {UserId} in clinic {ClinicId}",
            requirement.Permission, userId, tenantId);
        context.Succeed(requirement);
    }

    private bool HasPermissionWithWildcard(HashSet<string> permissions, string required)
    {
        if (permissions.Contains(required)) return true;

        if (permissions.Contains("*") || permissions.Contains("*.*")) return true;

        var parts = required.Split('.');
        if (parts.Length >= 1)
        {
            if (permissions.Contains($"{parts[0]}.*")) return true;
        }

        return false;
    }
}
