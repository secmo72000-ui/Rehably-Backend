using Rehably.Application.Common;
using Rehably.Application.DTOs.Feature;

namespace Rehably.Application.Services.Platform;

public interface IFeatureCategoryService
{
    Task<Result<FeatureCategoryDto>> GetCategoryByIdAsync(Guid id);
    Task<Result<List<FeatureCategoryDto>>> GetCategoriesAsync();
    Task<Result<FeatureCategoryDetailDto>> GetCategoryWithDetailsAsync(Guid id);
    Task<Result<FeatureCategoryDto>> CreateCategoryAsync(CreateFeatureCategoryRequestDto request);
    Task<Result<FeatureCategoryDto>> UpdateCategoryAsync(Guid id, UpdateFeatureCategoryRequestDto request);
    Task<Result> DeactivateCategoryAsync(Guid id);
}
