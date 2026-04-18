using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinic;

namespace Rehably.Application.Services.Clinic;

/// <summary>
/// Facade interface for clinic operations.
/// This interface provides backward compatibility by aggregating all clinic operations.
/// For new code, prefer using the specific service interfaces:
/// - IClinicCrudService for CRUD operations
/// - IClinicQueryService for query operations
/// - IClinicBillingService for billing/status operations
/// </summary>
public interface IClinicService
{
    // CRUD operations - delegates to IClinicCrudService
    Task<Result<RegisterClinicResponse>> RegisterClinicAsync(RegisterClinicRequest request);
    Task<Result<ClinicResponse>> CreateClinicAsync(CreateClinicRequest request);
    Task<Result<ClinicResponse>> GetClinicByIdAsync(Guid id);
    Task<Result<ClinicResponse>> UpdateClinicAsync(Guid id, UpdateClinicRequest request);
    Task<Result> DeleteClinicAsync(Guid id);
    Task<Result<ClinicResponse>> GetMyClinicAsync();
    Task<Result<ClinicResponse>> UpdateMyClinicAsync(UpdateClinicRequest request);

    // Query operations - delegates to IClinicQueryService
    Task<Result<PagedResult<ClinicResponse>>> GetAllClinicsAsync(int page = 1, int pageSize = 20);
    Task<Result<PagedResult<ClinicResponse>>> SearchClinicsAsync(GetClinicsQuery query);
    Task<Result<List<ClinicResponse>>> GetPendingClinicsAsync();
    Task<bool> CanAddPatientAsync(Guid clinicId);
    Task<bool> CanAddUserAsync(Guid clinicId);
    Task<bool> CanUploadStorageAsync(Guid clinicId, long bytes);
    void ValidateTenantAccess(Guid requestedClinicId);

    // Billing operations - delegates to IClinicBillingService
    Task<Result> UpgradeSubscriptionAsync(Guid clinicId, Guid newPlanId);
    Task<Result> SuspendClinicAsync(Guid clinicId);
    Task<Result> ActivateClinicAsync(Guid clinicId);
    Task<Result> BanClinicAsync(Guid clinicId, string reason, string adminUserId);
    Task<Result> UnbanClinicAsync(Guid clinicId, string? reason, string adminUserId);
}
