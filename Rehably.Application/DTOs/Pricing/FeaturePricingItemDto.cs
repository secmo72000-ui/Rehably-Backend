using Rehably.Domain.Entities.Platform;

namespace Rehably.Application.DTOs.Pricing;

public record FeaturePricingItemDto
{
    public Guid FeatureId { get; init; }
    public string FeatureName { get; init; } = string.Empty;
    public string FeatureCode { get; init; } = string.Empty;
    public PricingType PricingType { get; init; }
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal Subtotal { get; init; }
}
