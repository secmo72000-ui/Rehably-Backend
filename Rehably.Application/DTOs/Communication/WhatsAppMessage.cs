namespace Rehably.Application.DTOs.Communication;

public record WhatsAppMessage
{
    public string To { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public List<WhatsAppMedia> Media { get; init; } = new();
}
