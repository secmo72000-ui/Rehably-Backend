namespace Rehably.Application.DTOs.Package;

/// <summary>
/// Public-facing package information.
/// </summary>
public record PublicPackageDto
{
    /// <summary>Package ID</summary>
    public Guid Id { get; init; }
    /// <summary>Package name</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Package description</summary>
    public string? Description { get; init; }
    /// <summary>Monthly price</summary>
    public decimal MonthlyPrice { get; init; }
    /// <summary>Yearly price</summary>
    public decimal YearlyPrice { get; init; }
    /// <summary>Trial days offered</summary>
    public int TrialDays { get; init; }
    /// <summary>Package tier (e.g. Basic, Standard, Premium, Enterprise)</summary>
    public string Tier { get; init; } = string.Empty;
    /// <summary>Whether this package is highlighted as popular</summary>
    public bool IsPopular { get; init; }
    /// <summary>Whether this package has a monthly pricing option</summary>
    public bool HasMonthly { get; init; }
    /// <summary>Whether this package has a yearly pricing option</summary>
    public bool HasYearly { get; init; }
    /// <summary>Features included in this package</summary>
    public List<PublicPackageFeatureDto> Features { get; init; } = new();
}
