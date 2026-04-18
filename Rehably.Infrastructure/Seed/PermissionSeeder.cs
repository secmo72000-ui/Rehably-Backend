using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Rehably.Domain.Constants;
using Rehably.Domain.Entities.Identity;
using System.Security.Claims;

namespace Rehably.Infrastructure.Seed;

public static class PermissionSeeder
{
    /// <summary>
    /// Seeds platform-level permissions for PlatformAdmin role.
    /// Uses permissions defined in PlatformPermissions class plus wildcards for full access.
    /// Adds any missing permissions without removing existing ones.
    /// </summary>
    public static async Task SeedAsync(RoleManager<ApplicationRole> roleManager, ILogger? logger = null)
    {
        var platformAdmin = await roleManager.FindByNameAsync("PlatformAdmin");
        if (platformAdmin == null)
        {
            logger?.LogWarning("PlatformAdmin role not found - cannot seed permissions");
            return;
        }

        var existingClaims = await roleManager.GetClaimsAsync(platformAdmin);
        var existingPermissions = existingClaims
            .Where(c => c.Type == "Permission")
            .Select(c => c.Value)
            .ToHashSet();

        var allPermissions = PlatformPermissions.GetAllPermissionStrings();

        var wildcards = PlatformPermissions.Resources
            .Select(r => $"{r.Key}.*")
            .ToList();
        wildcards.Add("*.*");

        var requiredPermissions = allPermissions.Concat(wildcards).Distinct().ToList();

        var missingPermissions = requiredPermissions
            .Where(p => !existingPermissions.Contains(p))
            .ToList();

        if (missingPermissions.Count == 0)
        {
            logger?.LogInformation("PlatformAdmin permissions already up to date ({Count} permissions)", existingPermissions.Count);
            return;
        }

        foreach (var permission in missingPermissions)
        {
            await roleManager.AddClaimAsync(platformAdmin, new Claim("Permission", permission));
        }

        logger?.LogInformation("Added {AddedCount} missing permissions for PlatformAdmin role (total: {TotalCount})",
            missingPermissions.Count, existingPermissions.Count + missingPermissions.Count);
    }
}
