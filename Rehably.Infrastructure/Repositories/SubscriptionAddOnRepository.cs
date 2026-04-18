using Microsoft.EntityFrameworkCore;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for SubscriptionAddOn entity
/// </summary>
public class SubscriptionAddOnRepository : Repository<SubscriptionAddOn>, ISubscriptionAddOnRepository
{
    public SubscriptionAddOnRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<SubscriptionAddOn>> GetActiveBySubscriptionIdAsync(Guid subscriptionId)
    {
        return await _dbSet
            .Include(sa => sa.Feature)
            .Where(sa => sa.SubscriptionId == subscriptionId && sa.Status == AddOnStatus.Active)
            .OrderBy(sa => sa.Feature.Name)
            .ToListAsync();
    }

    public async Task<SubscriptionAddOn?> GetBySubscriptionAndFeatureAsync(Guid subscriptionId, Guid featureId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(sa => sa.SubscriptionId == subscriptionId
                && sa.FeatureId == featureId
                && sa.Status == AddOnStatus.Active);
    }

    public async Task<SubscriptionAddOn?> GetWithFeatureAsync(Guid addOnId)
    {
        return await _dbSet
            .Include(sa => sa.Feature)
                .ThenInclude(f => f.PricingHistory)
            .FirstOrDefaultAsync(sa => sa.Id == addOnId);
    }

    public async Task<SubscriptionAddOn?> GetWithSubscriptionAndFeatureAsync(Guid addOnId)
    {
        return await _dbSet
            .Include(sa => sa.Subscription)
            .Include(sa => sa.Feature)
                .ThenInclude(f => f.PricingHistory)
            .FirstOrDefaultAsync(sa => sa.Id == addOnId);
    }

    public async Task<List<Guid>> GetActiveAddOnFeatureIdsAsync(Guid subscriptionId)
    {
        return await _dbSet
            .Where(sa => sa.SubscriptionId == subscriptionId && sa.Status == AddOnStatus.Active)
            .Select(sa => sa.FeatureId)
            .ToListAsync();
    }

    public async Task<bool> ExistsActiveAddOnAsync(Guid subscriptionId, Guid featureId)
    {
        return await _dbSet
            .AnyAsync(sa => sa.SubscriptionId == subscriptionId
                && sa.FeatureId == featureId
                && sa.Status == AddOnStatus.Active);
    }

    public async Task<List<SubscriptionAddOn>> GetBySubscriptionIdAsync(Guid subscriptionId, AddOnStatus? status = null)
    {
        var query = _dbSet
            .Include(sa => sa.Feature)
            .Include(sa => sa.Subscription)
            .Where(sa => sa.SubscriptionId == subscriptionId);

        if (status.HasValue)
            query = query.Where(sa => sa.Status == status.Value);

        return await query.OrderBy(sa => sa.CreatedAt).ToListAsync();
    }
}
