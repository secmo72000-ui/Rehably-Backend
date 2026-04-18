namespace Rehably.Application.DTOs.Registration;

public record OnboardingResponse
{
    public Guid OnboardingId { get; init; }
    public Guid ClinicId { get; init; }
    public string CurrentStep { get; init; } = string.Empty;
    public string? Message { get; init; }
    public DateTime? EmailVerifiedAt { get; init; }
    public DateTime? DocumentsUploadedAt { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public DateTime? PaymentCompletedAt { get; init; }
}
