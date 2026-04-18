using Rehably.Domain.Entities.Base;
using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.ClinicPortal;

/// <summary>
/// Represents a single therapy session within a treatment plan.
/// Named TherapySession to avoid collision with ASP.NET Core's Session class.
/// </summary>
public class TherapySession : BaseEntity
{
    public Guid ClinicId { get; set; }
    public Guid TreatmentPlanId { get; set; }
    public Guid PatientId { get; set; }

    /// <summary>ApplicationUser.Id of the therapist who conducted the session.</summary>
    public string? TherapistId { get; set; }
    public string? TherapistName { get; set; }

    public int SessionNumber { get; set; }
    public DateTime SessionDate { get; set; }
    public int DurationMinutes { get; set; }

    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;

    public string? Notes { get; set; }
    public string? PatientProgress { get; set; }

    /// <summary>JSON array of exercise IDs performed in this session.</summary>
    public string? ExercisesPerformed { get; set; }

    /// <summary>Pain level reported by patient (0-10).</summary>
    public int? PainLevel { get; set; }

    /// <summary>Overall patient satisfaction (1-5).</summary>
    public int? PatientSatisfaction { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public TreatmentPlan TreatmentPlan { get; set; } = null!;
    public Patient Patient { get; set; } = null!;
}
