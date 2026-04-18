using Microsoft.AspNetCore.Mvc;
using Rehably.API.Authorization;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Services.Library;

namespace Rehably.API.Controllers.Admin;

/// <summary>
/// Global exercise management for platform administrators.
/// </summary>
[ApiController]
[Route("api/admin/exercises")]
[RequirePermission("library.manage")]
[Produces("application/json")]
[Tags("Admin - Exercises")]
public class AdminExercisesController : ControllerBase
{
    private readonly IExerciseService _exerciseService;

    public AdminExercisesController(IExerciseService exerciseService)
    {
        _exerciseService = exerciseService;
    }

    /// <summary>
    /// Get all exercises with pagination and optional body region filter
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(LibraryItemListResponse<ExerciseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LibraryItemListResponse<ExerciseDto>>> GetExercises(
        [FromQuery] Guid? bodyRegionId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _exerciseService.GetExercisesAsync(bodyRegionId, null, page, pageSize);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Get a specific exercise by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ExerciseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseDto>> GetExercise(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _exerciseService.GetExerciseByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Create a new global exercise
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(104_857_600)]
    [ProducesResponseType(typeof(ExerciseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExerciseDto>> CreateExercise([FromForm] CreateExerciseRequest request, IFormFile? video, IFormFile? thumbnail, CancellationToken cancellationToken = default)
    {
        var dtoWithStreams = request with
        {
            VideoStream = video?.OpenReadStream(),
            VideoFileName = video?.FileName,
            VideoContentType = video?.ContentType,
            ThumbnailStream = thumbnail?.OpenReadStream(),
            ThumbnailFileName = thumbnail?.FileName,
            ThumbnailContentType = thumbnail?.ContentType
        };

        var result = await _exerciseService.CreateExerciseAsync(dtoWithStreams, clinicId: null);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return CreatedAtAction(nameof(GetExercise), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Update an existing exercise
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequestSizeLimit(104_857_600)]
    [ProducesResponseType(typeof(ExerciseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseDto>> UpdateExercise(Guid id, [FromForm] UpdateExerciseRequest request, IFormFile? video, IFormFile? thumbnail, CancellationToken cancellationToken = default)
    {
        var dtoWithStreams = request with
        {
            VideoStream = video?.OpenReadStream(),
            VideoFileName = video?.FileName,
            VideoContentType = video?.ContentType,
            ThumbnailStream = thumbnail?.OpenReadStream(),
            ThumbnailFileName = thumbnail?.FileName,
            ThumbnailContentType = thumbnail?.ContentType
        };

        var result = await _exerciseService.UpdateExerciseAsync(id, dtoWithStreams, Guid.Empty);
        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Delete an exercise (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteExercise(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _exerciseService.DeleteExerciseAsync(id, Guid.Empty);
        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }
        return NoContent();
    }
}
