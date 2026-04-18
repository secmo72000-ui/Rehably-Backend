using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.API.Authorization;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Admin;
using Rehably.Application.Services.Admin;
using System.Security.Claims;

namespace Rehably.API.Controllers.Admin;

/// <summary>
/// Platform role management for system administration.
/// </summary>
/// <remarks>
/// Manages platform-level roles and their associated permissions. Key features:
/// - Create/update/delete roles
/// - Assign permissions to roles
/// - View role hierarchy and permissions
/// - Control access to admin features
/// </remarks>
[ApiController]
[Route("api/admin/roles")]
[Authorize]
[Produces("application/json")]
[Tags("Admin - Roles")]
public class PlatformRolesController : BaseController
{
    private readonly IPlatformRoleService _roleService;
    private readonly IPlatformRoleManagementService _roleManagementService;

    public PlatformRolesController(
        IPlatformRoleService roleService,
        IPlatformRoleManagementService roleManagementService)
    {
        _roleService = roleService;
        _roleManagementService = roleManagementService;
    }

    /// <summary>
    /// Get all platform roles
    /// </summary>
    /// <returns>List of all platform roles</returns>
    /// <response code="200">Returns list of roles</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [RequirePermission("roles.view")]
    [ProducesResponseType(typeof(ApiResponse<List<PlatformRoleResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<PlatformRoleResponse>>> GetAll(CancellationToken cancellationToken = default)
    {
        var result = await _roleService.GetAllRolesAsync();
        return FromResult(result);
    }

    /// <summary>
    /// Create a new platform role
    /// </summary>
    /// <param name="request">Role creation request with name, description, and permissions</param>
    /// <returns>Created role</returns>
    /// <response code="201">Role created successfully</response>
    /// <response code="400">Invalid request (duplicate name, missing fields)</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost]
    [RequirePermission("roles.create")]
    [ProducesResponseType(typeof(ApiResponse<PlatformRoleResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PlatformRoleResponse>> Create(CreatePlatformRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationError("Invalid request data");

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _roleManagementService.CreateRoleAsync(request, currentUserId);
        return FromResult(result, 201);
    }

    /// <summary>
    /// Update platform role
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <param name="request">Updated role data with name, description, and permissions</param>
    /// <returns>Updated role</returns>
    /// <response code="200">Role updated successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Role not found</response>
    [HttpPut("{id:guid}")]
    [RequirePermission("roles.update")]
    [ProducesResponseType(typeof(ApiResponse<PlatformRoleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlatformRoleResponse>> Update(string id, UpdatePlatformRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationError("Invalid request data");

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _roleManagementService.UpdateRoleAsync(id, request, currentUserId);
        return FromResult(result);
    }

    /// <summary>
    /// Delete platform role
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <returns>No content on success</returns>
    /// <response code="200">Role deleted successfully</response>
    /// <response code="400">Cannot delete (e.g., role in use or system role)</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Role not found</response>
    [HttpDelete("{id:guid}")]
    [RequirePermission("roles.delete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(string id, CancellationToken cancellationToken = default)
    {
        var result = await _roleManagementService.DeleteRoleAsync(id);
        return FromResult(result);
    }
}
