using Rehably.Domain.Entities.Base;
using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.ClinicPortal;

/// <summary>
/// Represents a patient registered at a clinic.
/// ClinicId serves as the multi-tenancy discriminator and is enforced by EF Core query filters.
/// </summary>
public class Patient : BaseEntity
{
    public Guid ClinicId { get; set; }

    // Demographics
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? FirstNameArabic { get; set; }
    public string? LastNameArabic { get; set; }
    public string? NationalId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Gender Gender { get; set; }

    // Contact
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }

    // Emergency contact
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }

    // Clinical
    public string? Diagnosis { get; set; }
    public string? MedicalHistory { get; set; }
    public string? Allergies { get; set; }
    public string? CurrentMedications { get; set; }
    public string? Notes { get; set; }
    public string? ProfileImageUrl { get; set; }
    public PatientStatus Status { get; set; } = PatientStatus.Active;
    public DateTime? DischargedAt { get; set; }

    // Navigation
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<TreatmentPlan> TreatmentPlans { get; set; } = new List<TreatmentPlan>();

    // Computed
    public string FullName => $"{FirstName} {LastName}";
}
