using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;

namespace Rehably.Application.Services.Library;

public interface IAssessmentService
{
    Task<Result<LibraryItemListResponse<AssessmentDto>>> GetAssessmentsAsync(Guid? bodyRegionId, string? search, int page, int pageSize);
    Task<Result<AssessmentDto>> GetAssessmentByIdAsync(Guid id);
    Task<Result<AssessmentDto>> CreateAssessmentAsync(CreateAssessmentRequest request, Guid? clinicId);
    Task<Result<AssessmentDto>> UpdateAssessmentAsync(Guid id, UpdateAssessmentRequest request, Guid clinicId);
    Task<Result> DeleteAssessmentAsync(Guid id, Guid clinicId);
}
