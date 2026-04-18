using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Payment;

public record CreatePaymentRequestDto
{
    public Guid ClinicId { get; init; }
    public Guid SubscriptionPlanId { get; init; }
    public BillingCycle BillingCycle { get; init; } = BillingCycle.Monthly;
    public string ReturnUrl { get; init; } = string.Empty;
    public string CancelUrl { get; init; } = string.Empty;
    public string? ProviderKey { get; init; }
}
