using Rehably.Application.Common;
using Rehably.Application.DTOs.Platform;

namespace Rehably.Application.Services.Platform;

public interface IAdminDashboardService
{
    Task<Result<AdminDashboardDto>> GetDashboardAsync(CancellationToken ct = default);
}
