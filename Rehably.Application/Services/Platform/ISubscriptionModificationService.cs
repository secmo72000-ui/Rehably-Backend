using Rehably.Application.Common;
using Rehably.Application.DTOs.Subscription;

namespace Rehably.Application.Services.Platform;

public interface ISubscriptionModificationService
{
    Task<Result<SubscriptionDetailDto>> UpgradeSubscriptionAsync(Guid id, UpgradeSubscriptionRequestDto request);
    Task<Result<SubscriptionModificationResultDto>> ModifySubscriptionAsync(Guid id, ModifySubscriptionRequestDto request);
    Task<Result<SubscriptionModificationResultDto>> PreviewModificationAsync(Guid id, ModifySubscriptionRequestDto request);
}
