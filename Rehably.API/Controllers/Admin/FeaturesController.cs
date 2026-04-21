using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.API.Authorization;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Feature;
using Rehably.Application.Services.Platform;

namespace Rehably.API.Controllers.Admin;

/// <summary>
/// Feature management for subscription packages.
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

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<FeatureDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FeatureDto>>> GetFeatures([FromQuery] Guid? categoryId = null, CancellationToken cancellationToken = default)
    {
        var result = await _featureService.GetFeaturesAsync(categoryId);
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FeatureDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeatureDto>> GetFeature(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _featureService.GetFeatureByIdAsync(id);
        return FromResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<FeatureDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FeatureDto>> CreateFeature([FromBody] CreateFeatureRequestDto request, CancellationToken cancellationToken = default)
    {
        var result = await _featureService.CreateFeatureAsync(request);
        return FromResult(result, 201);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FeatureDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeatureDto>> UpdateFeature(Guid id, [FromBody] UpdateFeatureRequestDto request, CancellationToken cancellationToken = default)
    {
        var result = await _featureService.UpdateFeatureAsync(id, request);
        return FromResult(result);
    }

    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeactivateFeature(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _featureService.DeactivateFeatureAsync(id);
        return FromResult(result);
    }
}
