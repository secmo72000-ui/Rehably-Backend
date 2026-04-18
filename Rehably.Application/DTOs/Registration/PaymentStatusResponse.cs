namespace Rehably.Application.DTOs.Registration;

public record PaymentStatusResponse
{
    public Guid OnboardingId { get; init; }
    public Guid ClinicId { get; init; }
    public string CurrentStep { get; init; } = string.Empty;
    public string PaymentStatus { get; init; } = string.Empty;
    public DateTime? PaymentCompletedAt { get; init; }
    public bool IsActivated { get; init; }
}
