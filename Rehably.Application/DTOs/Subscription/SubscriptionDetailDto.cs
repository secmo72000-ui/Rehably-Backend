using Rehably.Application.DTOs.Package;
using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Subscription;

public record SubscriptionDetailDto
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
    public PackageSnapshotDto? PriceSnapshot { get; init; }
    public string? PaymentProvider { get; init; }
    public string? ProviderSubscriptionId { get; init; }
    public bool AutoRenew { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public string? CancelReason { get; init; }
    public List<SubscriptionFeatureUsageDto> FeatureUsage { get; init; } = new();
}
