namespace Rehably.Application.DTOs.Communication;

public record NotificationResult
{
    public bool Success { get; init; }
    public string? MessageId { get; init; }
    public string? ErrorMessage { get; init; }

    public static NotificationResult Ok(string? messageId = null)
        => new() { Success = true, MessageId = messageId };

    public static NotificationResult Fail(string errorMessage)
        => new() { Success = false, ErrorMessage = errorMessage };
}
