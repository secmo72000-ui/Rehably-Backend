using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.ClinicPortal;

// ── Dashboard ─────────────────────────────────────────────────────────────────

public record ClinicDashboardDto(
    int TotalPatients,
    int ActivePatients,
    int NewPatientsThisMonth,
    int TodayAppointments,
    int WeekAppointments,
    int PendingAppointments,
    int ActiveTreatmentPlans,
    int CompletedPlansThisMonth,
    int SessionsThisMonth,
    int CompletedSessionsThisMonth,
    int? PatientsLimit,
    int? UsersLimit,
    string? SubscriptionPlanName,
    DateTime? SubscriptionEndDate,
    List<RecentAppointmentDto> TodaySchedule
);

public record RecentAppointmentDto(
    Guid Id,
    string PatientName,
    DateTime StartTime,
    DateTime EndTime,
    string? TherapistName,
    string Status,
    string Type
);

// ── Clinic Profile ────────────────────────────────────────────────────────────

public record ClinicProfileDto(
    Guid Id,
    string Name,
    string? NameArabic,
    string? LogoUrl,
    string? Description,
    string? Phone,
    string? Email,
    string? Address,
    string? City,
    string? Country,
    string Status,
    DateTime CreatedAt
);

public class UpdateClinicProfileRequest
{
    public string? NameArabic { get; set; }
    public string? Description { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
}

// ── Patient ───────────────────────────────────────────────────────────────────

public class PatientQueryParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public PatientStatus? Status { get; set; }
    public string? TherapistId { get; set; }
}

public record PatientListDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? FirstNameArabic,
    string? LastNameArabic,
    string? Phone,
    string? Email,
    DateTime? DateOfBirth,
    string Gender,
    PatientStatus Status,
    string? Diagnosis,
    int AppointmentsCount,
    int ActiveTreatmentPlansCount,
    DateTime CreatedAt
);

public record PatientDetailDto(
    Guid Id,
    Guid ClinicId,
    string FirstName,
    string LastName,
    string? FirstNameArabic,
    string? LastNameArabic,
    string? NationalId,
    DateTime? DateOfBirth,
    string Gender,
    string? Phone,
    string? Email,
    string? Address,
    string? City,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? EmergencyContactRelation,
    string? Diagnosis,
    string? MedicalHistory,
    string? Allergies,
    string? CurrentMedications,
    string? Notes,
    string? ProfileImageUrl,
    PatientStatus Status,
    DateTime? DischargedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public class CreatePatientRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? FirstNameArabic { get; set; }
    public string? LastNameArabic { get; set; }
    public string? NationalId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }
    public string? Diagnosis { get; set; }
    public string? MedicalHistory { get; set; }
    public string? Allergies { get; set; }
    public string? CurrentMedications { get; set; }
    public string? Notes { get; set; }
}

public class UpdatePatientRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FirstNameArabic { get; set; }
    public string? LastNameArabic { get; set; }
    public string? NationalId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }
    public string? Diagnosis { get; set; }
    public string? MedicalHistory { get; set; }
    public string? Allergies { get; set; }
    public string? CurrentMedications { get; set; }
    public string? Notes { get; set; }
}

// ── Appointment ───────────────────────────────────────────────────────────────

public class AppointmentQueryParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public AppointmentStatus? Status { get; set; }
    public string? TherapistId { get; set; }
    public Guid? PatientId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public record AppointmentDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    string? TherapistId,
    string? TherapistName,
    Guid? TreatmentPlanId,
    string? TreatmentPlanTitle,
    DateTime StartTime,
    DateTime EndTime,
    int DurationMinutes,
    AppointmentStatus Status,
    AppointmentType Type,
    string? Title,
    string? Notes,
    string? CancellationReason,
    DateTime CreatedAt
);

public class CreateAppointmentRequest
{
    public Guid PatientId { get; set; }
    public string? TherapistId { get; set; }
    public Guid? TreatmentPlanId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public AppointmentType Type { get; set; } = AppointmentType.Treatment;
    public string? Title { get; set; }
    public string? Notes { get; set; }
    public bool SendReminder { get; set; } = true;
}

public class UpdateAppointmentRequest
{
    public string? TherapistId { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public AppointmentType? Type { get; set; }
    public string? Title { get; set; }
    public string? Notes { get; set; }
}

// ── Treatment Plan ────────────────────────────────────────────────────────────

public class TreatmentPlanQueryParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public TreatmentPlanStatus? Status { get; set; }
    public Guid? PatientId { get; set; }
    public string? TherapistId { get; set; }
    public string? Search { get; set; }
}

public record TreatmentPlanDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    string? TherapistId,
    string? TherapistName,
    string Title,
    string? Diagnosis,
    TreatmentPlanStatus Status,
    DateTime StartDate,
    DateTime? EndDate,
    int TotalSessionsPlanned,
    int CompletedSessions,
    DateTime CreatedAt
);

public record TreatmentPlanDetailDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    string? TherapistId,
    string? TherapistName,
    Guid? LibraryTreatmentId,
    string Title,
    string? Description,
    string? Diagnosis,
    string? Goals,
    TreatmentPlanStatus Status,
    DateTime StartDate,
    DateTime? EndDate,
    int TotalSessionsPlanned,
    int CompletedSessions,
    string? Notes,
    List<SessionDto> Sessions,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public class CreateTreatmentPlanRequest
{
    public Guid PatientId { get; set; }
    public string? TherapistId { get; set; }
    public Guid? LibraryTreatmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Diagnosis { get; set; }
    public string? Goals { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int TotalSessionsPlanned { get; set; }
    public string? Notes { get; set; }
}

public class UpdateTreatmentPlanRequest
{
    public string? TherapistId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Diagnosis { get; set; }
    public string? Goals { get; set; }
    public DateTime? EndDate { get; set; }
    public int? TotalSessionsPlanned { get; set; }
    public string? Notes { get; set; }
}

// ── Session ───────────────────────────────────────────────────────────────────

public record SessionDto(
    Guid Id,
    Guid TreatmentPlanId,
    Guid PatientId,
    string? TherapistId,
    string? TherapistName,
    int SessionNumber,
    DateTime SessionDate,
    int DurationMinutes,
    SessionStatus Status,
    string? Notes,
    string? PatientProgress,
    int? PainLevel,
    int? PatientSatisfaction,
    DateTime? CompletedAt
);

public class CreateSessionRequest
{
    public string? TherapistId { get; set; }
    public DateTime SessionDate { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public string? Notes { get; set; }
}

public class UpdateSessionRequest
{
    public DateTime? SessionDate { get; set; }
    public int? DurationMinutes { get; set; }
    public string? TherapistId { get; set; }
    public string? Notes { get; set; }
    public SessionStatus? Status { get; set; }
}

public class CompleteSessionRequest
{
    public string? Notes { get; set; }
    public string? PatientProgress { get; set; }
    public int? PainLevel { get; set; }
    public int? PatientSatisfaction { get; set; }
    public string? ExercisesPerformed { get; set; }
}
