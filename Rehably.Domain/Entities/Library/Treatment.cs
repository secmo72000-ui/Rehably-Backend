using Rehably.Domain.Entities.Base;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Library;

/// <summary>
/// Treatment protocol in the global library.
/// ClinicId = null means global item, otherwise clinic-specific custom item.
/// </summary>
public class Treatment : BaseEntity
{
    public Guid? ClinicId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameArabic { get; set; }
    public Guid BodyRegionCategoryId { get; set; }
    public string AffectedArea { get; set; } = string.Empty;
    public int MinDurationWeeks { get; set; }
    public int MaxDurationWeeks { get; set; }
    public int ExpectedSessions { get; set; }
    public string? Description { get; set; }
    public string? RedFlags { get; set; }
    public string? Contraindications { get; set; }
    public string? ClinicalNotes { get; set; }
    public string? SourceReference { get; set; }
    public string? SourceDetails { get; set; }
    public LibraryAccessTier AccessTier { get; set; } = LibraryAccessTier.Free;

    public virtual Clinic? Clinic { get; set; }
    public virtual BodyRegionCategory BodyRegionCategory { get; set; } = null!;
    public virtual ICollection<TreatmentPhase> Phases { get; set; } = new List<TreatmentPhase>();
}
