using Rehably.Domain.Entities.Base;

namespace Rehably.Domain.Entities.Clinical;

/// <summary>Step 4 — SOAP-S (Subjective): patient-reported symptoms and history.</summary>
public class AssessmentSubjective : BaseEntity
{
    public Guid AssessmentId { get; set; }

    public string? ChiefComplaint { get; set; }
    public string? OnsetMechanism { get; set; }       // How/when injury occurred

    // Pain levels 0–10
    public int? PainNow { get; set; }
    public int? PainBest { get; set; }
    public int? PainWorst { get; set; }

    // 24-hour pattern
    public bool? NightPain { get; set; }
    public bool? MorningStiffness { get; set; }
    public string? PainPattern24h { get; set; }       // free text detail

    public string? AggravatIngFactors { get; set; }
    public string? EasingFactors { get; set; }

    /// <summary>JSON array: ["adls","work","sport"]</summary>
    public string? FunctionalLimits { get; set; }

    public string? PreviousInjuries { get; set; }
    public string? MedicalHistory { get; set; }
    public string? Medications { get; set; }

    /// <summary>
    /// JSON array of screening flags:
    /// "trauma_7days" | "fever" | "weight_loss" | "cancer" | "steroids" |
    /// "anticoagulants" | "diabetes" | "pregnancy" |
    /// "smoking" | "psychosocial_risk" | "sleep_disturbed" | "work_compensation"
    /// </summary>
    public string? ScreeningFlags { get; set; }

    public string? PatientGoals { get; set; }
    public string? AdditionalNotes { get; set; }

    // Navigation
    public PatientAssessment Assessment { get; set; } = null!;
}
