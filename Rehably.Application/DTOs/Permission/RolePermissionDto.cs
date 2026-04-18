namespace Rehably.Application.DTOs.Permission;

public record RolePermissionDto
{
    public string RoleName { get; init; } = string.Empty;
    public string Resource { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string PermissionName => $"{Resource}.{Action}";
}
