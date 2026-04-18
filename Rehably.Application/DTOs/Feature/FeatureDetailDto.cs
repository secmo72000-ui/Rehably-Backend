using Rehably.Domain.Entities.Platform;

namespace Rehably.Application.DTOs.Feature;

public record FeatureDetailDto
{
    public Guid Id { get; init; }
    public Guid CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public PricingType PricingType { get; init; }
    public bool IsActive { get; init; }
    public int DisplayOrder { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    public FeatureCategoryDto Category { get; init; } = null!;
}
