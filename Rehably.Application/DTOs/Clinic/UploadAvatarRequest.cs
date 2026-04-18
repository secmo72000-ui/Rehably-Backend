namespace Rehably.Application.DTOs.Clinic;

public record UploadAvatarRequest
{
    public long FileSizeBytes { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string Base64Data { get; init; } = string.Empty;
}
