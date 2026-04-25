using Rehably.Domain.Entities.Base;

namespace Rehably.Domain.Entities.Clinical;

/// <summary>Step 2 — Post-operation data (only when HasPostOp = true).</summary>
public class AssessmentPostOp : BaseEntity
{
    public Guid AssessmentId { get; set; }

    public string? ProcedureName { get; set; }
    public string? ProcedureSide { get; set; }        // "Left" | "Right" | "Bilateral"
    public DateTime? SurgeryDate { get; set; }
    public int? DaysPostOp { get; set; }
    public string? SurgeonFacility { get; set; }

    /// <summary>NWB | TTWB | PWB | WBAT | FWB</summary>
    public string? WeightBearingStatus { get; set; }

    public string? RomRestriction { get; set; }
    public string? PostOpPrecautions { get; set; }

    /// <summary>JSON array: ["clean","redness","drainage","fever"]</summary>
    public string? WoundStatus { get; set; }

    public string? Notes { get; set; }

    // Navigation
    public PatientAssessment Assessment { get; set; } = null!;
}
