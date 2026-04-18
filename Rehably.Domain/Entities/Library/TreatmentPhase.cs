using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Library;

/// <summary>
/// Phase within a treatment protocol.
/// Links to Treatment via TreatmentCode (not FK).
/// </summary>
public class TreatmentPhase
{
    public Guid Id { get; set; }
    public Guid? ClinicId { get; set; }
    public Guid TreatmentId { get; set; }
    public string TreatmentCode { get; set; } = string.Empty;
    public int PhaseNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameArabic { get; set; }
    public string? Description { get; set; }
    public string MainGoal { get; set; } = string.Empty;
    public string? ClinicalNotes { get; set; }
    public int MinDurationWeeks { get; set; }
    public int MaxDurationWeeks { get; set; }
    public int MinSessions { get; set; }
    public int MaxSessions { get; set; }
    public LibraryAccessTier AccessTier { get; set; } = LibraryAccessTier.Free;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public virtual Clinic? Clinic { get; set; }
    public virtual Treatment Treatment { get; set; } = null!;
}
