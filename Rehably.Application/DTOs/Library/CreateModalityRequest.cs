using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Library;

/// <summary>
/// Request DTO for creating a new modality.
/// </summary>
public record CreateModalityRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? NameArabic { get; init; }
    public ModalityType ModalityType { get; init; }
    public string TherapeuticCategory { get; init; } = string.Empty;
    public string MainGoal { get; init; } = string.Empty;
    public string? ParametersNotes { get; init; }
    public string? ClinicalNotes { get; init; }
    public int? MinDurationWeeks { get; init; }
    public int? MaxDurationWeeks { get; init; }
    public int? MinSessions { get; init; }
    public int? MaxSessions { get; init; }
    public string? RelatedConditionCodes { get; init; }
    public LibraryAccessTier AccessTier { get; init; } = LibraryAccessTier.Free;
}
