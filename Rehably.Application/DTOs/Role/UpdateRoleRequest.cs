namespace Rehably.Application.DTOs.Role;

public record UpdateRoleRequest
{
    public string? Description { get; init; }
    public List<string>? Permissions { get; init; }
}
