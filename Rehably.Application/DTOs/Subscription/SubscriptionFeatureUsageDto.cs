namespace Rehably.Application.DTOs.Subscription;

public record SubscriptionFeatureUsageDto
{
    public Guid Id { get; init; }
    public Guid FeatureId { get; init; }
    public string FeatureName { get; init; } = string.Empty;
    public string FeatureCode { get; init; } = string.Empty;
    public int Limit { get; init; }
    public int Used { get; init; }
    public int Remaining => Limit - Used;
    public DateTime LastResetAt { get; init; }
}
