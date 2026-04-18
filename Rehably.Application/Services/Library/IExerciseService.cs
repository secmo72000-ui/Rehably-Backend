using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;

namespace Rehably.Application.Services.Library;

public interface IExerciseService
{
    Task<Result<LibraryItemListResponse<ExerciseDto>>> GetExercisesAsync(Guid? bodyRegionId, string? search, int page, int pageSize);
    Task<Result<ExerciseDto>> GetExerciseByIdAsync(Guid id);
    Task<Result<ExerciseDto>> CreateExerciseAsync(CreateExerciseRequest request, Guid? clinicId);
    Task<Result<ExerciseDto>> UpdateExerciseAsync(Guid id, UpdateExerciseRequest request, Guid clinicId);
    Task<Result> DeleteExerciseAsync(Guid id, Guid clinicId);
}
