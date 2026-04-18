using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Subscription;

public record ReactivateSubscriptionRequestDto
{
    public PaymentType PaymentType { get; init; } = PaymentType.Online;
    public string? ReturnUrl { get; init; }
}
