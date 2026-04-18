using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.API.Authorization;
using Rehably.Application.DTOs.Admin;
using Rehably.Application.Services.Admin;
using Rehably.Application.Common;
using System.Security.Claims;

namespace Rehably.API.Controllers.Admin;

/// <summary>
/// Platform administrator user management.
/// </summary>
/// <remarks>
/// Manages platform-level administrators who have elevated privileges across all clinics.
/// Key features:
/// - Create/update/delete platform admins
/// - Assign platform roles (Super Admin, Admin, Support)
/// - View all platform user accounts
/// - Role-based access control via RequirePermission
/// </remarks>
[ApiController]
[Route("api/admin/platform-users")]
[Authorize]
[Produces("application/json")]
[Tags("Admin - Platform Users")]
public class PlatformAdminsController : BaseController
{
    private readonly IPlatformAdminService _adminService;
    private readonly IPlatformAdminManagementService _adminManagementService;

    public PlatformAdminsController(
        IPlatformAdminService adminService,
        IPlatformAdminManagementService adminManagementService)
    {
        _adminService = adminService;
        _adminManagementService = adminManagementService;
    }

    /// <summary>
    /// Get all platform administrators
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <returns>Paged list of platform administrators</returns>
    /// <response code="200">Returns paged list of platform administrators</response>
    /// <response code="400">Invalid request</response>
    [HttpGet]
    [RequirePermission("platform_users.view")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PlatformAdminResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<PlatformAdminResponse>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _adminService.GetAllAdminsAsync(page, pageSize);
        return FromResult(result);
    }

    /// <summary>
    /// Create a new platform administrator
    /// </summary>
    /// <param name="request">Administrator creation request with email, name, and initial role</param>
    /// <returns>Created administrator</returns>
    /// <response code="201">Administrator created successfully</response>
    /// <response code="400">Invalid request (duplicate email, missing fields)</response>
    [HttpPost]
    [RequirePermission("platform_users.create")]
    [ProducesResponseType(typeof(ApiResponse<PlatformAdminResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlatformAdminResponse>> Create(CreatePlatformAdminRequest request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationError("Invalid request data");

        var result = await _adminManagementService.CreateAdminAsync(request);
        return FromResult(result, 201);
    }

    /// <summary>
    /// Update platform administrator
    /// </summary>
    /// <param name="id">Administrator ID</param>
    /// <param name="request">Updated administrator data</param>
    /// <returns>Updated administrator</returns>
    /// <response code="200">Administrator updated successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="404">Administrator not found</response>
    [HttpPut("{id:guid}")]
    [RequirePermission("platform_users.update")]
    [ProducesResponseType(typeof(ApiResponse<PlatformAdminResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlatformAdminResponse>> Update(string id, UpdatePlatformAdminRequest request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationError("Invalid request data");

        var result = await _adminManagementService.UpdateAdminAsync(id, request);
        return FromResult(result);
    }

    /// <summary>
    /// Change administrator role
    /// </summary>
    /// <param name="id">Administrator ID</param>
    /// <param name="request">New role assignment</param>
    /// <returns>No content on success</returns>
    /// <response code="200">Role changed successfully</response>
    /// <response code="400">Invalid role or cannot change own role</response>
    /// <response code="404">Administrator not found</response>
    [HttpPut("{id:guid}/role")]
    [RequirePermission("platform_users.update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ChangeRole(string id, ChangeAdminRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationError("Invalid request data");

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _adminManagementService.ChangeAdminRoleAsync(id, request, currentUserId);
        return FromResult(result);
    }

    /// <summary>
    /// Delete platform administrator
    /// </summary>
    /// <param name="id">Administrator ID</param>
    /// <returns>No content on success</returns>
    /// <response code="200">Administrator deleted successfully</response>
    /// <response code="400">Cannot delete own account or last super admin</response>
    /// <response code="404">Administrator not found</response>
    [HttpDelete("{id:guid}")]
    [RequirePermission("platform_users.delete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(string id, CancellationToken cancellationToken = default)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _adminManagementService.DeleteAdminAsync(id, currentUserId);
        return FromResult(result);
    }
}
