namespace Rehably.Application.DTOs.Communication;

public record SmsResult
{
    public bool Success { get; init; }
    public string MessageId { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public DateTime SentAt { get; init; } = DateTime.UtcNow;
}
