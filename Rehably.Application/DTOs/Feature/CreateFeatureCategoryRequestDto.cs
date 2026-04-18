namespace Rehably.Application.DTOs.Feature;

public record CreateFeatureCategoryRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Icon { get; init; }
    public int DisplayOrder { get; init; }
    public Guid? ParentCategoryId { get; init; }
}
