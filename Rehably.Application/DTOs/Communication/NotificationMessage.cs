namespace Rehably.Application.DTOs.Communication;

public record NotificationMessage
{
    public string To { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public Dictionary<string, object> ChannelProperties { get; init; } = new();
}
