using Rehably.Application.Common;
using Rehably.Application.DTOs.Admin;

namespace Rehably.Application.Services.Admin;

public interface IPlatformRoleManagementService
{
    Task<Result<PlatformRoleResponse>> CreateRoleAsync(CreatePlatformRoleRequest request, string currentUserId);
    Task<Result<PlatformRoleResponse>> UpdateRoleAsync(string roleId, UpdatePlatformRoleRequest request, string currentUserId);
    Task<Result> DeleteRoleAsync(string roleId);
}
