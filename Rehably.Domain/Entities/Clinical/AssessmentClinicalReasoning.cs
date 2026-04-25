using Rehably.Domain.Entities.Base;

namespace Rehably.Domain.Entities.Clinical;

/// <summary>Step 7 — SOAP-A: Clinical reasoning, differential diagnosis and referral decision.</summary>
public class AssessmentClinicalReasoning : BaseEntity
{
    public Guid AssessmentId { get; set; }

    public string? ProblemList { get; set; }               // Impairments + activity limitations
    public string? WorkingHypothesis { get; set; }         // Suspected pain generator
    public string? SeverityIrritability { get; set; }      // Low/Moderate/High + Acute/Subacute/Chronic
    public string? DifferentialConsiderations { get; set; }
    public string? DecisionPoints { get; set; }            // Conservative vs refer vs post-op escalation

    // ── Referral / Imaging ────────────────────────────────────────────────────
    public bool? ImagingRequested { get; set; }
    public string? ImagingReason { get; set; }
    public bool? ReferralRequired { get; set; }
    public string? ReferralTo { get; set; }

    /// <summary>"routine" | "urgent" | "emergency"</summary>
    public string? Urgency { get; set; }

    public bool? BreakGlassUsed { get; set; }
    public string? BreakGlassReason { get; set; }

    public string? AdditionalNotes { get; set; }

    // Navigation
    public PatientAssessment Assessment { get; set; } = null!;
}
