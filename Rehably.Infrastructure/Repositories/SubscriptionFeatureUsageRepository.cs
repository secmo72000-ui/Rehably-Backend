using Microsoft.EntityFrameworkCore;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for SubscriptionFeatureUsage entity operations.
/// </summary>
public class SubscriptionFeatureUsageRepository : Repository<SubscriptionFeatureUsage>, ISubscriptionFeatureUsageRepository
{
    /// <summary>
    /// Initializes a new instance of the SubscriptionFeatureUsageRepository class.
    /// </summary>
    public SubscriptionFeatureUsageRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SubscriptionFeatureUsage>> GetBySubscriptionIdAsync(Guid subscriptionId)
    {
        return await _dbSet
            .Where(u => u.SubscriptionId == subscriptionId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<SubscriptionFeatureUsage?> GetBySubscriptionAndFeatureAsync(Guid subscriptionId, Guid featureId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.SubscriptionId == subscriptionId && u.FeatureId == featureId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SubscriptionFeatureUsage>> GetWithFeatureBySubscriptionIdAsync(Guid subscriptionId)
    {
        return await _dbSet
            .Include(u => u.Feature)
            .Where(u => u.SubscriptionId == subscriptionId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<SubscriptionFeatureUsage?> GetBySubscriptionAndFeatureCodeAsync(Guid subscriptionId, string featureCode)
    {
        return await _dbSet
            .Include(u => u.Feature)
            .FirstOrDefaultAsync(u => u.SubscriptionId == subscriptionId && u.Feature.Code == featureCode);
    }
}
