using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;

namespace Rehably.Application.Repositories;

/// <summary>
/// Repository interface for Subscription entity with specialized queries
/// </summary>
public interface ISubscriptionRepository : IRepository<Subscription>
{
    /// <summary>
    /// Gets the active subscription for a clinic
    /// </summary>
    Task<Subscription?> GetActiveSubscriptionByClinicIdAsync(Guid clinicId);

    /// <summary>
    /// Gets the active subscription for a clinic with full details (package, features, usage)
    /// </summary>
    Task<Subscription?> GetActiveSubscriptionWithDetailsByClinicIdAsync(Guid clinicId);

    /// <summary>
    /// Gets subscription with package details
    /// </summary>
    Task<Subscription?> GetWithPackageAsync(Guid subscriptionId);

    /// <summary>
    /// Gets subscription with all related data (package, features, usage)
    /// </summary>
    Task<Subscription?> GetFullSubscriptionDetailsAsync(Guid subscriptionId);

    /// <summary>
    /// Gets subscriptions by status
    /// </summary>
    Task<IEnumerable<Subscription>> GetByStatusAsync(SubscriptionStatus status);

    /// <summary>
    /// Gets subscriptions expiring within a specified number of days
    /// </summary>
    Task<IEnumerable<Subscription>> GetExpiringAsync(int daysThreshold);

    /// <summary>
    /// Gets subscriptions that are overdue for payment
    /// </summary>
    Task<IEnumerable<Subscription>> GetOverdueAsync();

    /// <summary>
    /// Gets subscription with feature usage for current billing period
    /// </summary>
    Task<Subscription?> GetWithCurrentUsageAsync(Guid subscriptionId);

    /// <summary>
    /// Gets all subscriptions for a clinic (including historical)
    /// </summary>
    Task<IEnumerable<Subscription>> GetByClinicIdAsync(Guid clinicId);

    /// <summary>
    /// Gets subscriptions that have specific add-on
    /// </summary>
    Task<IEnumerable<Subscription>> GetWithAddOnAsync(Guid featureId);

    /// <summary>
    /// Counts active subscriptions for a package
    /// </summary>
    Task<int> CountActiveByPackageAsync(Guid packageId);

    /// <summary>
    /// Gets subscriptions due for renewal invoice generation (active, auto-renew, ending soon, no existing invoice)
    /// </summary>
    Task<IEnumerable<Subscription>> GetDueForInvoiceGenerationAsync(int daysThreshold = 7);

    /// <summary>
    /// Gets subscription with package and clinic for DTO projection
    /// </summary>
    Task<Subscription?> GetWithPackageAndClinicAsync(Guid subscriptionId);

    /// <summary>
    /// Gets subscription with feature usage and features
    /// </summary>
    Task<Subscription?> GetWithFeatureUsageAsync(Guid subscriptionId);

    /// <summary>
    /// Gets active subscription for a clinic (for validation)
    /// </summary>
    Task<Subscription?> GetActiveByClinicIdAsync(Guid clinicId);

    /// <summary>
    /// Gets subscription with package and features for upgrade
    /// </summary>
    Task<Subscription?> GetForUpgradeAsync(Guid subscriptionId);

    /// <summary>
    /// Gets subscription with package, features, and feature usage for modification
    /// </summary>
    Task<Subscription?> GetForModificationAsync(Guid subscriptionId);

    /// <summary>
    /// Gets all subscriptions with package and clinic for listing
    /// </summary>
    Task<IEnumerable<Subscription>> GetAllWithPackageAndClinicAsync(Guid? clinicId = null);

    /// <summary>
    /// Gets paged subscriptions with package and clinic for listing
    /// </summary>
    Task<(IEnumerable<Subscription> Subscriptions, int TotalCount)> GetPagedWithPackageAndClinicAsync(
        int page, int pageSize, SubscriptionStatus? status = null, Guid? clinicId = null);

    Task<IEnumerable<Subscription>> GetActiveSubscriptionsAsync();
}
