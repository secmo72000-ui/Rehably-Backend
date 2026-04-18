using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;

namespace Rehably.Application.Services.Library;

public interface ITreatmentStageService
{
    Task<Result<LibraryItemListResponse<TreatmentStageDto>>> GetStagesAsync(Guid? bodyRegionId, string? search, int page, int pageSize);
    Task<Result<LibraryItemListResponse<TreatmentStageDto>>> GetClinicStagesAsync(Guid clinicId, Guid? bodyRegionId, string? search, int page, int pageSize);
    Task<Result<TreatmentStageDto>> GetStageByIdAsync(Guid id);
    Task<Result<TreatmentStageDto>> CreateStageAsync(CreateTreatmentStageRequest request, Guid clinicId);
    Task<Result<TreatmentStageDto>> UpdateStageAsync(Guid id, UpdateTreatmentStageRequest request, Guid clinicId);
    Task<Result> DeleteStageAsync(Guid id, Guid clinicId);
}
