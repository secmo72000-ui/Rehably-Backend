using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.Application.Common;
using Rehably.Application.DTOs.ClinicPortal;
using Rehably.Application.Services.ClinicPortal;

namespace Rehably.API.Controllers.Clinic;

[ApiController]
[Route("api/clinic/staff")]
[Authorize]
[Produces("application/json")]
[Tags("Clinic - Staff")]
public class ClinicStaffController : BaseController
{
    private readonly IClinicStaffService _staffService;

    public ClinicStaffController(IClinicStaffService staffService)
    {
        _staffService = staffService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<StaffMemberDto>>> GetStaff([FromQuery] StaffQueryParams query, CancellationToken ct = default)
    {
        var result = await _staffService.GetStaffAsync(TenantId ?? Guid.Empty, query, ct);
        return FromResult(result);
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<StaffMemberDto>> GetStaffMember(string userId, CancellationToken ct = default)
    {
        var result = await _staffService.GetStaffByIdAsync(TenantId ?? Guid.Empty, userId, ct);
        return FromResult(result);
    }

    [HttpPost("invite")]
    public async Task<ActionResult<StaffMemberDto>> InviteStaff([FromBody] InviteStaffRequest request, CancellationToken ct = default)
    {
        var result = await _staffService.InviteStaffAsync(TenantId ?? Guid.Empty, request, ct);
        return FromResult(result, 201);
    }

    [HttpPut("{userId}")]
    public async Task<ActionResult<StaffMemberDto>> UpdateStaff(string userId, [FromBody] UpdateStaffRequest request, CancellationToken ct = default)
    {
        var result = await _staffService.UpdateStaffAsync(TenantId ?? Guid.Empty, userId, request, ct);
        return FromResult(result);
    }

    [HttpPost("{userId}/deactivate")]
    public async Task<ActionResult> Deactivate(string userId, CancellationToken ct = default)
    {
        var result = await _staffService.DeactivateStaffAsync(TenantId ?? Guid.Empty, userId, ct);
        return FromResult(result);
    }

    [HttpPost("{userId}/reactivate")]
    public async Task<ActionResult> Reactivate(string userId, CancellationToken ct = default)
    {
        var result = await _staffService.ReactivateStaffAsync(TenantId ?? Guid.Empty, userId, ct);
        return FromResult(result);
    }
}
