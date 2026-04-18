using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Package;

public record UpdatePackageRequestDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public decimal MonthlyPrice { get; init; }
    public decimal YearlyPrice { get; init; }
    public decimal? CalculatedMonthlyPrice { get; init; }
    public decimal? CalculatedYearlyPrice { get; init; }
    public int? TrialDays { get; init; }
    public int DisplayOrder { get; init; }
    public PackageTier Tier { get; init; } = PackageTier.Basic;
    public bool IsPopular { get; init; } = false;
    public List<PackageFeatureRequestDto>? Features { get; init; }
}
