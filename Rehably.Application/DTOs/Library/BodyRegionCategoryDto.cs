namespace Rehably.Application.DTOs.Library;

/// <summary>
/// DTO for body region categories used in treatments and exercises.
/// </summary>
public record BodyRegionCategoryDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string NameArabic { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
}
