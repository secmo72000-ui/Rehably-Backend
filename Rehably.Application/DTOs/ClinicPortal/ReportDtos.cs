namespace Rehably.Application.DTOs.ClinicPortal;

public record ClinicReportSummaryDto(
    DateTime From,
    DateTime To,
    int TotalPatients,
    int NewPatients,
    int ActivePatients,
    int TotalAppointments,
    int CompletedAppointments,
    int CancelledAppointments,
    int NoShowAppointments,
    double AttendanceRate,
    int TotalSessions,
    int CompletedSessions,
    double SessionCompletionRate,
    double? AveragePainReduction,
    double? AveragePatientSatisfaction,
    int ActiveTreatmentPlans,
    int CompletedTreatmentPlans,
    List<MonthlyBreakdownDto> MonthlyBreakdown
);

public record MonthlyBreakdownDto(
    int Year,
    int Month,
    string MonthName,
    int Sessions,
    int Appointments,
    int NewPatients
);

public record SessionReportItemDto(
    Guid Id,
    int SessionNumber,
    string PatientName,
    string? TherapistName,
    DateTime SessionDate,
    int DurationMinutes,
    string Status,
    int? PainLevel,
    int? PatientSatisfaction,
    string? Notes
);

public record ReportQueryParams
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string? TherapistId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
