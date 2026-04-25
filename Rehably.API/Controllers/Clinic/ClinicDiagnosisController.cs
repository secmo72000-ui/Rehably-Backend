using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.Application.DTOs.Clinical;
using Rehably.Application.Services.Clinical;

namespace Rehably.API.Controllers.Clinic;

[ApiController]
[Route("api/clinic")]
[Authorize]
[Produces("application/json")]
[Tags("Clinic - Diagnoses")]
public class ClinicDiagnosisController : BaseController
{
    private readonly IDiagnosisService _diagnosisService;
    private readonly ISpecialityService _specialityService;

    public ClinicDiagnosisController(
        IDiagnosisService diagnosisService,
        ISpecialityService specialityService)
    {
        _diagnosisService = diagnosisService;
        _specialityService = specialityService;
    }

    /// <summary>
    /// Get diagnoses available for this clinic:
    /// global diagnoses matching clinic specialities + clinic's own custom diagnoses.
    /// </summary>
    [HttpGet("diagnoses")]
    public async Task<ActionResult<List<DiagnosisListItem>>> GetForClinic(
        [FromQuery] Guid? specialityId,
        [FromQuery] Guid? bodyRegionId,
        [FromQuery] string? search,
        CancellationToken ct = default)
    {
        if (TenantId is null) return UnauthorizedError("Clinic context not found.");
        return FromResult(await _diagnosisService.GetForClinicAsync(
            TenantId.Value, specialityId, bodyRegionId, search, ct));
    }

    /// <summary>
    /// Get all specialities assigned to this clinic.
    /// Used by the assessment wizard step 1 speciality selector.
    /// </summary>
    [HttpGet("diagnoses/specialities")]
    public async Task<ActionResult<List<ClinicSpecialityDto>>> GetSpecialities(
        CancellationToken ct = default)
    {
        if (TenantId is null) return UnauthorizedError("Clinic context not found.");
        return FromResult(await _specialityService.GetClinicSpecialitiesAsync(TenantId.Value, ct));
    }

    /// <summary>
    /// Create a custom diagnosis scoped to this clinic.
    /// Use for diagnoses not in the global ICD-10 list.
    /// </summary>
    [HttpPost("diagnoses")]
    public async Task<ActionResult<DiagnosisDto>> CreateCustom(
        [FromBody] CreateDiagnosisRequest request,
        CancellationToken ct = default)
    {
        if (TenantId is null) return UnauthorizedError("Clinic context not found.");
        return FromResult(
            await _diagnosisService.CreateClinicCustomAsync(TenantId.Value, request, ct), 201);
    }
}
