using Rehably.Application.Common;
using Rehably.Application.DTOs.Admin;

namespace Rehably.Application.Services.Admin;

public interface IPlatformAdminService
{
    Task<Result<PagedResult<PlatformAdminResponse>>> GetAllAdminsAsync(int page = 1, int pageSize = 20);
    Task<Result<PlatformAdminResponse>> GetAdminByIdAsync(string userId);
}
