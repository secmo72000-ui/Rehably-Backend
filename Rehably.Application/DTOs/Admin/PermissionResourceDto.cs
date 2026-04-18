namespace Rehably.Application.DTOs.Admin;

/// <summary>
/// A resource with its available actions (e.g., "clinics" with view, create, update, delete)
/// </summary>
public record PermissionResourceDto
{
    /// <summary>
    /// Resource key (e.g., "clinics")
    /// </summary>
    public string Resource { get; init; } = string.Empty;

    /// <summary>
    /// Resource name in English (e.g., "Clinic Management")
    /// </summary>
    public string NameEn { get; init; } = string.Empty;

    /// <summary>
    /// Resource name in Arabic (e.g., "ادارة العيادات")
    /// </summary>
    public string NameAr { get; init; } = string.Empty;

    /// <summary>
    /// Available actions for this resource
    /// </summary>
    public List<PermissionActionDto> Actions { get; init; } = new();
}
