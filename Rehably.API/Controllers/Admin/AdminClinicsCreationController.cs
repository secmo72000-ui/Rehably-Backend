using Microsoft.AspNetCore.Mvc;
using Rehably.API.Authorization;
using Rehably.API.Controllers;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinic;
using Rehably.Application.Services.Clinic;

namespace Rehably.API.Controllers.Admin;

/// <summary>
/// Handles clinic creation with full activation workflow including subscription and payment.
/// </summary>
[ApiController]
[Route("api/admin/clinics")]
[RequirePermission("platform.manage_clinics")]
[Produces("application/json")]
[Tags("Admin - Clinics")]
public class AdminClinicsCreationController : BaseController
{
    private readonly IClinicActivationService _activationService;

    public AdminClinicsCreationController(IClinicActivationService activationService)
    {
        _activationService = activationService;
    }

    /// <summary>Create a new clinic with subscription and payment recording.</summary>
    /// <remarks>
    /// Orchestrates the full clinic activation saga:
    /// creates the clinic, attaches a subscription, records payment (skipped for Free),
    /// activates the clinic, and sends the welcome email.
    /// </remarks>
    /// <param name="request">The clinic creation request containing clinic details, subscription package, and payment information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">Clinic created and activated successfully.</response>
    /// <response code="400">Invalid request data or business rule violation.</response>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<ClinicCreatedDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ClinicCreatedDto>>> CreateClinic(
        [FromForm] CreateClinicRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _activationService.ActivateNewClinicAsync(request, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<ClinicCreatedDto>.FailureResponse(ErrorCodes.BusinessRuleViolation, result.Error ?? "Failed to create clinic"));

        return StatusCode(StatusCodes.Status201Created, ApiResponse<ClinicCreatedDto>.SuccessResponse(result.Value));
    }
}
