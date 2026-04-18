namespace Rehably.Application.DTOs.Admin;

public record UpdatePlatformRoleRequest
{
    public string? Description { get; init; }
    public List<string> Permissions { get; init; } = new();
}
