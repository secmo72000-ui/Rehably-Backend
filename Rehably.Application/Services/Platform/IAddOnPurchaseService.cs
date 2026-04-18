using Rehably.Application.Common;
using Rehably.Application.DTOs.Subscription;

namespace Rehably.Application.Services.Platform;

public interface IAddOnPurchaseService
{
    Task<Result<PurchaseAddOnResponseDto>> PurchaseAddOnAsync(Guid clinicId, PurchaseAddOnRequestDto request);
    Task<Result<SubscriptionAddOnDto>> UpgradeAddOnAsync(Guid clinicId, Guid addOnId, int newQuantity);
    Task<Result> CancelAddOnAsync(Guid clinicId, Guid addOnId);
}
