using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.Application.DTOs.Role;
using Rehably.Application.Services.Clinic;

namespace Rehably.API.Controllers.Clinic;

[ApiController]
[Route("api/clinic/roles")]
[Authorize]
[Produces("application/json")]
[Tags("Clinic - Roles")]
public class ClinicRolesController : BaseController
{
    private readonly IRoleManagementService _roleService;

    public ClinicRolesController(IRoleManagementService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<ActionResult<List<RoleDto>>> GetRoles(CancellationToken ct = default)
    {
        var result = await _roleService.GetRolesAsync(TenantId, ct);
        return FromResult(result);
    }

    [HttpGet("permissions")]
    public async Task<ActionResult<List<PermissionDto>>> GetPermissions(CancellationToken ct = default)
    {
        var result = await _roleService.GetAvailablePermissionsAsync(TenantId ?? Guid.Empty, ct);
        return FromResult(result);
    }

    [HttpGet("{roleName}")]
    public async Task<ActionResult<RoleDto>> GetRole(string roleName, CancellationToken ct = default)
    {
        var result = await _roleService.GetRoleAsync(TenantId ?? Guid.Empty, roleName, ct);
        return FromResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleRequest request, CancellationToken ct = default)
    {
        var result = await _roleService.CreateRoleAsync(TenantId ?? Guid.Empty, request, ct);
        return FromResult(result, 201);
    }

    [HttpPut("{roleName}")]
    public async Task<ActionResult<RoleDto>> UpdateRole(string roleName, [FromBody] UpdateRoleRequest request, CancellationToken ct = default)
    {
        var result = await _roleService.UpdateRoleAsync(TenantId ?? Guid.Empty, roleName, request, ct);
        return FromResult(result);
    }

    [HttpDelete("{roleName}")]
    public async Task<ActionResult> DeleteRole(string roleName, CancellationToken ct = default)
    {
        var result = await _roleService.DeleteRoleAsync(TenantId ?? Guid.Empty, roleName, ct);
        return FromResult(result);
    }

    /// <summary>
    /// POST /api/clinic/roles/seed-defaults
    /// Creates the 5 standard clinic roles (skips any that already exist).
    /// Returns the list of role names that were newly created.
    /// </summary>
    [HttpPost("seed-defaults")]
    public async Task<ActionResult<List<string>>> SeedDefaults(CancellationToken ct = default)
    {
        var clinicId = TenantId ?? Guid.Empty;
        var result   = await _roleService.SeedDefaultRolesAsync(clinicId, ct);
        return FromResult(result);
    }

    [HttpPost("{roleName}/permissions/{permission}")]
    public async Task<ActionResult> AssignPermission(string roleName, string permission, CancellationToken ct = default)
    {
        var result = await _roleService.AssignPermissionToRoleAsync(TenantId ?? Guid.Empty, roleName, permission, ct);
        return FromResult(result);
    }

    [HttpDelete("{roleName}/permissions/{permission}")]
    public async Task<ActionResult> RemovePermission(string roleName, string permission, CancellationToken ct = default)
    {
        var result = await _roleService.RemovePermissionFromRoleAsync(TenantId ?? Guid.Empty, roleName, permission, ct);
        return FromResult(result);
    }
}
