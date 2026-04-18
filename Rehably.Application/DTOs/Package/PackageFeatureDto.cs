using Rehably.Domain.Entities.Platform;

namespace Rehably.Application.DTOs.Package;

/// <summary>
/// Package feature data transfer object.
/// </summary>
public record PackageFeatureDto
{
    /// <summary>
    /// Unique identifier for the package feature.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// ID of the package this feature belongs to.
    /// </summary>
    public required Guid PackageId { get; init; }

    /// <summary>
    /// ID of the feature.
    /// </summary>
    public required Guid FeatureId { get; init; }

    /// <summary>
    /// Name of the feature.
    /// </summary>
    public required string FeatureName { get; init; }

    /// <summary>
    /// Unique code for the feature.
    /// </summary>
    public required string FeatureCode { get; init; }

    /// <summary>
    /// Price of the feature.
    /// </summary>
    public decimal FeaturePrice { get; init; }

    /// <summary>
    /// Type of pricing for this feature.
    /// </summary>
    public PricingType PricingType { get; init; }

    /// <summary>
    /// Per-unit price if applicable.
    /// </summary>
    public decimal? PerUnitPrice { get; init; }

    /// <summary>
    /// Indicates if this feature is included in the package.
    /// </summary>
    public bool IsIncluded { get; init; }

    /// <summary>
    /// Usage limit for this feature.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// Calculated price for this feature.
    /// </summary>
    public decimal CalculatedPrice { get; init; }
}
