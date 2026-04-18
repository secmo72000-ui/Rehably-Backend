using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.API.Authorization;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Package;
using Rehably.Application.Services.Platform;

namespace Rehably.API.Controllers.Admin;

/// <summary>
/// Subscription package management.
/// </summary>
[ApiController]
[Route("api/admin/packages")]
[RequirePermission("platform.manage_packages")]
[Produces("application/json")]
[Tags("Admin - Packages")]
public class PackagesController : BaseController
{
    private readonly IPackageService _packageService;

    public PackagesController(IPackageService packageService)
    {
        _packageService = packageService;
    }

    /// <summary>
    /// Get all packages (active, draft, and archived)
    /// </summary>
    /// <returns>List of all packages</returns>
    /// <response code="200">Returns list of packages</response>
    /// <response code="400">Invalid request</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PackageDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<PackageDto>>> GetPackages(CancellationToken cancellationToken = default)
    {
        var result = await _packageService.GetPackagesAsync();
        return FromResult(result);
    }

    /// <summary>
    /// Get package by ID
    /// </summary>
    /// <param name="id">Package ID</param>
    /// <returns>Package details</returns>
    /// <response code="200">Returns the package</response>
    /// <response code="404">Package not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PackageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PackageDto>> GetPackage(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _packageService.GetPackageByIdAsync(id);
        return FromResult(result);
    }

    /// <summary>
    /// Create a new package (starts in Draft status)
    /// </summary>
    /// <param name="request">Package with name, features, and pricing</param>
    /// <returns>Created package</returns>
    /// <response code="201">Package created successfully</response>
    /// <response code="400">Invalid request (duplicate name, invalid features)</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PackageDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PackageDto>> CreatePackage([FromBody] CreatePackageRequestDto request, CancellationToken cancellationToken = default)
    {
        var result = await _packageService.CreatePackageAsync(request);
        return FromResult(result, 201);
    }

    /// <summary>
    /// Update package (only Draft packages can have features changed)
    /// </summary>
    /// <param name="id">Package ID</param>
    /// <param name="request">Updated package data</param>
    /// <returns>Updated package</returns>
    /// <response code="200">Package updated successfully</response>
    /// <response code="400">Invalid request or cannot modify active package</response>
    /// <response code="404">Package not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PackageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PackageDto>> UpdatePackage(Guid id, [FromBody] UpdatePackageRequestDto request, CancellationToken cancellationToken = default)
    {
        var result = await _packageService.UpdatePackageAsync(id, request);
        return FromResult(result);
    }

    /// <summary>
    /// Activate package (change status from Draft to Active). Makes it available for subscription.
    /// </summary>
    /// <param name="id">Package ID</param>
    /// <returns>Success message</returns>
    /// <response code="200">Package activated successfully</response>
    /// <response code="400">Cannot activate (already active, missing pricing, etc.)</response>
    /// <response code="404">Package not found</response>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ActivatePackage(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _packageService.ActivatePackageAsync(id);
        return FromResult(result);
    }

    /// <summary>
    /// Archive package (change status to Archived). Existing subscriptions continue but no new ones can be created.
    /// </summary>
    /// <param name="id">Package ID</param>
    /// <returns>Success message</returns>
    /// <response code="200">Package archived successfully</response>
    /// <response code="400">Cannot archive (already archived)</response>
    /// <response code="404">Package not found</response>
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ArchivePackage(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _packageService.ArchivePackageAsync(id);
        return FromResult(result);
    }
}
