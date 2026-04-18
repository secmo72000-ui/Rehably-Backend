using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Library;

/// <summary>
/// Generic DTO for library items (used when returning mixed item types).
/// </summary>
public record LibraryItemDto
{
    /// <summary>
    /// The item ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The type of library item.
    /// </summary>
    public LibraryType LibraryType { get; init; }

    /// <summary>
    /// The item name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The Arabic name if available.
    /// </summary>
    public string? NameArabic { get; init; }

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The access tier required for this item.
    /// </summary>
    public LibraryAccessTier AccessTier { get; init; }

    /// <summary>
    /// Whether this is a global item (ClinicId == null).
    /// </summary>
    public bool IsGlobal { get; init; }

    /// <summary>
    /// Whether the item is deleted (soft delete).
    /// </summary>
    public bool IsDeleted { get; init; }

    /// <summary>
    /// When the item was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// When the item was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }
}
