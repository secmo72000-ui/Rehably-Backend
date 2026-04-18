namespace Rehably.Domain.Entities.Platform;

public class PackageFeature
{
    public Guid Id { get; set; }
    public Guid PackageId { get; set; }
    public Guid FeatureId { get; set; }
    public int? Quantity { get; set; } = 1;
    public int? Limit { get; set; }
    public bool IsIncluded { get; set; } = true;
    public decimal CalculatedPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Package Package { get; set; } = null!;
    public Feature Feature { get; set; } = null!;
}
