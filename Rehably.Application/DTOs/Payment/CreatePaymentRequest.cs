namespace Rehably.Application.DTOs.Payment;

public record CreatePaymentRequest(
    Guid ClinicId,
    Guid SubscriptionPlanId,
    string ReturnUrl,
    string CancelUrl,
    string? ProviderKey = null
);
