using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.Contexts;
using Rehably.Application.DTOs.ClinicPortal;
using Rehably.Application.Services.ClinicPortal;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Services.ClinicPortal;

public class ClinicDashboardService : IClinicDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ClinicDashboardService> _logger;

    public ClinicDashboardService(ApplicationDbContext context, ITenantContext tenantContext, ILogger<ClinicDashboardService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<ClinicDashboardDto>> GetDashboardAsync(CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();
            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var todayEnd = todayStart.AddDays(1);
            var weekEnd = todayStart.AddDays(7);
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var totalPatientsTask = _context.Patients.CountAsync(p => p.ClinicId == clinicId, ct);
            var activePatientsTask = _context.Patients.CountAsync(p => p.ClinicId == clinicId && p.Status == PatientStatus.Active, ct);
            var newPatientsTask = _context.Patients.CountAsync(p => p.ClinicId == clinicId && p.CreatedAt >= monthStart, ct);
            var todayApptsTask = _context.Appointments.CountAsync(a => a.ClinicId == clinicId && a.StartTime >= todayStart && a.StartTime < todayEnd, ct);
            var weekApptsTask = _context.Appointments.CountAsync(a => a.ClinicId == clinicId && a.StartTime >= todayStart && a.StartTime < weekEnd, ct);
            var pendingApptsTask = _context.Appointments.CountAsync(a => a.ClinicId == clinicId && a.Status == AppointmentStatus.Scheduled, ct);
            var activePlansTask = _context.TreatmentPlans.CountAsync(t => t.ClinicId == clinicId && t.Status == TreatmentPlanStatus.Active, ct);
            var completedPlansTask = _context.TreatmentPlans.CountAsync(t => t.ClinicId == clinicId && t.Status == TreatmentPlanStatus.Completed && t.UpdatedAt >= monthStart, ct);
            var sessionsMonthTask = _context.TherapySessions.CountAsync(s => s.ClinicId == clinicId && s.SessionDate >= monthStart, ct);
            var completedSessionsTask = _context.TherapySessions.CountAsync(s => s.ClinicId == clinicId && s.Status == SessionStatus.Completed && s.SessionDate >= monthStart, ct);

            await Task.WhenAll(totalPatientsTask, activePatientsTask, newPatientsTask,
                todayApptsTask, weekApptsTask, pendingApptsTask,
                activePlansTask, completedPlansTask, sessionsMonthTask, completedSessionsTask);

            var todaySchedule = await _context.Appointments.AsNoTracking()
                .Include(a => a.Patient)
                .Where(a => a.ClinicId == clinicId && a.StartTime >= todayStart && a.StartTime < todayEnd)
                .OrderBy(a => a.StartTime)
                .Select(a => new RecentAppointmentDto(
                    a.Id,
                    a.Patient != null ? $"{a.Patient.FirstName} {a.Patient.LastName}" : "",
                    a.StartTime,
                    a.EndTime,
                    a.TherapistName,
                    a.Status.ToString(),
                    a.Type.ToString()
                ))
                .ToListAsync(ct);

            var clinic = await _context.Clinics.AsNoTracking()
                .Include(c => c.CurrentSubscription)
                    .ThenInclude(s => s!.Package)
                .FirstOrDefaultAsync(c => c.Id == clinicId, ct);

            var dashboard = new ClinicDashboardDto(
                await totalPatientsTask,
                await activePatientsTask,
                await newPatientsTask,
                await todayApptsTask,
                await weekApptsTask,
                await pendingApptsTask,
                await activePlansTask,
                await completedPlansTask,
                await sessionsMonthTask,
                await completedSessionsTask,
                clinic?.PatientsLimit,
                clinic?.UsersLimit,
                clinic?.CurrentSubscription?.Package?.Name,
                clinic?.CurrentSubscription?.EndDate,
                todaySchedule
            );

            return Result<ClinicDashboardDto>.Success(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard for clinic {ClinicId}", _tenantContext.TenantId);
            return Result<ClinicDashboardDto>.Failure("Failed to retrieve dashboard data");
        }
    }

    public async Task<Result<ClinicProfileDto>> GetProfileAsync(CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();
            var clinic = await _context.Clinics.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clinicId, ct);
            if (clinic == null) return Result<ClinicProfileDto>.Failure("Clinic not found");

            return Result<ClinicProfileDto>.Success(new ClinicProfileDto(
                clinic.Id, clinic.Name, clinic.NameArabic, clinic.LogoUrl, clinic.Description,
                clinic.Phone, clinic.Email, clinic.Address, clinic.City, clinic.Country,
                clinic.Status.ToString(), clinic.CreatedAt
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile for clinic {ClinicId}", _tenantContext.TenantId);
            return Result<ClinicProfileDto>.Failure("Failed to retrieve clinic profile");
        }
    }

    public async Task<Result<ClinicProfileDto>> UpdateProfileAsync(UpdateClinicProfileRequest request, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();
            var clinic = await _context.Clinics.FirstOrDefaultAsync(c => c.Id == clinicId, ct);
            if (clinic == null) return Result<ClinicProfileDto>.Failure("Clinic not found");

            if (request.NameArabic != null) clinic.NameArabic = request.NameArabic;
            if (request.Description != null) clinic.Description = request.Description;
            if (request.Phone != null) clinic.Phone = request.Phone;
            if (request.Email != null) clinic.Email = request.Email;
            if (request.Address != null) clinic.Address = request.Address;
            if (request.City != null) clinic.City = request.City;
            if (request.Country != null) clinic.Country = request.Country;
            clinic.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            return Result<ClinicProfileDto>.Success(new ClinicProfileDto(
                clinic.Id, clinic.Name, clinic.NameArabic, clinic.LogoUrl, clinic.Description,
                clinic.Phone, clinic.Email, clinic.Address, clinic.City, clinic.Country,
                clinic.Status.ToString(), clinic.CreatedAt
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for clinic {ClinicId}", _tenantContext.TenantId);
            return Result<ClinicProfileDto>.Failure("Failed to update clinic profile");
        }
    }
}
