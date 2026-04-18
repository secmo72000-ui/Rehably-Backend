using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.ClinicPortal;
using Rehably.Application.Services.ClinicPortal;
using Rehably.Domain.Enums;
using Rehably.Application.Contexts;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Services.ClinicPortal;

public class ClinicReportService : IClinicReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ClinicReportService> _logger;

    public ClinicReportService(ApplicationDbContext context, ILogger<ClinicReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<ClinicReportSummaryDto>> GetSummaryAsync(Guid clinicId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        try
        {
            var toEnd = to.Date.AddDays(1);

            var totalPatients  = await _context.Patients.CountAsync(p => p.ClinicId == clinicId, ct);
            var activePatients = await _context.Patients.CountAsync(p => p.ClinicId == clinicId && p.Status == PatientStatus.Active, ct);
            var newPatients    = await _context.Patients.CountAsync(p => p.ClinicId == clinicId && p.CreatedAt >= from && p.CreatedAt < toEnd, ct);

            var appts = await _context.Appointments
                .Where(a => a.ClinicId == clinicId && a.StartTime >= from && a.StartTime < toEnd)
                .Select(a => new { a.Status })
                .ToListAsync(ct);

            var totalAppts     = appts.Count;
            var completedAppts = appts.Count(a => a.Status == AppointmentStatus.Completed);
            var cancelledAppts = appts.Count(a => a.Status == AppointmentStatus.Cancelled);
            var noShowAppts    = appts.Count(a => a.Status == AppointmentStatus.NoShow);
            var attendanceRate = totalAppts > 0 ? Math.Round((double)completedAppts / totalAppts * 100, 1) : 0;

            var sessions = await _context.TherapySessions
                .Where(s => s.ClinicId == clinicId && s.SessionDate >= from && s.SessionDate < toEnd)
                .Select(s => new { s.Status, s.PainLevel, s.PatientSatisfaction })
                .ToListAsync(ct);

            var totalSessions     = sessions.Count;
            var completedSessions = sessions.Count(s => s.Status == SessionStatus.Completed);
            var sessionRate       = totalSessions > 0 ? Math.Round((double)completedSessions / totalSessions * 100, 1) : 0;
            var painSessions      = sessions.Where(s => s.PainLevel.HasValue).ToList();
            var satSessions       = sessions.Where(s => s.PatientSatisfaction.HasValue).ToList();
            double? avgPain       = painSessions.Count > 0 ? Math.Round(painSessions.Average(s => (double)s.PainLevel!.Value), 1) : null;
            double? avgSat        = satSessions.Count  > 0 ? Math.Round(satSessions.Average(s => (double)s.PatientSatisfaction!.Value), 1) : null;

            var activePlans    = await _context.TreatmentPlans.CountAsync(t => t.ClinicId == clinicId && t.Status == TreatmentPlanStatus.Active, ct);
            var completedPlans = await _context.TreatmentPlans.CountAsync(t => t.ClinicId == clinicId && t.Status == TreatmentPlanStatus.Completed && t.UpdatedAt >= from && t.UpdatedAt < toEnd, ct);

            var monthly = await BuildMonthlyAsync(clinicId, from, to, ct);

            return Result<ClinicReportSummaryDto>.Success(new ClinicReportSummaryDto(
                from, to,
                totalPatients, newPatients, activePatients,
                totalAppts, completedAppts, cancelledAppts, noShowAppts, attendanceRate,
                totalSessions, completedSessions, sessionRate, avgPain, avgSat,
                activePlans, completedPlans, monthly));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report for clinic {ClinicId}", clinicId);
            return Result<ClinicReportSummaryDto>.Failure("Failed to generate report");
        }
    }

    public async Task<Result<PagedResult<SessionReportItemDto>>> GetSessionsReportAsync(Guid clinicId, ReportQueryParams query, CancellationToken ct = default)
    {
        try
        {
            var from  = query.From ?? DateTime.UtcNow.AddMonths(-1);
            var toEnd = (query.To ?? DateTime.UtcNow).Date.AddDays(1);

            var q = _context.TherapySessions
                .Include(s => s.Patient)
                .Where(s => s.ClinicId == clinicId && s.SessionDate >= from && s.SessionDate < toEnd);

            if (!string.IsNullOrWhiteSpace(query.TherapistId))
                q = q.Where(s => s.TherapistId == query.TherapistId);

            var total = await q.CountAsync(ct);
            var items = await q.OrderByDescending(s => s.SessionDate)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(s => new SessionReportItemDto(
                    s.Id, s.SessionNumber,
                    s.Patient != null ? $"{s.Patient.FirstName} {s.Patient.LastName}" : "",
                    s.TherapistName, s.SessionDate, s.DurationMinutes,
                    s.Status.ToString(), s.PainLevel, s.PatientSatisfaction, s.Notes))
                .ToListAsync(ct);

            return Result<PagedResult<SessionReportItemDto>>.Success(
                PagedResult<SessionReportItemDto>.Create(items, total, query.Page, query.PageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sessions report for clinic {ClinicId}", clinicId);
            return Result<PagedResult<SessionReportItemDto>>.Failure("Failed to generate sessions report");
        }
    }

    private async Task<List<MonthlyBreakdownDto>> BuildMonthlyAsync(Guid clinicId, DateTime from, DateTime to, CancellationToken ct)
    {
        var arabicMonths = new[] { "يناير","فبراير","مارس","أبريل","مايو","يونيو","يوليو","أغسطس","سبتمبر","أكتوبر","نوفمبر","ديسمبر" };
        var toEnd = to.Date.AddDays(1);

        var sessionsRaw  = await _context.TherapySessions.Where(s => s.ClinicId == clinicId && s.SessionDate >= from && s.SessionDate < toEnd).Select(s => s.SessionDate).ToListAsync(ct);
        var apptsRaw     = await _context.Appointments.Where(a => a.ClinicId == clinicId && a.StartTime >= from && a.StartTime < toEnd).Select(a => a.StartTime).ToListAsync(ct);
        var patientsRaw  = await _context.Patients.Where(p => p.ClinicId == clinicId && p.CreatedAt >= from && p.CreatedAt < toEnd).Select(p => p.CreatedAt).ToListAsync(ct);

        var result = new List<MonthlyBreakdownDto>();
        var cursor = new DateTime(from.Year, from.Month, 1);
        while (cursor <= to)
        {
            var y = cursor.Year; var m = cursor.Month;
            result.Add(new MonthlyBreakdownDto(y, m, arabicMonths[m - 1],
                sessionsRaw.Count(d => d.Year == y && d.Month == m),
                apptsRaw.Count(d => d.Year == y && d.Month == m),
                patientsRaw.Count(d => d.Year == y && d.Month == m)));
            cursor = cursor.AddMonths(1);
        }
        return result;
    }
}
