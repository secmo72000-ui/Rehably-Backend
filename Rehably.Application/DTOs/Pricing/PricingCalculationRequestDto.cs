namespace Rehably.Application.DTOs.Pricing;

public class PricingCalculationRequestDto
{
    public int UserCount { get; set; }
    public int StorageGB { get; set; }
    public Dictionary<Guid, int> FeatureQuantities { get; set; } = new();
}
