using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Clinic;

public record ClinicDocumentInput
{
    public DocumentType DocumentType { get; init; }
    public string Base64Content { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
}
