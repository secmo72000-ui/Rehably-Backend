using Microsoft.AspNetCore.Mvc;
using Rehably.API.Authorization;
using Rehably.Application.DTOs.AddOn;
using Rehably.Application.DTOs.Subscription;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Enums;

namespace Rehably.API.Controllers.Admin;

/// <summary>
/// Admin management of clinic add-ons.
/// </summary>
[ApiController]
[Route("api/admin/clinics/{clinicId:guid}/addons")]
[RequirePermission("platform.manage_subscriptions")]
[Produces("application/json")]
[Tags("Admin - Add-Ons")]
public class AddOnsController : BaseController
{
    private readonly IAddOnService _addOnService;

    public AddOnsController(IAddOnService addOnService)
    {
        _addOnService = addOnService;
    }

    /// <summary>
    /// List all add-ons for a clinic, with optional status filter.
    /// </summary>
    /// <param name="clinicId">Clinic identifier</param>
    /// <param name="status">Optional status filter</param>
    [HttpGet]
    [ProducesResponseType(typeof(List<AddOnDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AddOnDto>>> GetAddOns(Guid clinicId, [FromQuery] AddOnStatus? status = null)
    {
        var result = await _addOnService.GetClinicAddOnsAsync(clinicId, status);
        return FromResult(result);
    }

    /// <summary>
    /// Get available add-on features for a clinic (from its subscription package).
    /// </summary>
    [HttpGet("available")]
    [ProducesResponseType(typeof(List<AvailableAddOnDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AvailableAddOnDto>>> GetAvailableAddOns(Guid clinicId)
    {
        var result = await _addOnService.GetAvailableAddOnsAsync(clinicId);
        return FromResult(result);
    }

    /// <summary>
    /// Create a new add-on for a clinic.
    /// </summary>
    /// <param name="clinicId">Clinic identifier</param>
    /// <param name="request">Add-on details</param>
    [HttpPost]
    [ProducesResponseType(typeof(AddOnDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddOnDto>> CreateAddOn(Guid clinicId, [FromBody] CreateAddOnRequestDto request)
    {
        var result = await _addOnService.CreateAddOnAsync(clinicId, request);
        return FromResult(result, successStatusCode: 201);
    }

    /// <summary>
    /// Cancel an active add-on for a clinic.
    /// </summary>
    /// <param name="clinicId">Clinic identifier</param>
    /// <param name="addOnId">Add-on identifier</param>
    [HttpPost("{addOnId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CancelAddOn(Guid clinicId, Guid addOnId)
    {
        var result = await _addOnService.CancelAddOnAsync(clinicId, addOnId);
        return FromResult(result);
    }
}
