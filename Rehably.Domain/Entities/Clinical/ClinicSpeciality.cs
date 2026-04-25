namespace Rehably.Domain.Entities.Clinical;

/// <summary>
/// Many-to-many join: a clinic can have multiple specialities.
/// Set by super-admin when onboarding or managing a clinic.
/// </summary>
public class ClinicSpeciality
{
    public Guid ClinicId { get; set; }
    public Guid SpecialityId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Speciality Speciality { get; set; } = null!;
}
