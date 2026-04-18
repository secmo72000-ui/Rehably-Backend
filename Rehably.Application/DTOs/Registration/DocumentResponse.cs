namespace Rehably.Application.DTOs.Registration;

public record DocumentResponse
{
    public Guid DocumentId { get; init; }
    public string DocumentType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}
