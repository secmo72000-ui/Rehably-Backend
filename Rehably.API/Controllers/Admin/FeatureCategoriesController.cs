using Microsoft.AspNetCore.Mvc;
using Rehably.API.Authorization;
using Rehably.Application.DTOs.Feature;
using Rehably.Application.Services.Platform;

namespace Rehably.API.Controllers.Admin;

/// <summary>
/// Read-only access to feature categories for the platform.
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

    /// <summary>
    /// Get all feature categories.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FeatureCategoryDto>>> GetCategories(CancellationToken cancellationToken = default)
    {
        var result = await _categoryService.GetCategoriesAsync();
        return FromResult(result);
    }

    /// <summary>
    /// Get a feature category with all details including subcategories and features.
    /// </summary>
    [HttpGet("{id:guid}/details")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeatureCategoryDetailDto>> GetCategoryWithDetails(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _categoryService.GetCategoryWithDetailsAsync(id);
        return FromResult(result);
    }
}
