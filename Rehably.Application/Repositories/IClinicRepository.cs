using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Application.Repositories;

/// <summary>
/// Repository interface for Clinic entity with specialized queries
/// </summary>
public interface IClinicRepository : IRepository<Clinic>
{
    /// <summary>
    /// Gets a clinic by its subdomain (slug)
    /// </summary>
    Task<Clinic?> GetBySubdomainAsync(string subdomain);

    /// <summary>
    /// Gets a clinic with its subscription details
    /// </summary>
    Task<Clinic?> GetWithSubscriptionAsync(Guid clinicId);

    /// <summary>
    /// Gets all active clinics
    /// </summary>
    Task<IEnumerable<Clinic>> GetActiveClinicsAsync();

    /// <summary>
    /// Gets clinics by status
    /// </summary>
    Task<IEnumerable<Clinic>> GetByStatusAsync(ClinicStatus status);

    /// <summary>
    /// Checks if a subdomain is available
    /// </summary>
    Task<bool> IsSubdomainAvailableAsync(string subdomain, Guid? excludeClinicId = null);

    /// <summary>
    /// Gets clinic count by subscription plan
    /// </summary>
    Task<int> CountBySubscriptionAsync(Guid subscriptionId);

    /// <summary>
    /// Gets clinics with expiring subscriptions
    /// </summary>
    Task<IEnumerable<Clinic>> GetExpiringSubscriptionsAsync(int daysThreshold);

    /// <summary>
    /// Gets clinic names by IDs for lookup
    /// </summary>
    Task<Dictionary<Guid, string>> GetClinicNamesByIdsAsync(IEnumerable<Guid> clinicIds);

    /// <summary>
    /// Gets clinic with subscription and package details for activity tracking
    /// </summary>
    Task<Clinic?> GetWithSubscriptionAndPackageAsync(Guid clinicId);

    /// <summary>
    /// Gets clinics with filtering, pagination and includes
    /// </summary>
    Task<(List<Clinic> Clinics, int TotalCount)> SearchAsync(
        string? search = null,
        ClinicStatus? status = null,
        SubscriptionStatus? subscriptionStatus = null,
        Guid? packageId = null,
        bool includeDeleted = false,
        string? sortBy = null,
        bool sortDesc = true,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Gets pending approval clinics with onboarding info
    /// </summary>
    Task<List<Clinic>> GetPendingApprovalAsync();

    /// <summary>
    /// Gets clinic with active subscription
    /// </summary>
    Task<Clinic?> GetWithActiveSubscriptionAsync(Guid clinicId);

    /// <summary>
    /// Gets clinic with its uploaded documents for onboarding review
    /// </summary>
    Task<Clinic?> GetWithDocumentsAsync(Guid clinicId, CancellationToken cancellationToken = default);
}
