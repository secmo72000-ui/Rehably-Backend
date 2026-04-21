using Microsoft.AspNetCore.Mvc;
using Rehably.API.Authorization;
using Rehably.Application.DTOs.Feature;
using Rehably.Application.Services.Platform;

namespace Rehably.API.Controllers.Admin;

/// <summary>
/// Feature category management for subscription packages.
/// </summary>
[ApiController]
[Route("api/admin/feature-categories")]
[Produces("application/json")]
[Tags("Admin - Feature Categories")]
[RequirePermission("platform.manage_feature_categories")]
public class FeatureCategoriesController : BaseController
{
    private readonly IFeatureCategoryService _categoryService;

    public FeatureCategoriesController(IFeatureCategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FeatureCategoryDto>>> GetCategories(CancellationToken cancellationToken = default)
    {
        var result = await _categoryService.GetCategoriesAsync();
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeatureCategoryDto>> GetCategory(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _categoryService.GetCategoryByIdAsync(id);
        return FromResult(result);
    }

    [HttpGet("{id:guid}/details")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeatureCategoryDetailDto>> GetCategoryWithDetails(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _categoryService.GetCategoryWithDetailsAsync(id);
        return FromResult(result);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FeatureCategoryDto>> CreateCategory([FromBody] CreateFeatureCategoryRequestDto request, CancellationToken cancellationToken = default)
    {
        var result = await _categoryService.CreateCategoryAsync(request);
        return FromResult(result, 201);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeatureCategoryDto>> UpdateCategory(Guid id, [FromBody] UpdateFeatureCategoryRequestDto request, CancellationToken cancellationToken = default)
    {
        var result = await _categoryService.UpdateCategoryAsync(id, request);
        return FromResult(result);
    }

    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeactivateCategory(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _categoryService.DeactivateCategoryAsync(id);
        return FromResult(result);
    }
}
