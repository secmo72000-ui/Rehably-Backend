using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.Application.Common;
using Rehably.Application.DTOs.ClinicPortal;
using Rehably.Application.Services.ClinicPortal;

namespace Rehably.API.Controllers.Clinic;

[ApiController]
[Route("api/clinic/patients")]
[Authorize]
[Produces("application/json")]
[Tags("Clinic - Patients")]
public class ClinicPatientsController : BaseController
{
    private readonly IPatientService _patientService;

    public ClinicPatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    /// <summary>
    /// Get all patients with filtering and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PatientListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PatientListDto>>> GetPatients(
        [FromQuery] PatientQueryParams query,
        CancellationToken cancellationToken = default)
    {
        var result = await _patientService.GetAllAsync(query, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Get a patient by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientDetailDto>> GetPatient(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _patientService.GetByIdAsync(id, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Create a new patient.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PatientDetailDto>> CreatePatient(
        [FromBody] CreatePatientRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _patientService.CreateAsync(request, cancellationToken);
        return FromResult(result, 201);
    }

    /// <summary>
    /// Update an existing patient.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientDetailDto>> UpdatePatient(
        Guid id,
        [FromBody] UpdatePatientRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _patientService.UpdateAsync(id, request, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Discharge a patient.
    /// </summary>
    [HttpPost("{id:guid}/discharge")]
    [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientDetailDto>> DischargePatient(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _patientService.DischargeAsync(id, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Delete a patient (soft delete).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeletePatient(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _patientService.DeleteAsync(id, cancellationToken);
        return FromResult(result, 204);
    }
}
