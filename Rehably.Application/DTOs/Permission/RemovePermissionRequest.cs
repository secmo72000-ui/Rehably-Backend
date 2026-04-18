namespace Rehably.Application.DTOs.Permission;

public record RemovePermissionRequest
{
    public string RoleName { get; init; } = string.Empty;
    public string Permission { get; init; } = string.Empty;
}
