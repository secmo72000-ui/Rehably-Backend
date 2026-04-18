using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Library;

/// <summary>
/// Query parameters specific to assessment filtering.
/// </summary>
public record AssessmentQueryParameters : LibraryQueryParameters
{
    public AssessmentTimePoint? TimePoint { get; init; }
}
