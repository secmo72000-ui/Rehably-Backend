using Rehably.Application.Common;
using Rehably.Application.DTOs.ClinicPortal;

namespace Rehably.Application.Services.ClinicPortal;

public interface IClinicDashboardService
{
    Task<Result<ClinicDashboardDto>> GetDashboardAsync(CancellationToken ct = default);
    Task<Result<ClinicProfileDto>> GetProfileAsync(CancellationToken ct = default);
    Task<Result<ClinicProfileDto>> UpdateProfileAsync(UpdateClinicProfileRequest request, CancellationToken ct = default);
}
