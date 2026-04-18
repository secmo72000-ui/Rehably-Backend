using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Subscription;

public record SubscriptionAddOnDto
{
    public Guid Id { get; init; }
    public Guid FeatureId { get; init; }
    public string FeatureName { get; init; } = string.Empty;
    public string FeatureCode { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal Price { get; init; }
    public AddOnStatus Status { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DateTime? CancelledAt { get; init; }
    public DateTime NextBillingDate { get; init; }
}
