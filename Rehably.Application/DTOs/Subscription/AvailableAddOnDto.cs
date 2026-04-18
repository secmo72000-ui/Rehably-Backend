using Rehably.Domain.Entities.Platform;

namespace Rehably.Application.DTOs.Subscription;

public record AvailableAddOnDto
{
    public Guid FeatureId { get; init; }
    public string FeatureName { get; init; } = string.Empty;
    public string FeatureCode { get; init; } = string.Empty;
    public string? Description { get; init; }
    public PricingType PricingType { get; init; }
    public decimal BasePrice { get; init; }
    public decimal PerUnitPrice { get; init; }
    public int MinQuantity { get; init; } = 1;
    public int MaxQuantity { get; init; } = 100;

    /// <summary>Current active add-on limit for this feature (0 if no active add-on).</summary>
    public int CurrentAddonLimit { get; init; }
}
