using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinical;
using Rehably.Application.Services.Clinical;

namespace Rehably.API.Controllers.Clinic;

[ApiController]
[Route("api/clinic")]
[Authorize]
[Produces("application/json")]
[Tags("Clinic - Assessments")]
public class ClinicAssessmentController : BaseController
{
    private readonly IPatientAssessmentService _service;

    public ClinicAssessmentController(IPatientAssessmentService service) => _service = service;

    // ── Start ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Start a new assessment for a checked-in appointment.
    /// Idempotent — returns existing draft if already started.
    /// Transitions appointment: CheckedIn → InProgress.
    /// </summary>
    [HttpPost("appointments/{appointmentId:guid}/assessment")]
    public async Task<ActionResult<AssessmentDetailDto>> Start(
        Guid appointmentId,
        [FromBody] CreateAssessmentRequest request,
        CancellationToken ct = default)
        => FromResult(await _service.StartAsync(appointmentId, request, ct), 201);

    /// <summary>Get the assessment for a specific appointment (returns null if not started).</summary>
    [HttpGet("appointments/{appointmentId:guid}/assessment")]
    public async Task<ActionResult<AssessmentDetailDto?>> GetByAppointment(
        Guid appointmentId,
        CancellationToken ct = default)
        => FromResult(await _service.GetByAppointmentAsync(appointmentId, ct));

    // ── Assessment CRUD ───────────────────────────────────────────────────────

    /// <summary>Load a full assessment by ID (all steps).</summary>
    [HttpGet("assessments/{id:guid}")]
    public async Task<ActionResult<AssessmentDetailDto>> GetById(
        Guid id,
        CancellationToken ct = default)
        => FromResult(await _service.GetByIdAsync(id, ct));

    /// <summary>Get all assessments for a patient (summary list).</summary>
    [HttpGet("patients/{patientId:guid}/assessments")]
    public async Task<ActionResult<List<AssessmentSummaryDto>>> GetByPatient(
        Guid patientId,
        CancellationToken ct = default)
        => FromResult(await _service.GetByPatientAsync(patientId, ct));

    // ── Step saves ────────────────────────────────────────────────────────────

    /// <summary>Save step 1 — Patient info, speciality, diagnosis.</summary>
    [HttpPut("assessments/{id:guid}/steps/1")]
    public async Task<ActionResult<AssessmentDetailDto>> SaveStep1(
        Guid id,
        [FromBody] UpdateStep1Request request,
        CancellationToken ct = default)
        => FromResult(await _service.SaveStep1Async(id, request, ct));

    /// <summary>Save step 2 — Post-operative details (only relevant when HasPostOp = true).</summary>
    [HttpPut("assessments/{id:guid}/steps/2")]
    public async Task<ActionResult<AssessmentDetailDto>> SaveStep2(
        Guid id,
        [FromBody] StepPostOpDto request,
        CancellationToken ct = default)
        => FromResult(await _service.SaveStep2Async(id, request, ct));

    /// <summary>Save step 3 — Red flags screening.</summary>
    [HttpPut("assessments/{id:guid}/steps/3")]
    public async Task<ActionResult<AssessmentDetailDto>> SaveStep3(
        Guid id,
        [FromBody] StepRedFlagsDto request,
        CancellationToken ct = default)
        => FromResult(await _service.SaveStep3Async(id, request, ct));

    /// <summary>Save step 4 — Subjective (SOAP-S).</summary>
    [HttpPut("assessments/{id:guid}/steps/4")]
    public async Task<ActionResult<AssessmentDetailDto>> SaveStep4(
        Guid id,
        [FromBody] StepSubjectiveDto request,
        CancellationToken ct = default)
        => FromResult(await _service.SaveStep4Async(id, request, ct));

    /// <summary>Save step 5 — Objective (SOAP-O): ROM, MMT, posture.</summary>
    [HttpPut("assessments/{id:guid}/steps/5")]
    public async Task<ActionResult<AssessmentDetailDto>> SaveStep5(
        Guid id,
        [FromBody] StepObjectiveDto request,
        CancellationToken ct = default)
        => FromResult(await _service.SaveStep5Async(id, request, ct));

    /// <summary>Save step 6 — Neurological exam and special tests.</summary>
    [HttpPut("assessments/{id:guid}/steps/6")]
    public async Task<ActionResult<AssessmentDetailDto>> SaveStep6(
        Guid id,
        [FromBody] StepNeuroDto request,
        CancellationToken ct = default)
        => FromResult(await _service.SaveStep6Async(id, request, ct));

    /// <summary>Save step 7 — Clinical reasoning, referral, imaging (SOAP-A+P).</summary>
    [HttpPut("assessments/{id:guid}/steps/7")]
    public async Task<ActionResult<AssessmentDetailDto>> SaveStep7(
        Guid id,
        [FromBody] StepClinicalReasoningDto request,
        CancellationToken ct = default)
        => FromResult(await _service.SaveStep7Async(id, request, ct));

    // ── Submit ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Submit the assessment (Draft → Submitted).
    /// Also transitions appointment: InProgress → Completed.
    /// </summary>
    [HttpPost("assessments/{id:guid}/submit")]
    public async Task<ActionResult<AssessmentDetailDto>> Submit(
        Guid id,
        CancellationToken ct = default)
        => FromResult(await _service.SubmitAsync(id, ct));
}
