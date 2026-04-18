using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Library;

/// <summary>
/// Request DTO for updating an existing treatment phase.
/// </summary>
public record UpdateTreatmentPhaseRequest
{
    public int PhaseNumber { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? NameArabic { get; init; }
    public string? Description { get; init; }
    public string MainGoal { get; init; } = string.Empty;
    public string? ClinicalNotes { get; init; }
    public int MinDurationWeeks { get; init; }
    public int MaxDurationWeeks { get; init; }
    public int MinSessions { get; init; }
    public int MaxSessions { get; init; }
    public LibraryAccessTier AccessTier { get; init; }
}
