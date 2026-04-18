using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.Application.Common;
using Rehably.Application.DTOs.ClinicPortal;
using Rehably.Application.Services.ClinicPortal;

namespace Rehably.API.Controllers.Clinic;

[ApiController]
[Route("api/clinic/treatment-plans")]
[Authorize]
[Produces("application/json")]
[Tags("Clinic - Treatment Plans")]
public class ClinicTreatmentPlansController : BaseController
{
    private readonly ITreatmentPlanService _treatmentPlanService;

    public ClinicTreatmentPlansController(ITreatmentPlanService treatmentPlanService)
    {
        _treatmentPlanService = treatmentPlanService;
    }

    /// <summary>
    /// Get all treatment plans with filtering and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TreatmentPlanDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TreatmentPlanDto>>> GetTreatmentPlans(
        [FromQuery] TreatmentPlanQueryParams query,
        CancellationToken cancellationToken = default)
    {
        var result = await _treatmentPlanService.GetAllAsync(query, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Get a treatment plan by ID including all sessions.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TreatmentPlanDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TreatmentPlanDetailDto>> GetTreatmentPlan(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _treatmentPlanService.GetByIdAsync(id, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Get all treatment plans for a specific patient.
    /// </summary>
    [HttpGet("by-patient/{patientId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<TreatmentPlanDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TreatmentPlanDto>>> GetByPatient(Guid patientId, CancellationToken cancellationToken = default)
    {
        var result = await _treatmentPlanService.GetByPatientAsync(patientId, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Create a new treatment plan.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TreatmentPlanDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TreatmentPlanDetailDto>> CreateTreatmentPlan(
        [FromBody] CreateTreatmentPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _treatmentPlanService.CreateAsync(request, cancellationToken);
        return FromResult(result, 201);
    }

    /// <summary>
    /// Update a treatment plan.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TreatmentPlanDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TreatmentPlanDetailDto>> UpdateTreatmentPlan(
        Guid id,
        [FromBody] UpdateTreatmentPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _treatmentPlanService.UpdateAsync(id, request, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Activate a treatment plan (move from Draft to Active).
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(typeof(ApiResponse<TreatmentPlanDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TreatmentPlanDetailDto>> ActivateTreatmentPlan(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _treatmentPlanService.ActivateAsync(id, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Mark a treatment plan as completed.
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(ApiResponse<TreatmentPlanDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TreatmentPlanDetailDto>> CompleteTreatmentPlan(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _treatmentPlanService.CompleteAsync(id, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Delete a treatment plan (soft delete).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteTreatmentPlan(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _treatmentPlanService.DeleteAsync(id, cancellationToken);
        return FromResult(result, 204);
    }

    // ── Sessions ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Get all sessions for a treatment plan.
    /// </summary>
    [HttpGet("{planId:guid}/sessions")]
    [ProducesResponseType(typeof(ApiResponse<List<SessionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SessionDto>>> GetSessions(Guid planId, CancellationToken cancellationToken = default)
    {
        var result = await _treatmentPlanService.GetSessionsAsync(planId, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Add a session to a treatment plan.
    /// </summary>
    [HttpPost("{planId:guid}/sessions")]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionDto>> AddSession(
        Guid planId,
        [FromBody] CreateSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _treatmentPlanService.AddSessionAsync(planId, request, cancellationToken);
        return FromResult(result, 201);
    }

    /// <summary>
    /// Update a session.
    /// </summary>
    [HttpPut("sessions/{sessionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionDto>> UpdateSession(
        Guid sessionId,
        [FromBody] UpdateSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _treatmentPlanService.UpdateSessionAsync(sessionId, request, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Mark a session as completed with outcome data.
    /// </summary>
    [HttpPost("sessions/{sessionId:guid}/complete")]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionDto>> CompleteSession(
        Guid sessionId,
        [FromBody] CompleteSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _treatmentPlanService.CompleteSessionAsync(sessionId, request, cancellationToken);
        return FromResult(result);
    }
}
