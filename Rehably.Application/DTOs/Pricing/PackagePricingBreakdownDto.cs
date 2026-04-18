namespace Rehably.Application.DTOs.Pricing;

public record PackagePricingBreakdownDto
{
    public decimal BasePrice { get; init; }
    public decimal FeaturesTotal { get; init; }
    public decimal UserTierPrice { get; init; }
    public decimal StoragePrice { get; init; }
    public decimal TotalPrice { get; init; }
    public List<FeaturePricingItemDto> FeaturePricing { get; init; } = new();
}
