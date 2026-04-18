namespace Rehably.Application.DTOs.Package;

public record CustomPackageRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsPublic { get; init; }
    public bool IsCustom { get; init; } = true;
    public Guid? ForClinicId { get; init; }
    public int TrialDays { get; init; }
    public int DisplayOrder { get; init; }
    public decimal MonthlyPrice { get; init; }
    public decimal YearlyPrice { get; init; }
    public decimal? CalculatedMonthlyPrice { get; init; }
    public decimal? CalculatedYearlyPrice { get; init; }
    public List<PackageFeatureRequestDto> Features { get; init; } = new();
}
