namespace Rehably.Application.DTOs.Feature;

public record UpdateFeatureRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int DisplayOrder { get; init; }
}
