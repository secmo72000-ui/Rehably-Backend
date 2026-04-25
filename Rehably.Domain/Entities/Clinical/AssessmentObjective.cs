using Rehably.Domain.Entities.Base;

namespace Rehably.Domain.Entities.Clinical;

/// <summary>
/// Step 5 — SOAP-O (Objective): physical observations and measurements.
/// Structured tables (ROM, MMT) stored as JSON for flexibility across body regions.
/// </summary>
public class AssessmentObjective : BaseEntity
{
    public Guid AssessmentId { get; set; }

    // ── Observation ───────────────────────────────────────────────────────────
    public string? Posture { get; set; }
    public string? Swelling { get; set; }
    public string? Redness { get; set; }
    public string? Deformity { get; set; }
    public string? Gait { get; set; }
    public string? Transfers { get; set; }
    public string? AssistiveDevices { get; set; }

    /// <summary>
    /// JSON array of selected functional tests:
    /// "equal" | "step_down" | "sit_to_stand" | "reach" | "grip" | "balance"
    /// </summary>
    public string? FunctionalTests { get; set; }

    // ── MMT Strength Table ────────────────────────────────────────────────────
    /// <summary>
    /// JSON array of strength measurements per muscle group:
    /// [{ muscleGroup, left, right, painLimited, notes }]
    /// </summary>
    public string? StrengthData { get; set; }

    // ── ROM Table ─────────────────────────────────────────────────────────────
    /// <summary>
    /// JSON array of ROM measurements per movement:
    /// [{ movement, arom, prom, painEndFeel, notes }]
    /// </summary>
    public string? RomData { get; set; }

    public string? AdditionalNotes { get; set; }

    // Navigation
    public PatientAssessment Assessment { get; set; } = null!;
}
