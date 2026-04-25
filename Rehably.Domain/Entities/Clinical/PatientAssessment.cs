using Rehably.Domain.Entities.Base;
using Rehably.Domain.Entities.ClinicPortal;
using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Clinical;

/// <summary>
/// A clinical SOAP assessment performed by a doctor during a session.
/// Linked to an Appointment (must be CheckedIn status to start).
/// Contains the base Patient Information (Step 1) directly;
/// each subsequent step is a separate 1:1 child entity.
/// </summary>
public class PatientAssessment : BaseEntity
{
    public Guid ClinicId { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }

    /// <summary>Doctor/therapist who performed the assessment.</summary>
    public string? TherapistId { get; set; }
    public string? TherapistName { get; set; }

    // ── Step 1 — Patient Information ──────────────────────────────────────────
    public Guid SpecialityId { get; set; }
    public Guid? BodyRegionCategoryId { get; set; }
    public Guid? DiagnosisId { get; set; }

    /// <summary>Free-text diagnosis override (for clinic-custom or unmatched).</summary>
    public string? DiagnosisFreeText { get; set; }

    public int? PatientAge { get; set; }
    public string? Gender { get; set; }          // "Male" | "Female"
    public bool HasPostOp { get; set; }

    /// <summary>JSON array of attachment URLs uploaded during assessment.</summary>
    public string? AttachmentUrls { get; set; }

    public string? AdditionalNotes { get; set; }

    // ── Status ────────────────────────────────────────────────────────────────
    public AssessmentStatus Status { get; set; } = AssessmentStatus.Draft;
    public DateTime? SubmittedAt { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public Speciality Speciality { get; set; } = null!;
    public Diagnosis? Diagnosis { get; set; }
    public AssessmentPostOp? PostOp { get; set; }
    public AssessmentRedFlags? RedFlags { get; set; }
    public AssessmentSubjective? Subjective { get; set; }
    public AssessmentObjective? Objective { get; set; }
    public AssessmentNeuro? Neuro { get; set; }
    public AssessmentClinicalReasoning? ClinicalReasoning { get; set; }
}
