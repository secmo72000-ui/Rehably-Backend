namespace Rehably.Application.DTOs.Admin;

/// <summary>
/// DTO for users nested within a role response (for expandable list in UI)
/// </summary>
public record RoleUserDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}
