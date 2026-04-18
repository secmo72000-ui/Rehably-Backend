using Rehably.Application.Common;
using Rehably.Application.DTOs.Admin;

namespace Rehably.Application.Services.Admin;

public interface IPlatformRoleService
{
    Task<Result<List<PlatformRoleResponse>>> GetAllRolesAsync();
    Task<Result<PlatformRoleResponse>> GetRoleByIdAsync(string roleId);
    Task<Result<PlatformPermissionMatrixResponse>> GetAvailablePermissionsAsync();
}
