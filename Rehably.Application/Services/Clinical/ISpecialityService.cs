using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinical;

namespace Rehably.Application.Services.Clinical;

public interface ISpecialityService
{
    // ── Global (super-admin) ───────────────────────────────────────────────────
    Task<Result<List<SpecialityDto>>> GetAllAsync(bool activeOnly = false, CancellationToken ct = default);
    Task<Result<SpecialityDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<SpecialityDto>> CreateAsync(CreateSpecialityRequest request, CancellationToken ct = default);
    Task<Result<SpecialityDto>> UpdateAsync(Guid id, UpdateSpecialityRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);

    // ── Clinic assignment ──────────────────────────────────────────────────────
    Task<Result<List<ClinicSpecialityDto>>> GetClinicSpecialitiesAsync(Guid clinicId, CancellationToken ct = default);
    Task<Result> AssignToClinicAsync(Guid clinicId, AssignSpecialitiesRequest request, CancellationToken ct = default);
    Task<Result> RemoveFromClinicAsync(Guid clinicId, Guid specialityId, CancellationToken ct = default);
}
