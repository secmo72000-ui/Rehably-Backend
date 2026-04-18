namespace Rehably.Application.DTOs.Communication;

public record WhatsAppMedia
{
    public string Url { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string? Caption { get; init; }
}
