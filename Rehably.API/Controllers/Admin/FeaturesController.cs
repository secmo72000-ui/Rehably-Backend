using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.API.Authorization;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Feature;
using Rehably.Application.Services.Platform;

namespace Rehably.API.Controllers.Admin;

/// <summary>
/// Feature listing for subscription packages.
/// </summary>
[ApiController]
[Route("api/admin/features")]
[RequirePermission("platform.manage_features")]
[Produces("application/json")]
[Tags("Admin - Features")]
public class FeaturesController : BaseController
{
    private readonly IFeatureService _featureService;

    public FeaturesController(IFeatureService featureService)
    {
        _featureService = featureService;
    }

    /// <summary>
    /// Get all features, optionally filtered by category
    /// </summary>
    /// <param name="categoryId">Optional category ID to filter features</param>
    /// <returns>List of features</returns>
    /// <response code="200">Returns list of features</response>
    /// <response code="400">Invalid request</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<FeatureDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<FeatureDto>>> GetFeatures([FromQuery] Guid? categoryId = null, CancellationToken cancellationToken = default)
    {
        var result = await _featureService.GetFeaturesAsync(categoryId);
        return FromResult(result);
    }
}
