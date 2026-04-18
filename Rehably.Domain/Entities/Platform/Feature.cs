using System.Collections.Generic;

namespace Rehably.Domain.Entities.Platform;

public class Feature
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PricingType PricingType { get; set; }
    public string? IconKey { get; set; }
    public bool IsAddOn { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public FeatureCategory Category { get; set; } = null!;
    public ICollection<FeaturePricing> PricingHistory { get; set; } = new List<FeaturePricing>();
    public ICollection<PackageFeature> PackageFeatures { get; set; } = new List<PackageFeature>();
}
