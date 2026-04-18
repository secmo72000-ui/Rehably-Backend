using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.Application.Common;
using Rehably.Application.DTOs.ClinicPortal;
using Rehably.Application.Services.ClinicPortal;

namespace Rehably.API.Controllers.Clinic;

[ApiController]
[Route("api/clinic")]
[Authorize]
[Produces("application/json")]
[Tags("Clinic - Dashboard")]
public class ClinicDashboardController : BaseController
{
    private readonly IClinicDashboardService _dashboardService;

    public ClinicDashboardController(IClinicDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Get clinic dashboard statistics and today's schedule.
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<ClinicDashboardDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClinicDashboardDto>> GetDashboard(CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetDashboardAsync(cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Get clinic profile information.
    /// </summary>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(ApiResponse<ClinicProfileDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClinicProfileDto>> GetProfile(CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetProfileAsync(cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Update clinic profile information.
    /// </summary>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(ApiResponse<ClinicProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClinicProfileDto>> UpdateProfile(
        [FromBody] UpdateClinicProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.UpdateProfileAsync(request, cancellationToken);
        return FromResult(result);
    }
}
