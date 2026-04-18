namespace Rehably.Application.DTOs.Role;

public record CreateRoleRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public List<string> Permissions { get; init; } = new();
}
