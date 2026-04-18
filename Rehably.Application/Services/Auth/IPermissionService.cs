using Rehably.Application.DTOs.Permission;

namespace Rehably.Application.Services.Auth;

public interface IPermissionService
{
    Task InvalidateUserPermissionsAsync(string userId, string tenantId, CancellationToken cancellationToken = default);

    Task<PermissionDto[]> GetAllPermissionsAsync(CancellationToken cancellationToken = default);
    Task<RolePermissionDto[]> GetRolePermissionsAsync(string roleName, CancellationToken cancellationToken = default);
    Task AssignPermissionToRoleAsync(string roleName, string permission, CancellationToken cancellationToken = default);
    Task RemovePermissionFromRoleAsync(string roleName, string permission, CancellationToken cancellationToken = default);
}
