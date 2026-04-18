namespace Rehably.Application.DTOs.Payment;

public record PaymentTestRequest(
    decimal? Amount = null,
    Guid? ClinicId = null,
    Guid? SubscriptionPlanId = null,
    string? ReturnUrl = null,
    string? CancelUrl = null
);
