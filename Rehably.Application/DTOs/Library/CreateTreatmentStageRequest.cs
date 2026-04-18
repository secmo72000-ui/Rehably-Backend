namespace Rehably.Application.DTOs.Library;

/// <summary>
/// Request to create a new treatment stage.
/// </summary>
public record CreateTreatmentStageRequest
{
    /// <summary>Optional body region association</summary>
    public Guid? BodyRegionId { get; set; }

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
}
