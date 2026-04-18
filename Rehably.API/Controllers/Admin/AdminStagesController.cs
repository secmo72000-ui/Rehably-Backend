using Microsoft.AspNetCore.Mvc;
using Rehably.API.Authorization;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Services.Library;

namespace Rehably.API.Controllers.Admin;

/// <summary>
/// Global treatment stage management for platform administrators.
/// </summary>
[ApiController]
[Route("api/admin/stages")]
[RequirePermission("library.manage")]
[Produces("application/json")]
[Tags("Admin - Stages")]
public class AdminStagesController : BaseController
{
    private readonly ITreatmentStageService _stageService;

    public AdminStagesController(ITreatmentStageService stageService)
    {
        _stageService = stageService;
    }

    /// <summary>
    /// Get all treatment stages with pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LibraryItemListResponse<TreatmentStageDto>>> GetAll(
        [FromQuery] Guid? bodyRegionId = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _stageService.GetStagesAsync(bodyRegionId, search, page, pageSize);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse.FailureResponse(result.Error ?? "Error", result.Error ?? "Error"));

        return Ok(ApiResponse<LibraryItemListResponse<TreatmentStageDto>>.SuccessResponse(result.Value));
    }

    /// <summary>
    /// Get a treatment stage by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TreatmentStageDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _stageService.GetStageByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(ApiResponse.FailureResponse("NOT_FOUND", result.Error ?? "Stage not found"));

        return Ok(ApiResponse<TreatmentStageDto>.SuccessResponse(result.Value));
    }

    /// <summary>
    /// Create a new treatment stage (global, no clinic scope).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TreatmentStageDto>> Create(
        [FromBody] CreateTreatmentStageRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _stageService.CreateStageAsync(request, Guid.Empty);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse.FailureResponse(result.Error ?? "Error", result.Error ?? "Error"));

        return StatusCode(201, ApiResponse<TreatmentStageDto>.SuccessResponse(result.Value));
    }

    /// <summary>
    /// Delete a treatment stage.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _stageService.DeleteStageAsync(id, Guid.Empty);
        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound(ApiResponse.FailureResponse("NOT_FOUND", result.Error));
            return BadRequest(ApiResponse.FailureResponse(result.Error ?? "Error", result.Error ?? "Error"));
        }

        return Ok(ApiResponse.SuccessResponse());
    }
}
