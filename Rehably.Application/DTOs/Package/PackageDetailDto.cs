using Rehably.Domain.Entities.Platform;

namespace Rehably.Application.DTOs.Package;

/// <summary>
/// Detailed package data transfer object with features.
/// </summary>
public record PackageDetailDto
{
    /// <summary>
    /// Unique identifier for the package.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Name of the package.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Unique code for the package.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Optional description of the package.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Monthly price of the package.
    /// </summary>
    public decimal MonthlyPrice { get; init; }

    /// <summary>
    /// Yearly price of the package.
    /// </summary>
    public decimal YearlyPrice { get; init; }

    /// <summary>
    /// Current status of the package.
    /// </summary>
    public PackageStatus Status { get; init; }

    /// <summary>
    /// Indicates if the package is active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Indicates if the package is publicly visible.
    /// </summary>
    public bool IsPublic { get; init; }

    /// <summary>
    /// Indicates if this is a custom package for a specific clinic.
    /// </summary>
    public bool IsCustom { get; init; }

    /// <summary>
    /// The clinic ID this custom package is for, if applicable.
    /// </summary>
    public Guid? ForClinicId { get; init; }

    /// <summary>
    /// Display order for sorting.
    /// </summary>
    public int DisplayOrder { get; init; }

    /// <summary>
    /// Number of trial days for this package.
    /// </summary>
    public int TrialDays { get; init; }

    /// <summary>
    /// Date when the package was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Date when the package was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// List of features included in this package.
    /// </summary>
    public required List<PackageFeatureDto> Features { get; init; } = new();
}
