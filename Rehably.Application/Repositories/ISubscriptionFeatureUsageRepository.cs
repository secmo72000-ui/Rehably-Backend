using Rehably.Domain.Entities.Platform;

namespace Rehably.Application.Repositories;

/// <summary>
/// Repository interface for SubscriptionFeatureUsage entity operations.
/// </summary>
public interface ISubscriptionFeatureUsageRepository : IRepository<SubscriptionFeatureUsage>
{
    /// <summary>
    /// Gets all feature usage records for a specific subscription.
    /// </summary>
    Task<IEnumerable<SubscriptionFeatureUsage>> GetBySubscriptionIdAsync(Guid subscriptionId);

    /// <summary>
    /// Gets a specific feature usage record by subscription and feature.
    /// </summary>
    Task<SubscriptionFeatureUsage?> GetBySubscriptionAndFeatureAsync(Guid subscriptionId, Guid featureId);

    /// <summary>
    /// Gets feature usage with feature details for a subscription.
    /// </summary>
    Task<IEnumerable<SubscriptionFeatureUsage>> GetWithFeatureBySubscriptionIdAsync(Guid subscriptionId);

    /// <summary>
    /// Gets feature usage by subscription and feature code.
    /// </summary>
    Task<SubscriptionFeatureUsage?> GetBySubscriptionAndFeatureCodeAsync(Guid subscriptionId, string featureCode);
}
