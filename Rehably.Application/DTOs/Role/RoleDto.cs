namespace Rehably.Application.DTOs.Role;

public record RoleDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsCustom { get; init; }
    public List<PermissionDto> Permissions { get; init; } = new();
    public DateTime CreatedAt { get; init; }
}
