using Rehably.Domain.Entities.Base;

namespace Rehably.Domain.Entities.Clinical;

/// <summary>Step 6 — Neurological and special orthopaedic tests.</summary>
public class AssessmentNeuro : BaseEntity
{
    public Guid AssessmentId { get; set; }

    // ── Neurological ──────────────────────────────────────────────────────────
    public string? Sensation { get; set; }
    public string? Numbness { get; set; }
    public string? Tingling { get; set; }
    public string? Myotomes { get; set; }
    public string? KeyMuscleWeakness { get; set; }
    public string? Reflexes { get; set; }

    /// <summary>
    /// JSON array of neurovascular checks: "pulses" | "cap_refill" | "temperature"
    /// </summary>
    public string? NeurovascularChecks { get; set; }

    // ── Special Tests ─────────────────────────────────────────────────────────
    /// <summary>
    /// JSON array of selected special test keys per body region.
    /// Shoulder: "painful_arc" | "er_resistance" | "hawkins_kennedy" | "neer" |
    ///           "apprehension_relocation" | "speeds" | "obrien"
    /// Knee:     "lachman" | "anterior_drawer" | "mcmurray" | "valgus_stress" | ...
    /// (Extensible per body region — stored as flat key list)
    /// </summary>
    public string? SpecialTests { get; set; }

    public string? AdditionalNotes { get; set; }

    // Navigation
    public PatientAssessment Assessment { get; set; } = null!;
}
