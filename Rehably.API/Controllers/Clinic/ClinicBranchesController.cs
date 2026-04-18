using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.Application.DTOs.ClinicPortal;
using Rehably.Application.Services.ClinicPortal;

namespace Rehably.API.Controllers.Clinic;

[ApiController]
[Route("api/clinic/branches")]
[Authorize]
[Produces("application/json")]
[Tags("Clinic - Branches")]
public class ClinicBranchesController : BaseController
{
    private readonly IClinicBranchService _branchService;

    public ClinicBranchesController(IClinicBranchService branchService)
    {
        _branchService = branchService;
    }

    [HttpGet]
    public async Task<ActionResult<List<BranchDto>>> GetBranches(CancellationToken ct = default)
    {
        var clinicId = TenantId ?? Guid.Empty;
        var result   = await _branchService.GetBranchesAsync(clinicId, ct);
        return FromResult(result);
    }

    [HttpGet("{branchId:guid}")]
    public async Task<ActionResult<BranchDto>> GetBranch(Guid branchId, CancellationToken ct = default)
    {
        var clinicId = TenantId ?? Guid.Empty;
        var result   = await _branchService.GetBranchByIdAsync(clinicId, branchId, ct);
        return FromResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<BranchDto>> CreateBranch([FromBody] CreateBranchRequest request, CancellationToken ct = default)
    {
        var clinicId = TenantId ?? Guid.Empty;
        var result   = await _branchService.CreateBranchAsync(clinicId, request, ct);
        return FromResult(result, 201);
    }

    [HttpPut("{branchId:guid}")]
    public async Task<ActionResult<BranchDto>> UpdateBranch(Guid branchId, [FromBody] UpdateBranchRequest request, CancellationToken ct = default)
    {
        var clinicId = TenantId ?? Guid.Empty;
        var result   = await _branchService.UpdateBranchAsync(clinicId, branchId, request, ct);
        return FromResult(result);
    }

    [HttpDelete("{branchId:guid}")]
    public async Task<ActionResult> DeleteBranch(Guid branchId, CancellationToken ct = default)
    {
        var clinicId = TenantId ?? Guid.Empty;
        var result   = await _branchService.DeleteBranchAsync(clinicId, branchId, ct);
        return FromResult(result);
    }
}
