using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Library;

/// <summary>
/// Request DTO for creating a new exercise.
/// </summary>
public record CreateExerciseRequest
{
    public string Name { get; init; } = string.Empty;
    public string? NameArabic { get; init; }
    public string? Description { get; init; }
    public Guid BodyRegionCategoryId { get; init; }
    public string? RelatedConditionCode { get; init; }
    public string? Tags { get; init; }
    public int? Repeats { get; init; }
    public int? Steps { get; init; }
    public int? HoldSeconds { get; init; }
    public Stream? VideoStream { get; init; }
    public string? VideoFileName { get; init; }
    public string? VideoContentType { get; init; }
    public Stream? ThumbnailStream { get; init; }
    public string? ThumbnailFileName { get; init; }
    public string? ThumbnailContentType { get; init; }
    public string? LinkedExerciseIds { get; init; }
    public LibraryAccessTier AccessTier { get; init; } = LibraryAccessTier.Free;
}
