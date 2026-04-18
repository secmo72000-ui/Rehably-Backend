using Microsoft.AspNetCore.Mvc;
using Rehably.API.Authorization;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Services.Library;

namespace Rehably.API.Controllers.Admin;

/// <summary>
/// Global body region management for platform administrators.
/// </summary>
[ApiController]
[Route("api/admin/library")]
[RequirePermission("library.manage")]
[Produces("application/json")]
[Tags("Admin - Library")]
public class AdminBodyRegionsController : ControllerBase
{
    private readonly IBodyRegionService _bodyRegionService;

    public AdminBodyRegionsController(IBodyRegionService bodyRegionService)
    {
        _bodyRegionService = bodyRegionService;
    }

    /// <summary>
    /// Get all body region categories with their body regions
    /// </summary>
    /// <returns>List of body region categories</returns>
    [HttpGet("body-regions")]
    [ProducesResponseType(typeof(List<BodyRegionCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<BodyRegionCategoryDto>>> GetBodyRegions(CancellationToken cancellationToken = default)
    {
        var result = await _bodyRegionService.GetBodyRegionsAsync();
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
