using Rehably.Application.Common;
using Rehably.Application.DTOs.Subscription;
using Rehably.Application.DTOs.Usage;

namespace Rehably.Application.Services.Platform;

public interface IUsageService
{
    Task<Result<bool>> CanUseFeatureAsync(Guid tenantId, string featureCode);
    Task<Result<bool>> IncrementUsageAsync(Guid tenantId, string featureCode, int amount = 1);
    Task<Result<SubscriptionFeatureUsageDto>> GetUsageAsync(Guid tenantId, string featureCode);
    Task<Result<List<SubscriptionFeatureUsageDto>>> GetAllUsageAsync(Guid tenantId);
    Task<Result> ResetFeatureUsageAsync(Guid subscriptionId, Guid featureId);
    Task<Result> ResetAllUsageAsync(Guid subscriptionId);
    Task<Result<Dictionary<string, UsageStatsDto>>> GetUsageStatsAsync(Guid tenantId);
}
