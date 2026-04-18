namespace Rehably.Application.DTOs.Library;

/// <summary>
/// DTO for a specific body region (subset of BodyRegionCategory).
/// </summary>
public record BodyRegionDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string NameArabic { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
}
