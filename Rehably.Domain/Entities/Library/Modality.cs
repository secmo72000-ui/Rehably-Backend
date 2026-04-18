using Rehably.Domain.Entities.Base;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Library;

/// <summary>
/// Therapeutic modality in the global library.
/// </summary>
public class Modality : BaseEntity
{
    public Guid? ClinicId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameArabic { get; set; }
    public ModalityType ModalityType { get; set; }
    public string TherapeuticCategory { get; set; } = string.Empty;
    public string MainGoal { get; set; } = string.Empty;
    public string? ParametersNotes { get; set; }
    public string? ClinicalNotes { get; set; }
    public int? MinDurationWeeks { get; set; }
    public int? MaxDurationWeeks { get; set; }
    public int? MinSessions { get; set; }
    public int? MaxSessions { get; set; }
    public string? RelatedConditionCodes { get; set; }
    public LibraryAccessTier AccessTier { get; set; } = LibraryAccessTier.Free;

    public virtual Clinic? Clinic { get; set; }
}
