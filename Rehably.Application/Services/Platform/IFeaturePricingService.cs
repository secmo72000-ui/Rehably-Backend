using Rehably.Application.Common;
using Rehably.Application.DTOs.Feature;

namespace Rehably.Application.Services.Platform;

public interface IFeaturePricingService
{
    Task<Result<FeaturePricingDto>> GetCurrentPricingAsync(Guid featureId);
    Task<Result<FeaturePricingDto>> SetFeaturePricingAsync(Guid featureId, SetFeaturePricingRequestDto request);
    Task<Result<List<FeaturePricingDto>>> GetPricingHistoryAsync(Guid featureId);
    Task<Result> CanDeactivateFeatureAsync(Guid id);
}
