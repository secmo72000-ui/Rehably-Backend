using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Library;

public record LibraryQueryParameters
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public LibraryAccessTier? AccessTier { get; init; }
    public Guid? BodyRegionCategoryId { get; init; }
    public bool IncludeGlobal { get; init; } = true;
    public bool IncludeClinicSpecific { get; init; } = true;
    public bool IncludeDeleted { get; init; } = false;
    public string? SortBy { get; init; } = "name";
    public bool SortDesc { get; init; } = false;
}
