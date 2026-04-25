namespace Rehably.Application.DTOs.Clinical;

// ─── Summary (list view) ──────────────────────────────────────────────────────

public record AssessmentSummaryDto(
    Guid Id,
    Guid AppointmentId,
    Guid PatientId,
    string? TherapistName,
    string? SpecialityNameAr,
    string? DiagnosisNameAr,
    string? DiagnosisFreeText,
    string Status,             // "Draft" | "Submitted" | "Archived"
    DateTime CreatedAt,
    DateTime? SubmittedAt
);

// ─── Full detail (wizard load) ────────────────────────────────────────────────

public record AssessmentDetailDto(
    Guid Id,
    Guid AppointmentId,
    Guid PatientId,
    string? TherapistId,
    string? TherapistName,

    // Step 1
    Guid SpecialityId,
    string? SpecialityNameAr,
    Guid? BodyRegionCategoryId,
    Guid? DiagnosisId,
    string? DiagnosisNameAr,
    string? DiagnosisFreeText,
    int? PatientAge,
    string? Gender,
    bool HasPostOp,
    string? AdditionalNotes,

    string Status,
    DateTime CreatedAt,
    DateTime? SubmittedAt,

    // Sub-steps (null = not yet saved)
    StepPostOpDto? PostOp,
    StepRedFlagsDto? RedFlags,
    StepSubjectiveDto? Subjective,
    StepObjectiveDto? Objective,
    StepNeuroDto? Neuro,
    StepClinicalReasoningDto? ClinicalReasoning
);

// ─── Step 1 — Create / Patient Info ──────────────────────────────────────────

public class CreateAssessmentRequest
{
    public Guid SpecialityId { get; set; }
    public Guid? BodyRegionCategoryId { get; set; }
    public Guid? DiagnosisId { get; set; }
    public string? DiagnosisFreeText { get; set; }
    public int? PatientAge { get; set; }
    public string? Gender { get; set; }
    public bool HasPostOp { get; set; }
    public string? AdditionalNotes { get; set; }
}

public class UpdateStep1Request
{
    public Guid SpecialityId { get; set; }
    public Guid? BodyRegionCategoryId { get; set; }
    public Guid? DiagnosisId { get; set; }
    public string? DiagnosisFreeText { get; set; }
    public int? PatientAge { get; set; }
    public string? Gender { get; set; }
    public bool HasPostOp { get; set; }
    public string? AdditionalNotes { get; set; }
}

// ─── Step 2 — Post-Op ────────────────────────────────────────────────────────

public class StepPostOpDto
{
    public string? ProcedureName { get; set; }
    public string? ProcedureSide { get; set; }
    public DateTime? SurgeryDate { get; set; }
    public int? DaysPostOp { get; set; }
    public string? SurgeonFacility { get; set; }
    public string? WeightBearingStatus { get; set; }
    public string? RomRestriction { get; set; }
    public string? PostOpPrecautions { get; set; }
    public string? WoundStatus { get; set; }   // JSON array
    public string? Notes { get; set; }
}

// ─── Step 3 — Red Flags ──────────────────────────────────────────────────────

public class StepRedFlagsDto
{
    public string? Flags { get; set; }           // JSON array
    public string? Decision { get; set; }        // "None" | "Present"
    public string? DecisionNotes { get; set; }
    public string? ActionsTaken { get; set; }    // JSON array
    public string? ActionNotes { get; set; }
}

// ─── Step 4 — Subjective ─────────────────────────────────────────────────────

public class StepSubjectiveDto
{
    public string? ChiefComplaint { get; set; }
    public string? OnsetMechanism { get; set; }
    public int? PainNow { get; set; }
    public int? PainBest { get; set; }
    public int? PainWorst { get; set; }
    public bool? NightPain { get; set; }
    public bool? MorningStiffness { get; set; }
    public string? PainPattern24h { get; set; }
    public string? AggravatIngFactors { get; set; }
    public string? EasingFactors { get; set; }
    public string? FunctionalLimits { get; set; }  // JSON array
    public string? PreviousInjuries { get; set; }
    public string? MedicalHistory { get; set; }
    public string? Medications { get; set; }
    public string? ScreeningFlags { get; set; }    // JSON array
    public string? PatientGoals { get; set; }
    public string? AdditionalNotes { get; set; }
}

// ─── Step 5 — Objective ──────────────────────────────────────────────────────

public class StepObjectiveDto
{
    public string? Posture { get; set; }
    public string? Swelling { get; set; }
    public string? Redness { get; set; }
    public string? Deformity { get; set; }
    public string? Gait { get; set; }
    public string? Transfers { get; set; }
    public string? AssistiveDevices { get; set; }
    public string? FunctionalTests { get; set; }  // JSON array
    public string? StrengthData { get; set; }     // JSON array [{muscleGroup, left, right, painLimited, notes}]
    public string? RomData { get; set; }          // JSON array [{movement, arom, prom, painEndFeel, notes}]
    public string? AdditionalNotes { get; set; }
}

// ─── Step 6 — Neuro ──────────────────────────────────────────────────────────

public class StepNeuroDto
{
    public string? Sensation { get; set; }
    public string? Numbness { get; set; }
    public string? Tingling { get; set; }
    public string? Myotomes { get; set; }
    public string? KeyMuscleWeakness { get; set; }
    public string? Reflexes { get; set; }
    public string? NeurovascularChecks { get; set; }  // JSON array
    public string? SpecialTests { get; set; }         // JSON array
    public string? AdditionalNotes { get; set; }
}

// ─── Step 7 — Clinical Reasoning ─────────────────────────────────────────────

public class StepClinicalReasoningDto
{
    public string? ProblemList { get; set; }
    public string? WorkingHypothesis { get; set; }
    public string? SeverityIrritability { get; set; }
    public string? DifferentialConsiderations { get; set; }
    public string? DecisionPoints { get; set; }
    public bool? ImagingRequested { get; set; }
    public string? ImagingReason { get; set; }
    public bool? ReferralRequired { get; set; }
    public string? ReferralTo { get; set; }
    public string? Urgency { get; set; }
    public bool? BreakGlassUsed { get; set; }
    public string? BreakGlassReason { get; set; }
    public string? AdditionalNotes { get; set; }
}
