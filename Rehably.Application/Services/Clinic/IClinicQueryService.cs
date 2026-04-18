using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinic;

namespace Rehably.Application.Services.Clinic;

/// <summary>
/// Service interface for clinic query operations.
/// </summary>
public interface IClinicQueryService
{
    /// <summary>
    /// Gets all clinics with pagination.
    /// </summary>
    Task<Result<PagedResult<ClinicResponse>>> GetAllClinicsAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Searches clinics with filters and pagination.
    /// </summary>
    Task<Result<PagedResult<ClinicResponse>>> SearchClinicsAsync(GetClinicsQuery query);

    /// <summary>
    /// Gets clinics pending approval.
    /// </summary>
    Task<Result<List<ClinicResponse>>> GetPendingClinicsAsync();

    /// <summary>
    /// Checks if a clinic can add a new patient.
    /// </summary>
    Task<bool> CanAddPatientAsync(Guid clinicId);

    /// <summary>
    /// Checks if a clinic can add a new user.
    /// </summary>
    Task<bool> CanAddUserAsync(Guid clinicId);

    /// <summary>
    /// Checks if a clinic can upload storage.
    /// </summary>
    Task<bool> CanUploadStorageAsync(Guid clinicId, long bytes);

    /// <summary>
    /// Validates tenant access for cross-tenant protection.
    /// </summary>
    void ValidateTenantAccess(Guid requestedClinicId);
}
