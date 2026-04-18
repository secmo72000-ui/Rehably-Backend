using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;

namespace Rehably.Application.Repositories;

/// <summary>
/// Repository interface for SubscriptionAddOn entity with specialized queries
/// </summary>
public interface ISubscriptionAddOnRepository : IRepository<SubscriptionAddOn>
{
    /// <summary>
    /// Gets active add-ons for a subscription
    /// </summary>
    Task<IEnumerable<SubscriptionAddOn>> GetActiveBySubscriptionIdAsync(Guid subscriptionId);

    /// <summary>
    /// Gets add-on by subscription and feature
    /// </summary>
    Task<SubscriptionAddOn?> GetBySubscriptionAndFeatureAsync(Guid subscriptionId, Guid featureId);

    /// <summary>
    /// Gets add-on with feature details
    /// </summary>
    Task<SubscriptionAddOn?> GetWithFeatureAsync(Guid addOnId);

    /// <summary>
    /// Gets add-on with subscription and feature details
    /// </summary>
    Task<SubscriptionAddOn?> GetWithSubscriptionAndFeatureAsync(Guid addOnId);

    /// <summary>
    /// Gets active add-on feature IDs for a subscription
    /// </summary>
    Task<List<Guid>> GetActiveAddOnFeatureIdsAsync(Guid subscriptionId);

    /// <summary>
    /// Checks if an add-on exists for a subscription and feature
    /// </summary>
    Task<bool> ExistsActiveAddOnAsync(Guid subscriptionId, Guid featureId);

    /// <summary>
    /// Gets all add-ons for a subscription with Feature included, optionally filtered by status.
    /// </summary>
    Task<List<SubscriptionAddOn>> GetBySubscriptionIdAsync(Guid subscriptionId, AddOnStatus? status = null);
}
