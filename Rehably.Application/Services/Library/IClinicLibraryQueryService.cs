using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;

namespace Rehably.Application.Services.Library;

public interface IClinicLibraryQueryService
{
    Task<Result<LibraryItemListResponse<TreatmentDto>>> GetClinicTreatmentsAsync(Guid clinicId, Guid? bodyRegionId, string? search, int page, int pageSize);

    Task<Result<LibraryItemListResponse<ExerciseDto>>> GetClinicExercisesAsync(Guid clinicId, Guid? bodyRegionId, string? search, int page, int pageSize);

    Task<Result<LibraryItemListResponse<ModalityDto>>> GetClinicModalitiesAsync(Guid clinicId, Guid? bodyRegionId, string? search, int page, int pageSize);

    Task<Result<LibraryItemListResponse<AssessmentDto>>> GetClinicAssessmentsAsync(Guid clinicId, Guid? bodyRegionId, string? search, int page, int pageSize);

    Task<Result<LibraryItemListResponse<DeviceDto>>> GetClinicDevicesAsync(Guid clinicId, Guid? bodyRegionId, string? search, int page, int pageSize);
}
