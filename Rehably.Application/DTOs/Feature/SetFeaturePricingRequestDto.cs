namespace Rehably.Application.DTOs.Feature;

public record SetFeaturePricingRequestDto
{
    public decimal BasePrice { get; init; }
    public decimal PerUnitPrice { get; init; }
    public DateTime? EffectiveDate { get; init; }
    public DateTime? ExpiryDate { get; init; }
}
