using Rehably.Domain.Entities.Platform;

namespace Rehably.Application.DTOs.Package;

public record PackageSnapshotDto
{
    public Guid PackageId { get; init; }
    public string PackageName { get; init; } = string.Empty;
    public string PackageCode { get; init; } = string.Empty;
    public decimal MonthlyPrice { get; init; }
    public decimal YearlyPrice { get; init; }
    public decimal CalculatedMonthlyPrice { get; init; }
    public decimal CalculatedYearlyPrice { get; init; }
    public int TrialDays { get; init; }
    public bool IsPublic { get; init; }
    public bool IsCustom { get; init; }
    public Guid? ForClinicId { get; init; }
    public DateTime SnapshotDate { get; init; }
    public List<PackageFeatureSnapshotDto> Features { get; init; } = new();
    public decimal TotalMonthlyPrice => Features.Sum(f => f.CalculatedPrice);
}
