namespace Rehably.Application.DTOs.Role;

public record AssignPermissionRequest
{
    public string Permission { get; init; } = string.Empty;
}
