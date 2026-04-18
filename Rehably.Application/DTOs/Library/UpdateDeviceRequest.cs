using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Library;

/// <summary>
/// Request DTO for updating an existing device.
/// </summary>
public record UpdateDeviceRequest
{
    public string Name { get; init; } = string.Empty;
    public string? NameArabic { get; init; }
    public string? Description { get; init; }
    public Stream? ImageStream { get; init; }
    public string? ImageFileName { get; init; }
    public string? ImageContentType { get; init; }
    public string? RelatedConditionCodes { get; init; }
    public string? Manufacturer { get; init; }
    public string? Model { get; init; }
    public LibraryAccessTier AccessTier { get; init; }
}
