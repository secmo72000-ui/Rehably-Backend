namespace Rehably.Application.DTOs.Library;

/// <summary>
/// DTO representing a treatment stage for a clinic.
/// </summary>
public record TreatmentStageDto
{
    /// <summary>Unique identifier</summary>
    public Guid Id { get; set; }

    /// <summary>Tenant (clinic) identifier</summary>
    public Guid TenantId { get; set; }

    /// <summary>Optional body region filter</summary>
    public Guid? BodyRegionId { get; set; }

    /// <summary>Name of the associated body region</summary>
    public string? BodyRegionName { get; set; }

    /// <summary>Unique code within the clinic</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Stage name in English</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Stage name in Arabic</summary>
    public string? NameArabic { get; set; }

    /// <summary>Description of the stage</summary>
    public string? Description { get; set; }

    /// <summary>Minimum duration in weeks</summary>
    public int? MinWeeks { get; set; }

    /// <summary>Maximum duration in weeks</summary>
    public int? MaxWeeks { get; set; }

    /// <summary>Minimum number of sessions</summary>
    public int? MinSessions { get; set; }

    /// <summary>Maximum number of sessions</summary>
    public int? MaxSessions { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
