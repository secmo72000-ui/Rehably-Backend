using Microsoft.AspNetCore.Mvc;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Package;
using Rehably.Application.Services.Platform;
using Rehably.API.Controllers;

namespace Rehably.API.Controllers.Public;

/// <summary>
/// Public package listing endpoints (no authentication required).
/// </summary>
[ApiController]
[Route("api/public/packages")]
[Produces("application/json")]
[Tags("Public - Packages")]
public class PackagesController : BaseController
{
    private readonly IPackageService _packageService;

    public PackagesController(IPackageService packageService)
    {
        _packageService = packageService;
    }

    /// <summary>
    /// Get all active public packages ordered by price ascending.
    /// </summary>
    /// <returns>List of active public packages.</returns>
    /// <response code="200">Returns all active public packages.</response>
    /// <response code="400">Failed to load packages.</response>
    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(List<PublicPackageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<PublicPackageDto>>> GetPublicPackages(CancellationToken cancellationToken = default)
    {
        var result = await _packageService.GetPublicPackagesAsync();
        if (!result.IsSuccess)
            return BadRequest(ApiResponse.FailureResponse(result.Error));
        return Ok(ApiResponse<List<PublicPackageDto>>.SuccessResponse(result.Value!));
    }
}
