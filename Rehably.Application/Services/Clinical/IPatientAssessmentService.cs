using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinical;

namespace Rehably.Application.Services.Clinical;

public interface IPatientAssessmentService
{
    /// <summary>
    /// Create a new draft assessment when the doctor clicks "بدء التقييم".
    /// The appointment must be in CheckedIn status.
    /// If a draft already exists for this appointment, returns the existing one.
    /// </summary>
    Task<Result<AssessmentDetailDto>> StartAsync(Guid appointmentId, CreateAssessmentRequest request, CancellationToken ct = default);

    /// <summary>Load the full assessment (all steps) by assessment ID.</summary>
    Task<Result<AssessmentDetailDto>> GetByIdAsync(Guid assessmentId, CancellationToken ct = default);

    /// <summary>Load the assessment for a given appointment (if exists).</summary>
    Task<Result<AssessmentDetailDto?>> GetByAppointmentAsync(Guid appointmentId, CancellationToken ct = default);

    /// <summary>List all assessments for a patient (summary only).</summary>
    Task<Result<List<AssessmentSummaryDto>>> GetByPatientAsync(Guid patientId, CancellationToken ct = default);

    /// <summary>Save / upsert step 1 (patient info, speciality, diagnosis).</summary>
    Task<Result<AssessmentDetailDto>> SaveStep1Async(Guid assessmentId, UpdateStep1Request request, CancellationToken ct = default);

    /// <summary>Save / upsert step 2 (post-op). Ignored if HasPostOp = false.</summary>
    Task<Result<AssessmentDetailDto>> SaveStep2Async(Guid assessmentId, StepPostOpDto request, CancellationToken ct = default);

    /// <summary>Save / upsert step 3 (red flags).</summary>
    Task<Result<AssessmentDetailDto>> SaveStep3Async(Guid assessmentId, StepRedFlagsDto request, CancellationToken ct = default);

    /// <summary>Save / upsert step 4 (subjective — SOAP S).</summary>
    Task<Result<AssessmentDetailDto>> SaveStep4Async(Guid assessmentId, StepSubjectiveDto request, CancellationToken ct = default);

    /// <summary>Save / upsert step 5 (objective — SOAP O).</summary>
    Task<Result<AssessmentDetailDto>> SaveStep5Async(Guid assessmentId, StepObjectiveDto request, CancellationToken ct = default);

    /// <summary>Save / upsert step 6 (neuro exam).</summary>
    Task<Result<AssessmentDetailDto>> SaveStep6Async(Guid assessmentId, StepNeuroDto request, CancellationToken ct = default);

    /// <summary>Save / upsert step 7 (clinical reasoning — SOAP A+P).</summary>
    Task<Result<AssessmentDetailDto>> SaveStep7Async(Guid assessmentId, StepClinicalReasoningDto request, CancellationToken ct = default);

    /// <summary>
    /// Submit the assessment (Draft → Submitted).
    /// Also transitions the appointment to Completed.
    /// </summary>
    Task<Result<AssessmentDetailDto>> SubmitAsync(Guid assessmentId, CancellationToken ct = default);
}
