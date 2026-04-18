using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;
using Rehably.Domain.Enums;

namespace Rehably.Application.Services.Library;

/// <summary>
/// Facade interface for clinic-specific library operations.
/// This interface provides backward compatibility by aggregating all clinic library operations.
/// For new code, prefer using the specific service interfaces:
/// - IClinicLibraryQueryService for query operations
/// - IClinicLibraryOverrideService for override management
/// </summary>
public interface IClinicLibraryService
{
    #region Clinic Library Items (Combined View) - Delegates to IClinicLibraryQueryService

    /// <summary>
    /// Gets treatments available to a clinic (global + clinic-specific, with overrides applied).
    /// </summary>
    Task<Result<LibraryItemListResponse<TreatmentDto>>> GetClinicTreatmentsAsync(Guid clinicId, Guid? bodyRegionId, string? search, int page, int pageSize);

    /// <summary>
    /// Gets exercises available to a clinic (global + clinic-specific, with overrides applied).
    /// </summary>
    Task<Result<LibraryItemListResponse<ExerciseDto>>> GetClinicExercisesAsync(Guid clinicId, Guid? bodyRegionId, string? search, int page, int pageSize);

    /// <summary>
    /// Gets modalities available to a clinic (global + clinic-specific, with overrides applied).
    /// </summary>
    Task<Result<LibraryItemListResponse<ModalityDto>>> GetClinicModalitiesAsync(Guid clinicId, Guid? bodyRegionId, string? search, int page, int pageSize);

    /// <summary>
    /// Gets assessments available to a clinic (global + clinic-specific, with overrides applied).
    /// </summary>
    Task<Result<LibraryItemListResponse<AssessmentDto>>> GetClinicAssessmentsAsync(Guid clinicId, Guid? bodyRegionId, string? search, int page, int pageSize);

    /// <summary>
    /// Gets devices available to a clinic (global + clinic-specific, with overrides applied).
    /// </summary>
    Task<Result<LibraryItemListResponse<DeviceDto>>> GetClinicDevicesAsync(Guid clinicId, Guid? bodyRegionId, string? search, int page, int pageSize);

    #endregion

    #region Clinic Library Overrides - Delegates to IClinicLibraryOverrideService

    /// <summary>
    /// Creates an override for a global library item (hide, rename, or modify).
    /// </summary>
    Task<Result<ClinicLibraryOverrideDto>> CreateOverrideAsync(Guid clinicId, CreateClinicLibraryOverrideRequest request);

    /// <summary>
    /// Updates an existing clinic library override.
    /// </summary>
    Task<Result<ClinicLibraryOverrideDto>> UpdateOverrideAsync(Guid clinicId, Guid overrideId, UpdateClinicLibraryOverrideRequest request);

    /// <summary>
    /// Removes an override, restoring the original global library item behavior.
    /// </summary>
    Task<Result> RemoveOverrideAsync(Guid clinicId, Guid overrideId);

    /// <summary>
    /// Gets all overrides for a clinic, optionally filtered by library type.
    /// </summary>
    Task<Result<List<ClinicLibraryOverrideDto>>> GetClinicOverridesAsync(Guid clinicId, LibraryType? type);

    /// <summary>
    /// Gets a specific override by ID.
    /// </summary>
    Task<Result<ClinicLibraryOverrideDto>> GetOverrideByIdAsync(Guid clinicId, Guid overrideId);

    #endregion

}
