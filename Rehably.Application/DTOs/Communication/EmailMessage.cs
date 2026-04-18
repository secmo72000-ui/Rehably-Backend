namespace Rehably.Application.DTOs.Communication;

public record EmailMessage
{
    public string To { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public bool IsHtml { get; init; }
    public List<EmailAttachment> Attachments { get; init; } = new();
}
