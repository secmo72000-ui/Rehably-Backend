namespace Rehably.Application.DTOs.Admin;

public record UpdatePlatformAdminRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public bool? IsActive { get; init; }
}
