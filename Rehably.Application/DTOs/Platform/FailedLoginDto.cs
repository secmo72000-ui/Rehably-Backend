namespace Rehably.Application.DTOs.Platform;

public record FailedLoginDto
{
    public DateTime Date { get; init; }
    public string Time { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string UserIdentifier { get; init; } = string.Empty;
    public string FailureReason { get; init; } = string.Empty;
}
