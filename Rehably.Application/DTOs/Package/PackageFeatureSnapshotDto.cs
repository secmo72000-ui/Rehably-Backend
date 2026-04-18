using Rehably.Domain.Entities.Platform;

namespace Rehably.Application.DTOs.Package;

public record PackageFeatureSnapshotDto
{
    public Guid FeatureId { get; init; }
    public string FeatureName { get; init; } = string.Empty;
    public string FeatureCode { get; init; } = string.Empty;
    public int? Limit { get; init; }
    public bool IsIncluded { get; init; }
    public decimal CalculatedPrice { get; init; }
    public PricingType PricingType { get; init; }
}
