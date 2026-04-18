using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Package;

public record CreatePackageRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal MonthlyPrice { get; init; }
    public decimal YearlyPrice { get; init; }
    public decimal? CalculatedMonthlyPrice { get; init; }
    public decimal? CalculatedYearlyPrice { get; init; }
    public bool IsPublic { get; init; } = true;
    public bool IsCustom { get; init; } = false;
    public Guid? ForClinicId { get; init; }
    public int TrialDays { get; init; } = 0;
    public int DisplayOrder { get; init; } = 0;
    public PackageTier Tier { get; init; } = PackageTier.Basic;
    public bool IsPopular { get; init; } = false;
    public List<PackageFeatureRequestDto> Features { get; init; } = new();
}
