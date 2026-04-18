using Rehably.Application.Common;
using Rehably.Application.DTOs.ClinicPortal;

namespace Rehably.Application.Services.ClinicPortal;

public interface IClinicBranchService
{
    Task<Result<List<BranchDto>>> GetBranchesAsync(Guid clinicId, CancellationToken ct = default);
    Task<Result<BranchDto>> GetBranchByIdAsync(Guid clinicId, Guid branchId, CancellationToken ct = default);
    Task<Result<BranchDto>> CreateBranchAsync(Guid clinicId, CreateBranchRequest request, CancellationToken ct = default);
    Task<Result<BranchDto>> UpdateBranchAsync(Guid clinicId, Guid branchId, UpdateBranchRequest request, CancellationToken ct = default);
    Task<Result> DeleteBranchAsync(Guid clinicId, Guid branchId, CancellationToken ct = default);
}
