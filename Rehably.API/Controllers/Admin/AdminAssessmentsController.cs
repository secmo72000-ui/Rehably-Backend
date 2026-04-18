using Microsoft.AspNetCore.Mvc;
using Rehably.API.Authorization;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Services.Library;

namespace Rehably.API.Controllers.Admin;

/// <summary>
/// Global assessment management for platform administrators.
/// </summary>
[ApiController]
[Route("api/admin/assessments")]
[RequirePermission("library.manage")]
[Produces("application/json")]
[Tags("Admin - Assessments")]
public class AdminAssessmentsController : ControllerBase
{
    private readonly IAssessmentService _assessmentService;

    public AdminAssessmentsController(IAssessmentService assessmentService)
    {
        _assessmentService = assessmentService;
    }

    /// <summary>
    /// Get all assessments with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(LibraryItemListResponse<AssessmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LibraryItemListResponse<AssessmentDto>>> GetAssessments(
        [FromQuery] Guid? bodyRegionId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _assessmentService.GetAssessmentsAsync(bodyRegionId, null, page, pageSize);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Get a specific assessment by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AssessmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssessmentDto>> GetAssessment(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _assessmentService.GetAssessmentByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Create a new global assessment
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AssessmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssessmentDto>> CreateAssessment([FromBody] CreateAssessmentRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _assessmentService.CreateAssessmentAsync(request, clinicId: null);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return CreatedAtAction(nameof(GetAssessment), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Update an existing assessment
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AssessmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssessmentDto>> UpdateAssessment(Guid id, [FromBody] UpdateAssessmentRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _assessmentService.UpdateAssessmentAsync(id, request, Guid.Empty);
        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Delete an assessment (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAssessment(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _assessmentService.DeleteAssessmentAsync(id, Guid.Empty);
        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }
        return NoContent();
    }
}
