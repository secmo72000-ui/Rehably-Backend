namespace Rehably.Application.DTOs.Admin;

/// <summary>
/// An action that can be performed on a resource
/// </summary>
public record PermissionActionDto
{
    /// <summary>
    /// Action key (e.g., "view", "create", "update", "delete")
    /// </summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>
    /// Full permission string (e.g., "clinics.view")
    /// </summary>
    public string Permission { get; init; } = string.Empty;

    /// <summary>
    /// Action name in English (e.g., "View")
    /// </summary>
    public string NameEn { get; init; } = string.Empty;

    /// <summary>
    /// Action name in Arabic (e.g., "قراءة")
    /// </summary>
    public string NameAr { get; init; } = string.Empty;
}
