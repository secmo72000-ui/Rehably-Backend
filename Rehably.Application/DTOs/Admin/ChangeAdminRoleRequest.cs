namespace Rehably.Application.DTOs.Admin;

public record ChangeAdminRoleRequest
{
    public string RoleId { get; init; } = string.Empty;
}
