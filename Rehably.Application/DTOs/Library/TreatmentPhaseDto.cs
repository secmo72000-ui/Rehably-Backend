using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Library;

/// <summary>
/// DTO for a phase within a treatment protocol.
/// </summary>
public record TreatmentPhaseDto
{
    public Guid Id { get; init; }
    public Guid? ClinicId { get; init; }
    public string TreatmentCode { get; init; } = string.Empty;
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

    /// <summary>
    /// Computed property: true if ClinicId is null (global item).
    /// </summary>
    public bool IsGlobal => ClinicId == null;

    public bool IsDeleted { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
