using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;

namespace Rehably.Application.Services.Library;

public interface ITreatmentService
{
    Task<Result<LibraryItemListResponse<TreatmentDto>>> GetTreatmentsAsync(Guid? bodyRegionId, string? search, int page, int pageSize);
    Task<Result<TreatmentDto>> GetTreatmentByIdAsync(Guid id);
    Task<Result<TreatmentDto>> CreateTreatmentAsync(CreateTreatmentRequest request, Guid? clinicId);
    Task<Result<TreatmentDto>> UpdateTreatmentAsync(Guid id, UpdateTreatmentRequest request, Guid clinicId);
    Task<Result> DeleteTreatmentAsync(Guid id, Guid clinicId);
}
