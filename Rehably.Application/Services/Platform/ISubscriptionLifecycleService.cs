using Rehably.Application.Common;
using Rehably.Application.DTOs.Subscription;
using Rehably.Domain.Enums;

namespace Rehably.Application.Services.Platform;

public interface ISubscriptionLifecycleService
{
    Task<Result<SubscriptionDetailDto>> CreateSubscriptionAsync(CreateSubscriptionRequestDto request);
    Task<Result<SubscriptionDetailDto>> CancelSubscriptionAsync(Guid id, CancelSubscriptionRequestDto request);
    Task<Result<SubscriptionDetailDto>> RenewSubscriptionAsync(Guid id, RenewSubscriptionRequestDto request);
    Task<Result> CheckSubscriptionStatusAsync(Guid clinicId);
    Task<Result> ResetUsageAsync(Guid subscriptionId, Guid featureId);
    Task<Result> RenewSubscriptionForCycleAsync(Guid subscriptionId);
    Task<Result> SuspendClinicAsync(Guid clinicId, string? reason);
    Task<Result> ReactivateClinicAsync(Guid clinicId, PaymentType paymentType);
    Task<Result> ConvertTrialAsync(Guid subscriptionId, PaymentType paymentType);
}
