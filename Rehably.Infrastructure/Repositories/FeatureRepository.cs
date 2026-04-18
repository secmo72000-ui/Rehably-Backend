using Microsoft.EntityFrameworkCore;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Repositories;

/// <summary>
/// Implementation of IFeatureRepository
/// </summary>
public class FeatureRepository : Repository<Feature>, IFeatureRepository
{
    public FeatureRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Feature?> GetByCodeAsync(string code)
    {
        return await _dbSet
            .FirstOrDefaultAsync(f => f.Code == code);
    }

    public async Task<IEnumerable<Feature>> GetActiveFeaturesAsync()
    {
        return await _dbSet
            .Where(f => f.IsActive && !f.IsDeleted)
            .OrderBy(f => f.DisplayOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<Feature>> GetByCategoryAsync(Guid categoryId)
    {
        return await _dbSet
            .Where(f => f.CategoryId == categoryId && f.IsActive && !f.IsDeleted)
            .OrderBy(f => f.DisplayOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<Feature>> GetAddOnFeaturesAsync()
    {
        return await _dbSet
            .Where(f => f.IsAddOn && f.IsActive && !f.IsDeleted)
            .OrderBy(f => f.DisplayOrder)
            .ToListAsync();
    }

    public async Task<Feature?> GetWithPricingAsync(Guid featureId)
    {
        return await _dbSet
            .Include(f => f.PricingHistory)
            .FirstOrDefaultAsync(f => f.Id == featureId);
    }

    public async Task<bool> IsCodeUniqueAsync(string code, Guid? excludeFeatureId = null)
    {
        var query = _dbSet.Where(f => f.Code == code);

        if (excludeFeatureId.HasValue)
        {
            query = query.Where(f => f.Id != excludeFeatureId.Value);
        }

        return !await query.AnyAsync();
    }

    public async Task<IEnumerable<Feature>> GetFeaturesWithCurrentPricingAsync()
    {
        return await _dbSet
            .Include(f => f.PricingHistory)
            .Where(f => f.IsActive && !f.IsDeleted)
            .OrderBy(f => f.DisplayOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<Feature>> GetAddOnFeaturesWithPricingAsync()
    {
        return await _dbSet
            .Include(f => f.PricingHistory)
            .Where(f => f.IsAddOn && f.IsActive && !f.IsDeleted)
            .OrderBy(f => f.DisplayOrder)
            .ToListAsync();
    }

    public async Task<Feature?> GetAddOnWithPricingAsync(Guid featureId)
    {
        return await _dbSet
            .Include(f => f.PricingHistory)
            .FirstOrDefaultAsync(f => f.Id == featureId && f.IsAddOn && f.IsActive && !f.IsDeleted);
    }

    public async Task<bool> ExistsAsync(Guid featureId)
    {
        return await _dbSet.AnyAsync(f => f.Id == featureId && !f.IsDeleted);
    }

    public async Task<IEnumerable<Guid>> GetActiveCategoryIdsAsync()
    {
        return await _dbSet
            .Where(f => f.IsActive && !f.IsDeleted)
            .Select(f => f.CategoryId)
            .Distinct()
            .ToListAsync();
    }
}
