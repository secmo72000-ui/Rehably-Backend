using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.Application.DTOs.ClinicPortal;
using Rehably.Application.Services.ClinicPortal;

namespace Rehably.API.Controllers.Clinic;

[ApiController]
[Route("api/clinic/working-hours")]
[Authorize]
[Produces("application/json")]
[Tags("Clinic - Working Hours")]
public class ClinicWorkingHoursController : BaseController
{
    private readonly IClinicWorkingHoursService _service;

    public ClinicWorkingHoursController(IClinicWorkingHoursService service)
        => _service = service;

    /// <summary>
    /// GET /api/clinic/working-hours
    /// Returns all 7 days. Creates default schedule on first call.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<WorkingHoursDayDto>>> Get(CancellationToken ct = default)
    {
        var clinicId = TenantId ?? Guid.Empty;
        var result   = await _service.GetAsync(clinicId, ct);
        return Ok(result);
    }

    /// <summary>
    /// PUT /api/clinic/working-hours
    /// Upserts the full weekly schedule.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateWorkingHoursRequest request, CancellationToken ct = default)
    {
        var clinicId = TenantId ?? Guid.Empty;
        await _service.UpdateAsync(clinicId, request, ct);
        return NoContent();
    }
}
