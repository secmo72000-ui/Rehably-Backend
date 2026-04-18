namespace Rehably.Domain.Entities.Platform;

public class FeaturePricing
{
    public Guid Id { get; set; }
    public Guid FeatureId { get; set; }
    public decimal BasePrice { get; set; }
    public decimal PerUnitPrice { get; set; }
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Feature Feature { get; set; } = null!;
}
