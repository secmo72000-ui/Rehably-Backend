using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Rehably.Application.Contexts;
using Rehably.Application.Services.Platform;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;

namespace Rehably.Infrastructure.Services.Auth;

public class PlanPermissionService : IPlanPermissionService
{
    private readonly IClinicRepository _clinicRepository;
    private readonly IPackageRepository _packageRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PlanPermissionService> _logger;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public PlanPermissionService(
        IClinicRepository clinicRepository,
        IPackageRepository packageRepository,
        ITenantContext tenantContext,
        IMemoryCache cache,
        ILogger<PlanPermissionService> logger)
    {
        _clinicRepository = clinicRepository;
        _packageRepository = packageRepository;
        _tenantContext = tenantContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> CanClinicUsePermissionAsync(Guid clinicId, string permission, CancellationToken cancellationToken = default)
    {
        var availablePermissions = await GetAvailablePermissionsAsync(clinicId, cancellationToken);

        foreach (var pattern in availablePermissions)
        {
            if (PermissionMatchesPattern(permission, pattern))
            {
                _logger.LogDebug("Permission {Permission} matched pattern {Pattern} for clinic {ClinicId}", permission, pattern, clinicId);
                return true;
            }
        }

        _logger.LogWarning("Permission {Permission} not allowed for clinic {ClinicId}", permission, clinicId);
        return false;
    }

    public async Task<string[]> GetAvailablePermissionsAsync(Guid clinicId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"PlanPermissions:{clinicId}";

        if (_cache.TryGetValue(cacheKey, out string[]? cachedPermissions))
        {
            return cachedPermissions ?? Array.Empty<string>();
        }

        var clinic = await _clinicRepository.GetWithSubscriptionAndPackageAsync(clinicId);

        if (clinic?.CurrentSubscription?.Package == null)
        {
            _logger.LogWarning("Clinic {ClinicId} or its subscription package not found", clinicId);
            return Array.Empty<string>();
        }

        // Clinic has an active subscription — grant access to all clinic-level permissions.
        // Fine-grained per-package permission scoping can be added here when package
        // permission fields are defined. For now "*.*" unlocks the full permission catalogue.
        var permissions = new[] { "*.*" };

        _cache.Set(cacheKey, permissions, CacheDuration);
        _logger.LogInformation("Cached {Count} permissions for clinic {ClinicId}", permissions.Length, clinicId);

        return permissions;
    }

    public async Task<bool> DoesPlanAllowPermissionAsync(Guid packageId, string permission, CancellationToken cancellationToken = default)
    {
        var package = await _packageRepository.GetByIdAsync(packageId);
        if (package == null)
        {
            return false;
        }

        var permissions = ParsePermissionsFromPackage(package);

        foreach (var pattern in permissions)
        {
            if (PermissionMatchesPattern(permission, pattern))
            {
                return true;
            }
        }

        return false;
    }

    public bool PermissionMatchesPattern(string permission, string pattern)
    {
        if (pattern == "*.*" || pattern == "*")
        {
            return true;
        }

        var permissionParts = permission.Split('.');
        var patternParts = pattern.Split('.');

        if (patternParts.Length != 2)
        {
            return permission.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }

        var resourceMatch = patternParts[0] == "*" || permissionParts[0].Equals(patternParts[0], StringComparison.OrdinalIgnoreCase);
        var actionMatch = patternParts[1] == "*" || permissionParts[1].Equals(patternParts[1], StringComparison.OrdinalIgnoreCase);

        return resourceMatch && actionMatch;
    }

    public Task InvalidatePlanPermissionsCacheAsync(Guid clinicId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"PlanPermissions:{clinicId}";
        _cache.Remove(cacheKey);
        _logger.LogInformation("Invalidated plan permissions cache for clinic {ClinicId}", clinicId);
        return Task.CompletedTask;
    }

    private static string[] ParsePermissionsFromPackage(Package package)
    {
        return Array.Empty<string>();
    }
}
