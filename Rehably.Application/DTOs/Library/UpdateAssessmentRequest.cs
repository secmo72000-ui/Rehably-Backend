using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Library;

/// <summary>
/// Request DTO for updating an existing assessment.
/// </summary>
public record UpdateAssessmentRequest
{
    public string Name { get; init; } = string.Empty;
    public string? NameArabic { get; init; }
    public AssessmentTimePoint TimePoint { get; init; }
    public string? Description { get; init; }
    public string? Instructions { get; init; }
    public string? ScoringGuide { get; init; }
    public string? RelatedConditionCodes { get; init; }
    public LibraryAccessTier AccessTier { get; init; }
}
