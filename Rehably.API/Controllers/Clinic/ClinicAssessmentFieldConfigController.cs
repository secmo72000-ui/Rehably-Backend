using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.Application.DTOs.Clinical;
using Rehably.Application.Services.Clinical;

namespace Rehably.API.Controllers.Clinic;

[ApiController]
[Route("api/clinic/assessments/field-config")]
[Authorize]
[Produces("application/json")]
[Tags("Clinic - Assessment Field Config")]
public class ClinicAssessmentFieldConfigController : BaseController
{
    private readonly IAssessmentFieldConfigService _service;

    public ClinicAssessmentFieldConfigController(IAssessmentFieldConfigService service)
        => _service = service;

    /// <summary>
    /// Get field visibility config for this clinic.
    /// Returns all configurable fields; unset fields default to visible=true, required=false.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AssessmentFieldConfigDto>>> GetConfig(
        CancellationToken ct = default)
    {
        if (TenantId is null) return UnauthorizedError("Clinic context not found.");
        return FromResult(await _service.GetConfigAsync(TenantId.Value, ct));
    }

    /// <summary>
    /// Batch upsert field visibility/required for this clinic.
    /// Only send fields you want to change — unset fields keep their current value.
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<List<AssessmentFieldConfigDto>>> UpsertConfig(
        [FromBody] UpdateFieldConfigRequest request,
        CancellationToken ct = default)
    {
        if (TenantId is null) return UnauthorizedError("Clinic context not found.");
        return FromResult(await _service.UpsertConfigAsync(TenantId.Value, request.Fields, ct));
    }
}
