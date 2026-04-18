using Rehably.Domain.Entities.Base;
using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.ClinicPortal;

/// <summary>
/// Represents a rehabilitation treatment plan assigned to a patient.
/// References the global/clinic Library Treatment as a template.
/// </summary>
public class TreatmentPlan : BaseEntity
{
    public Guid ClinicId { get; set; }
    public Guid PatientId { get; set; }

    /// <summary>Optional reference to a Library Treatment template.</summary>
    public Guid? LibraryTreatmentId { get; set; }

    /// <summary>ApplicationUser.Id of the responsible therapist.</summary>
    public string? TherapistId { get; set; }
    public string? TherapistName { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Diagnosis { get; set; }
    public string? Goals { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public TreatmentPlanStatus Status { get; set; } = TreatmentPlanStatus.Draft;

    public int TotalSessionsPlanned { get; set; }
    public int CompletedSessions { get; set; }

    public string? Notes { get; set; }

    // Navigation
    public Patient Patient { get; set; } = null!;
    public ICollection<TherapySession> Sessions { get; set; } = new List<TherapySession>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
