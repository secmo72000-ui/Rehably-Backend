namespace Rehably.Application.DTOs.Auth;

public record UpdateRolePermissionsRequest
{
    public List<string> PermissionNames { get; init; } = new();
}
