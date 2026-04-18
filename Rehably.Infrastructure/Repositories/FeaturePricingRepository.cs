using Microsoft.EntityFrameworkCore;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Repositories;

public class FeaturePricingRepository : Repository<FeaturePricing>, IFeaturePricingRepository
{
    public FeaturePricingRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<FeaturePricing?> GetCurrentPricingAsync(Guid featureId)
    {
        return await _dbSet
            .Where(fp => fp.FeatureId == featureId)
            .OrderByDescending(fp => fp.EffectiveDate)
            .FirstOrDefaultAsync(fp => !fp.ExpiryDate.HasValue || fp.ExpiryDate >= DateTime.UtcNow);
    }

    public async Task<FeaturePricing?> GetPricingAtDateAsync(Guid featureId, DateTime effectiveDate)
    {
        return await _dbSet
            .Where(fp => fp.FeatureId == featureId && fp.EffectiveDate <= effectiveDate && (!fp.ExpiryDate.HasValue || fp.ExpiryDate >= effectiveDate))
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<FeaturePricing>> GetPricingHistoryAsync(Guid featureId)
    {
        return await _dbSet
            .Where(fp => fp.FeatureId == featureId)
            .OrderByDescending(fp => fp.EffectiveDate)
            .ToListAsync();
    }
}
