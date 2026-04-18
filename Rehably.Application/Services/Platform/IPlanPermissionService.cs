namespace Rehably.Application.Services.Platform;

public interface IPlanPermissionService
{
    Task<bool> CanClinicUsePermissionAsync(Guid clinicId, string permission, CancellationToken cancellationToken = default);

    Task<string[]> GetAvailablePermissionsAsync(Guid clinicId, CancellationToken cancellationToken = default);

    Task<bool> DoesPlanAllowPermissionAsync(Guid planId, string permission, CancellationToken cancellationToken = default);

    bool PermissionMatchesPattern(string permission, string pattern);

    Task InvalidatePlanPermissionsCacheAsync(Guid clinicId, CancellationToken cancellationToken = default);
}
