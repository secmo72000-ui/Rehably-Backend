namespace Rehably.Application.DTOs.Feature;

public record UpdateFeaturePriceRequestDto
{
    public decimal Price { get; init; }
    public decimal? PerUnitPrice { get; init; }
}
