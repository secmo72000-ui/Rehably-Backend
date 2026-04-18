using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Library;

/// <summary>
/// DTO for assessment tool in the global library.
/// </summary>
public record AssessmentDto
{
    public Guid Id { get; set; }
    public Guid? ClinicId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameArabic { get; set; }
    public AssessmentTimePoint TimePoint { get; set; }
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public string? ScoringGuide { get; set; }
    public string? RelatedConditionCodes { get; set; }
    public LibraryAccessTier AccessTier { get; set; }

    /// <summary>
    /// Computed property: true if ClinicId is null (global item).
    /// </summary>
    public bool IsGlobal => ClinicId == null;

    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
