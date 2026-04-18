namespace Rehably.Application.DTOs.Registration;

public record PaymentOptionsResponse
{
    public Guid OnboardingId { get; init; }
    public Guid ClinicId { get; init; }
    public List<SubscriptionPlanOption> Plans { get; init; } = new();
    public string DefaultReturnUrl { get; init; } = string.Empty;
    public string DefaultCancelUrl { get; init; } = string.Empty;
}
