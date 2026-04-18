using Microsoft.AspNetCore.Mvc;
using Rehably.API.Authorization;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Services.Library;

namespace Rehably.API.Controllers.Admin;

/// <summary>
/// Global modality management for platform administrators.
/// </summary>
[ApiController]
[Route("api/admin/modalities")]
[RequirePermission("library.manage")]
[Produces("application/json")]
[Tags("Admin - Modalities")]
public class AdminModalitiesController : ControllerBase
{
    private readonly IModalityService _modalityService;

    public AdminModalitiesController(IModalityService modalityService)
    {
        _modalityService = modalityService;
    }

    /// <summary>
    /// Get all modalities with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(LibraryItemListResponse<ModalityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LibraryItemListResponse<ModalityDto>>> GetModalities(
        [FromQuery] Guid? bodyRegionId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _modalityService.GetModalitiesAsync(bodyRegionId, null, page, pageSize);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Get a specific modality by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ModalityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ModalityDto>> GetModality(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _modalityService.GetModalityByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Create a new global modality
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ModalityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ModalityDto>> CreateModality([FromBody] CreateModalityRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _modalityService.CreateModalityAsync(request, clinicId: null);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return CreatedAtAction(nameof(GetModality), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Update an existing modality
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ModalityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ModalityDto>> UpdateModality(Guid id, [FromBody] UpdateModalityRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _modalityService.UpdateModalityAsync(id, request, Guid.Empty);
        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Delete a modality (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteModality(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _modalityService.DeleteModalityAsync(id, Guid.Empty);
        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }
        return NoContent();
    }
}
