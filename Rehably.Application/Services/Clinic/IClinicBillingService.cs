using Rehably.Application.Common;

namespace Rehably.Application.Services.Clinic;

/// <summary>
/// Service interface for clinic billing and status operations.
/// </summary>
public interface IClinicBillingService
{
    /// <summary>
    /// Upgrades a clinic's subscription plan.
    /// </summary>
    Task<Result> UpgradeSubscriptionAsync(Guid clinicId, Guid newPlanId);

    /// <summary>
    /// Suspends a clinic.
    /// </summary>
    Task<Result> SuspendClinicAsync(Guid clinicId);

    /// <summary>
    /// Activates a clinic.
    /// </summary>
    Task<Result> ActivateClinicAsync(Guid clinicId);

    /// <summary>
    /// Bans a clinic with reason.
    /// </summary>
    Task<Result> BanClinicAsync(Guid clinicId, string reason, string adminUserId);

    /// <summary>
    /// Unbans a clinic.
    /// </summary>
    Task<Result> UnbanClinicAsync(Guid clinicId, string? reason, string adminUserId);
}
