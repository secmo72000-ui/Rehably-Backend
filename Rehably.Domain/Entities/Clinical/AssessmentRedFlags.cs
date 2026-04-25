using Rehably.Domain.Entities.Base;

namespace Rehably.Domain.Entities.Clinical;

/// <summary>Step 3 — Red Flags and Immediate Actions.</summary>
public class AssessmentRedFlags : BaseEntity
{
    public Guid AssessmentId { get; set; }

    /// <summary>
    /// JSON array of selected red flag keys. Possible values:
    /// "fracture_dislocation" | "neuro_deficit" | "saddle_anesthesia" |
    /// "infection_signs" | "dvt_pe" | "night_pain_weight_loss" | "chest_pain_syncope"
    /// </summary>
    public string? Flags { get; set; }

    /// <summary>"None" | "Present"</summary>
    public string? Decision { get; set; }
    public string? DecisionNotes { get; set; }

    /// <summary>
    /// JSON array of actions taken:
    /// "urgent_referral" | "emergency_protocol" | "surgeon_notified"
    /// </summary>
    public string? ActionsTaken { get; set; }
    public string? ActionNotes { get; set; }

    // Navigation
    public PatientAssessment Assessment { get; set; } = null!;
}
