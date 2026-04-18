using Microsoft.EntityFrameworkCore;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Repositories;

/// <summary>
/// Implementation of IPackageRepository
/// </summary>
public class PackageRepository : Repository<Package>, IPackageRepository
{
    public PackageRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Package>> GetActivePackagesAsync()
    {
        return await _dbSet
            .Where(p => !p.IsDeleted && p.Status == PackageStatus.Active)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync();
    }

    public async Task<Package?> GetWithFeaturesAsync(Guid packageId)
    {
        return await _dbSet
            .Include(p => p.Features)
            .ThenInclude(pf => pf.Feature)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == packageId);
    }

    public async Task<Package?> GetByCodeAsync(string code)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.Code == code);
    }

    public async Task<IEnumerable<Package>> GetPublicPackagesAsync()
    {
        return await _dbSet
            .Include(p => p.Features)
                .ThenInclude(pf => pf.Feature)
                    .ThenInclude(f => f!.Category)
            .Where(p => p.IsPublic && !p.IsDeleted && p.Status == PackageStatus.Active)
            .OrderBy(p => p.DisplayOrder)
            .AsSplitQuery()
            .ToListAsync();
    }

    public async Task<IEnumerable<Package>> GetByStatusAsync(PackageStatus status)
    {
        return await _dbSet
            .Where(p => p.Status == status)
            .ToListAsync();
    }

    public async Task<bool> IsCodeUniqueAsync(string code, Guid? excludePackageId = null)
    {
        var query = _dbSet.Where(p => p.Code == code);

        if (excludePackageId.HasValue)
        {
            query = query.Where(p => p.Id != excludePackageId.Value);
        }

        return !await query.AnyAsync();
    }

    public async Task<Package?> GetWithPricingHistoryAsync(Guid packageId)
    {
        return await _dbSet
            .Include(p => p.Features)
            .FirstOrDefaultAsync(p => p.Id == packageId);
    }

    public async Task<List<Guid>> GetIncludedFeatureIdsAsync(Guid packageId)
    {
        return await _context.PackageFeatures
            .Where(pf => pf.PackageId == packageId && pf.IsIncluded)
            .Select(pf => pf.FeatureId)
            .ToListAsync();
    }

    public async Task<Package?> GetWithFeaturesAndPricingAsync(Guid packageId, bool activeOnly = false)
    {
        var query = _dbSet
            .Where(p => p.Id == packageId && !p.IsDeleted);

        if (activeOnly)
        {
            query = query.Where(p => p.Status == PackageStatus.Active);
        }

        return await query
            .Include(p => p.Features)
                .ThenInclude(pf => pf.Feature)
                    .ThenInclude(f => f!.PricingHistory)
            .AsSplitQuery()
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Package>> GetAllForAdminAsync()
    {
        return await _dbSet
            .Include(p => p.Features)
                .ThenInclude(pf => pf.Feature)
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.DisplayOrder)
            .AsSplitQuery()
            .ToListAsync();
    }

    public async Task<Package?> GetForEditAsync(Guid packageId)
    {
        return await _dbSet
            .Include(p => p.Features)
            .FirstOrDefaultAsync(p => p.Id == packageId && !p.IsDeleted);
    }

    public async Task ClearFeaturesAsync(Guid packageId)
    {
        var features = await _context.PackageFeatures
            .Where(pf => pf.PackageId == packageId)
            .ToListAsync();

        _context.PackageFeatures.RemoveRange(features);
    }

    public async Task<bool> HasAnySubscriptionsAsync(Guid packageId)
    {
        return await _context.Subscriptions
            .AnyAsync(s => s.PackageId == packageId);
    }
}

public class PackageFeatureRepository : Repository<PackageFeature>, IPackageFeatureRepository
{
    public PackageFeatureRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<bool> IsFeatureInActivePackagesAsync(Guid featureId)
    {
        return await _dbSet
            .Include(pf => pf.Package)
            .AnyAsync(pf => pf.FeatureId == featureId && pf.Package.Status == PackageStatus.Active);
    }
}
