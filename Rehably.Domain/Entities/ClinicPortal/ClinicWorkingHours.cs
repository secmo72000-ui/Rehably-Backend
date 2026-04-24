namespace Rehably.Domain.Entities.ClinicPortal;

/// <summary>
/// Stores the weekly working-hours schedule for a clinic.
/// One row per day of the week (0 = Sunday … 6 = Saturday).
/// </summary>
public class ClinicWorkingHours
{
    public Guid       Id         { get; set; }
    public Guid       ClinicId   { get; set; }

    /// <summary>0 = Sunday, 1 = Monday, … 6 = Saturday</summary>
    public int        DayOfWeek  { get; set; }

    /// <summary>false = weekend / day off — time fields are irrelevant when false.</summary>
    public bool       IsOpen     { get; set; }

    /// <summary>"HH:mm" — null when IsOpen = false</summary>
    public string?    OpenTime   { get; set; }

    /// <summary>"HH:mm" — null when IsOpen = false</summary>
    public string?    CloseTime  { get; set; }

    public DateTime   UpdatedAt  { get; set; } = DateTime.UtcNow;
}
