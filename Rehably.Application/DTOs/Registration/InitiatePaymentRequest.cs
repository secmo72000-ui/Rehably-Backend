namespace Rehably.Application.DTOs.Registration;

public record InitiatePaymentRequest(
    Guid SubscriptionPlanId,
    string? ProviderKey = null,
    string? ReturnUrl = null,
    string? CancelUrl = null
);
