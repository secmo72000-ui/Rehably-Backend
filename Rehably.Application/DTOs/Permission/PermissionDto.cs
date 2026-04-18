namespace Rehably.Application.DTOs.Permission;

public record PermissionDto
{
    public string Resource { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Name => $"{Resource}.{Action}";
}
