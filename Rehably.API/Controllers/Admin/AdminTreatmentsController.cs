using Microsoft.AspNetCore.Mvc;
using Rehably.API.Authorization;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Services.Library;

namespace Rehably.API.Controllers.Admin;

/// <summary>
/// Global treatment management for platform administrators.
/// </summary>
[ApiController]
[Route("api/admin/treatments")]
[RequirePermission("library.manage")]
[Produces("application/json")]
[Tags("Admin - Treatments")]
public class AdminTreatmentsController : ControllerBase
{
    private readonly ITreatmentService _treatmentService;

    public AdminTreatmentsController(ITreatmentService treatmentService)
    {
        _treatmentService = treatmentService;
    }

    /// <summary>
    /// Get all treatments with pagination and optional body region filter
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(LibraryItemListResponse<TreatmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LibraryItemListResponse<TreatmentDto>>> GetTreatments(
        [FromQuery] Guid? bodyRegionId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _treatmentService.GetTreatmentsAsync(bodyRegionId, null, page, pageSize);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Get a specific treatment by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TreatmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TreatmentDto>> GetTreatment(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _treatmentService.GetTreatmentByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Create a new global treatment
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TreatmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TreatmentDto>> CreateTreatment([FromBody] CreateTreatmentRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _treatmentService.CreateTreatmentAsync(request, clinicId: null);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return CreatedAtAction(nameof(GetTreatment), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Update an existing treatment
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TreatmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TreatmentDto>> UpdateTreatment(Guid id, [FromBody] UpdateTreatmentRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _treatmentService.UpdateTreatmentAsync(id, request, Guid.Empty);
        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Delete a treatment (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteTreatment(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _treatmentService.DeleteTreatmentAsync(id, Guid.Empty);
        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }
        return NoContent();
    }
}
