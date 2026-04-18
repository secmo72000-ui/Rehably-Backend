using Rehably.Domain.Entities.Platform;

namespace Rehably.Application.DTOs.Feature;

public record CreateFeatureRequestDto
{
    public Guid CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public PricingType PricingType { get; init; }
    public int DisplayOrder { get; init; }
}
