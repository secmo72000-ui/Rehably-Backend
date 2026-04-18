using Rehably.Domain.Entities.Base;
using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.ClinicPortal;

/// <summary>
/// Represents a scheduled appointment between a patient and a therapist.
/// </summary>
public class Appointment : BaseEntity
{
    public Guid ClinicId { get; set; }
    public Guid PatientId { get; set; }

    /// <summary>ApplicationUser.Id of the assigned therapist/doctor.</summary>
    public string? TherapistId { get; set; }
    public string? TherapistName { get; set; }

    public Guid? TreatmentPlanId { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int DurationMinutes => (int)(EndTime - StartTime).TotalMinutes;

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public AppointmentType Type { get; set; } = AppointmentType.Treatment;

    public string? Title { get; set; }
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Optional patient-facing portal access
    public bool SendReminder { get; set; } = true;
    public DateTime? ReminderSentAt { get; set; }

    // Navigation
    public Patient Patient { get; set; } = null!;
    public TreatmentPlan? TreatmentPlan { get; set; }
}
