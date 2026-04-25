using System.ComponentModel.DataAnnotations;

namespace Rehably.Application.DTOs.Clinical;

public record DiagnosisDto(
    Guid Id,
    Guid? ClinicId,
    Guid SpecialityId,
    string SpecialityNameAr,
    Guid? BodyRegionCategoryId,
    string? BodyRegionNameAr,
    string IcdCode,
    string NameEn,
    string NameAr,
    string? Description,
    bool IsActive,
    bool IsGlobal,
    // Auto-suggest
    string? DefaultProtocolName,
    string? DefaultExerciseIds,
    int? SuggestedSessions,
    int? SuggestedDurationWeeks
);

public record DiagnosisListItem(
    Guid Id,
    string IcdCode,
    string NameEn,
    string NameAr,
    string SpecialityNameAr,
    string? BodyRegionNameAr,
    bool IsGlobal,
    bool IsActive
);

public record CreateDiagnosisRequest(
    [Required] Guid SpecialityId,
    Guid? BodyRegionCategoryId,
    [Required][MaxLength(20)] string IcdCode,
    [Required][MaxLength(300)] string NameEn,
    [Required][MaxLength(300)] string NameAr,
    string? Description,
    string? DefaultProtocolName,
    string? DefaultExerciseIds,
    int? SuggestedSessions,
    int? SuggestedDurationWeeks
);

public record UpdateDiagnosisRequest(
    Guid? BodyRegionCategoryId,
    [MaxLength(300)] string? NameEn,
    [MaxLength(300)] string? NameAr,
    string? Description,
    bool? IsActive,
    string? DefaultProtocolName,
    string? DefaultExerciseIds,
    int? SuggestedSessions,
    int? SuggestedDurationWeeks
);

public record DiagnosisQueryParams
{
    public Guid? SpecialityId { get; init; }
    public Guid? BodyRegionCategoryId { get; init; }
    public string? Search { get; init; }
    public bool? IsGlobal { get; init; }
    public bool? IsActive { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
