namespace Rehably.Application.DTOs.Role;

/// <summary>
/// Permission data transfer object for use within role context.
/// </summary>
public record PermissionDto
{
    public string Name { get; init; } = string.Empty;
    public string Resource { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
}
