using Rehably.Application.Common;
using Rehably.Application.DTOs.Feature;

namespace Rehably.Application.Services.Platform;

public interface IFeatureService
{
    Task<Result<FeatureDto>> GetFeatureByIdAsync(Guid id);
    Task<Result<List<FeatureDto>>> GetFeaturesAsync(Guid? categoryId = null);
    Task<Result<FeatureDetailDto>> GetFeatureWithDetailsAsync(Guid id);
    Task<Result<FeatureDto>> CreateFeatureAsync(CreateFeatureRequestDto request);
    Task<Result<FeatureDto>> UpdateFeatureAsync(Guid id, UpdateFeatureRequestDto request);
    Task<Result> DeactivateFeatureAsync(Guid id);
}
