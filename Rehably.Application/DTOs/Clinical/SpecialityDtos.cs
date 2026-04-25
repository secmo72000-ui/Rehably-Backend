using System.ComponentModel.DataAnnotations;

namespace Rehably.Application.DTOs.Clinical;

public record SpecialityDto(
    Guid Id,
    string Code,
    string NameEn,
    string NameAr,
    string IcdChapters,
    string? Description,
    string? IconUrl,
    int DisplayOrder,
    bool IsActive,
    int DiagnosisCount
);

public record CreateSpecialityRequest(
    [Required][MaxLength(20)] string Code,
    [Required][MaxLength(100)] string NameEn,
    [Required][MaxLength(100)] string NameAr,
    [MaxLength(50)] string IcdChapters,
    string? Description,
    string? IconUrl,
    int DisplayOrder = 0
);

public record UpdateSpecialityRequest(
    [MaxLength(100)] string? NameEn,
    [MaxLength(100)] string? NameAr,
    [MaxLength(50)] string? IcdChapters,
    string? Description,
    string? IconUrl,
    int? DisplayOrder,
    bool? IsActive
);

public record ClinicSpecialityDto(
    Guid SpecialityId,
    string Code,
    string NameEn,
    string NameAr,
    DateTime AssignedAt
);

public record AssignSpecialitiesRequest(
    [Required] List<Guid> SpecialityIds
);
