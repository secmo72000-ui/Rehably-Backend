namespace Rehably.Application.DTOs.Feature;

public record FeaturePricingDto
{
    public Guid Id { get; init; }
    public Guid FeatureId { get; init; }
    public decimal BasePrice { get; init; }
    public decimal PerUnitPrice { get; init; }
    public DateTime EffectiveDate { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
