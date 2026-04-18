using Rehably.Application.DTOs.Platform;

namespace Rehably.Application.DTOs.Subscription;

public record PurchaseAddOnResponseDto
{
    public SubscriptionAddOnDto AddOn { get; init; } = null!;
    public PaymentInfoDto Payment { get; init; } = null!;
}
