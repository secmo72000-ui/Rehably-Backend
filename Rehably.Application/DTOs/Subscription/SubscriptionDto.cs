using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Subscription;

public record SubscriptionDto
{
    public Guid Id { get; init; }
    public Guid ClinicId { get; init; }
    public Guid PackageId { get; init; }
    public string PackageName { get; init; } = string.Empty;
    public string PackageCode { get; init; } = string.Empty;
    public SubscriptionStatus Status { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DateTime? TrialEndsAt { get; init; }
    public string? PaymentProvider { get; init; }
    public bool AutoRenew { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public string? CancelReason { get; init; }

    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
}
