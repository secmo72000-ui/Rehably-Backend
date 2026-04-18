namespace Rehably.Application.DTOs.Communication;

public record SmsMessage
{
    public string To { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
}
