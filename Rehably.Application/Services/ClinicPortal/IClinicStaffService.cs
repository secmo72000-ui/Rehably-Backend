using Rehably.Application.Common;
using Rehably.Application.DTOs.ClinicPortal;

namespace Rehably.Application.Services.ClinicPortal;

public interface IClinicStaffService
{
    Task<Result<PagedResult<StaffMemberDto>>> GetStaffAsync(Guid clinicId, StaffQueryParams query, CancellationToken ct = default);
    Task<Result<StaffMemberDto>> GetStaffByIdAsync(Guid clinicId, string userId, CancellationToken ct = default);
    Task<Result<StaffMemberDto>> InviteStaffAsync(Guid clinicId, InviteStaffRequest request, CancellationToken ct = default);
    Task<Result<StaffMemberDto>> UpdateStaffAsync(Guid clinicId, string userId, UpdateStaffRequest request, CancellationToken ct = default);
    Task<Result> DeactivateStaffAsync(Guid clinicId, string userId, CancellationToken ct = default);
    Task<Result> ReactivateStaffAsync(Guid clinicId, string userId, CancellationToken ct = default);
}
