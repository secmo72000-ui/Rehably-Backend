using Rehably.Application.Common;
using Rehably.Application.DTOs.Role;

namespace Rehably.Application.Services.Clinic;

public interface IRoleManagementService
{
    Task<Result<List<RoleDto>>> GetRolesAsync(Guid? clinicId, CancellationToken cancellationToken = default);

    Task<Result<RoleDto>> GetRoleAsync(Guid clinicId, string roleName, CancellationToken cancellationToken = default);

    Task<Result<RoleDto>> CreateRoleAsync(Guid clinicId, CreateRoleRequest request, CancellationToken cancellationToken = default);

    Task<Result<RoleDto>> UpdateRoleAsync(Guid clinicId, string roleName, UpdateRoleRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteRoleAsync(Guid clinicId, string roleName, CancellationToken cancellationToken = default);

    Task<Result<List<PermissionDto>>> GetAvailablePermissionsAsync(Guid clinicId, CancellationToken cancellationToken = default);

    Task<Result> AssignPermissionToRoleAsync(Guid clinicId, string roleName, string permission, CancellationToken cancellationToken = default);

    Task<Result> RemovePermissionFromRoleAsync(Guid clinicId, string roleName, string permission, CancellationToken cancellationToken = default);
}
