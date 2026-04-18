namespace Rehably.Domain.Entities.Platform;

public class FeatureCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public FeatureCategory? ParentCategory { get; set; }
    public ICollection<FeatureCategory> SubCategories { get; set; } = new List<FeatureCategory>();
    public ICollection<Feature> Features { get; set; } = new List<Feature>();
}
