namespace Rehably.Application.DTOs.Feature;

public record FeatureCategoryDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int DisplayOrder { get; init; }
    public Guid? ParentCategoryId { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    public List<FeatureCategoryDto> SubCategories { get; init; } = new();
    public List<FeatureDto> Features { get; init; } = new();
}
