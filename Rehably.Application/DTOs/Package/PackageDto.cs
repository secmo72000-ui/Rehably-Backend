using Rehably.Domain.Entities.Platform;

namespace Rehably.Application.DTOs.Package;

public record PackageDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal MonthlyPrice { get; init; }
    public decimal YearlyPrice { get; init; }
    public PackageStatus Status { get; init; }
    public bool IsActive { get; init; }
    public bool IsPublic { get; init; }
    public bool IsCustom { get; init; }
    public Guid? ForClinicId { get; init; }
    public int DisplayOrder { get; init; }
    public int TrialDays { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public List<PackageFeatureDto> Features { get; init; } = new();
}
