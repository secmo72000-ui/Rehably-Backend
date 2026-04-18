using Microsoft.AspNetCore.Mvc;
using Rehably.API.Authorization;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Platform;
using Rehably.Application.DTOs.Clinic;
using Rehably.Application.Services.Clinic;
using Rehably.Domain.Enums;

namespace Rehably.API.Controllers.Admin;

/// <summary>
/// Basic CRUD operations for clinic management.
/// </summary>
[ApiController]
[Route("api/admin/clinics")]
[RequirePermission("platform.manage_clinics")]
[Produces("application/json")]
[Tags("Admin - Clinics CRUD")]
public class AdminClinicsCrudController : BaseController
{
    private readonly IClinicService _clinicService;

    public AdminClinicsCrudController(IClinicService clinicService)
    {
        _clinicService = clinicService;
    }

    /// <summary>
    /// Get all clinics with search and filtering (supports Arabic text)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ClinicResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ClinicResponse>>> GetClinics([FromQuery] GetClinicsQuery query, CancellationToken cancellationToken = default)
    {
        var result = await _clinicService.SearchClinicsAsync(query);
        return FromResult(result);
    }

    /// <summary>
    /// Get clinic by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ClinicResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClinicResponse>> GetClinic(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _clinicService.GetClinicByIdAsync(id);
        return FromResult(result);
    }

    /// <summary>
    /// Update clinic information
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ClinicResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClinicResponse>> UpdateClinic(Guid id, [FromBody] UpdateClinicRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _clinicService.UpdateClinicAsync(id, request);
        return FromResult(result);
    }

    /// <summary>
    /// Delete clinic (soft delete). Data is preserved for audit purposes.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteClinic(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _clinicService.DeleteClinicAsync(id);
        return FromResult(result, 204);
    }

    /// <summary>
    /// Suspend clinic operations. Users can still log in but cannot perform operations.
    /// </summary>
    [HttpPost("{id:guid}/suspend")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SuspendClinic(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _clinicService.SuspendClinicAsync(id);
        return FromResult(result);
    }

    /// <summary>
    /// Activate clinic (resume operations after suspension)
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ActivateClinic(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _clinicService.ActivateClinicAsync(id);
        return FromResult(result);
    }

    /// <summary>
    /// Ban clinic (terminates all user sessions immediately)
    /// </summary>
    [HttpPost("{id:guid}/ban")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> BanClinic(Guid id, [FromBody] BanClinicRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _clinicService.BanClinicAsync(id, request.Reason, CurrentAdminId.ToString());
        return FromResult(result);
    }

    /// <summary>
    /// Unban clinic (requires super_admin permission)
    /// </summary>
    [HttpPost("{id:guid}/unban")]
    [RequirePermission("platform.super_admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UnbanClinic(Guid id, [FromBody] UnbanClinicRequest? request, CancellationToken cancellationToken = default)
    {
        var result = await _clinicService.UnbanClinicAsync(id, request?.Reason, CurrentAdminId.ToString());
        return FromResult(result);
    }
}
