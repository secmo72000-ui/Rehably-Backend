using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Platform;

public record PaymentDto
{
    public Guid Id { get; init; }
    public Guid ClinicId { get; init; }
    public Guid InvoiceId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "EGP";
    public string Provider { get; init; } = string.Empty;
    public string? ProviderTransactionId { get; init; }
    public PaymentStatus Status { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public string? FailureReason { get; init; }
    public string? Metadata { get; init; }
    public DateTime CreatedAt { get; init; }
}
