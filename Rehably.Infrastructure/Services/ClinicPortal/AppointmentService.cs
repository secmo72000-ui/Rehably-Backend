using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.Contexts;
using Rehably.Application.DTOs.ClinicPortal;
using Rehably.Application.Services.ClinicPortal;
using Rehably.Domain.Entities.ClinicPortal;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Services.ClinicPortal;

public class AppointmentService : IAppointmentService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(ApplicationDbContext context, ITenantContext tenantContext, ILogger<AppointmentService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<PagedResult<AppointmentDto>>> GetAllAsync(AppointmentQueryParams query, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();
            var q = _context.Appointments.AsNoTracking()
                .Include(a => a.Patient)
                .Where(a => a.ClinicId == clinicId);

            if (query.PatientId.HasValue) q = q.Where(a => a.PatientId == query.PatientId.Value);
            if (!string.IsNullOrEmpty(query.TherapistId)) q = q.Where(a => a.TherapistId == query.TherapistId);
            if (query.Status.HasValue) q = q.Where(a => a.Status == query.Status.Value);
            if (query.From.HasValue) q = q.Where(a => a.StartTime >= query.From.Value);
            if (query.To.HasValue) q = q.Where(a => a.StartTime <= query.To.Value);

            var totalCount = await q.CountAsync(ct);
            var items = await q
                .OrderBy(a => a.StartTime)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(a => MapToDto(a))
                .ToListAsync(ct);

            return Result<PagedResult<AppointmentDto>>.Success(
                PagedResult<AppointmentDto>.Create(items, totalCount, query.Page, query.PageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting appointments for clinic {ClinicId}", _tenantContext.TenantId);
            return Result<PagedResult<AppointmentDto>>.Failure("Failed to retrieve appointments");
        }
    }

    public async Task<Result<AppointmentDto>> GetByIdAsync(Guid appointmentId, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();
            var a = await _context.Appointments
                .AsNoTracking()
                .Include(x => x.Patient)
                .Include(x => x.TreatmentPlan)
                .FirstOrDefaultAsync(x => x.Id == appointmentId && x.ClinicId == clinicId, ct);

            if (a == null) return Result<AppointmentDto>.Failure("Appointment not found");
            return Result<AppointmentDto>.Success(MapToDto(a));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting appointment {AppointmentId}", appointmentId);
            return Result<AppointmentDto>.Failure("Failed to retrieve appointment");
        }
    }

    public async Task<Result<List<AppointmentDto>>> GetByDateRangeAsync(DateTime from, DateTime to, string? therapistId = null, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();
            var q = _context.Appointments.AsNoTracking()
                .Include(a => a.Patient)
                .Where(a => a.ClinicId == clinicId && a.StartTime >= from && a.StartTime <= to);

            if (!string.IsNullOrEmpty(therapistId)) q = q.Where(a => a.TherapistId == therapistId);

            var items = await q.OrderBy(a => a.StartTime).Select(a => MapToDto(a)).ToListAsync(ct);
            return Result<List<AppointmentDto>>.Success(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting appointments by date range for clinic {ClinicId}", _tenantContext.TenantId);
            return Result<List<AppointmentDto>>.Failure("Failed to retrieve appointments");
        }
    }

    public async Task<Result<AppointmentDto>> CreateAsync(CreateAppointmentRequest request, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();

            var patientExists = await _context.Patients.AnyAsync(p => p.Id == request.PatientId && p.ClinicId == clinicId, ct);
            if (!patientExists) return Result<AppointmentDto>.Failure("Patient not found");

            var appointment = new Appointment
            {
                ClinicId = clinicId,
                PatientId = request.PatientId,
                TherapistId = request.TherapistId,
                TreatmentPlanId = request.TreatmentPlanId,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Type = request.Type,
                Title = request.Title,
                Notes = request.Notes,
                SendReminder = request.SendReminder,
                Status = AppointmentStatus.Scheduled,
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync(ct);

            var saved = await _context.Appointments.AsNoTracking().Include(a => a.Patient).FirstAsync(a => a.Id == appointment.Id, ct);
            return Result<AppointmentDto>.Success(MapToDto(saved));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appointment for clinic {ClinicId}", _tenantContext.TenantId);
            return Result<AppointmentDto>.Failure("Failed to create appointment");
        }
    }

    public async Task<Result<AppointmentDto>> UpdateAsync(Guid appointmentId, UpdateAppointmentRequest request, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();
            var a = await _context.Appointments.Include(x => x.Patient).FirstOrDefaultAsync(x => x.Id == appointmentId && x.ClinicId == clinicId, ct);
            if (a == null) return Result<AppointmentDto>.Failure("Appointment not found");

            if (request.StartTime.HasValue) a.StartTime = request.StartTime.Value;
            if (request.EndTime.HasValue) a.EndTime = request.EndTime.Value;
            if (request.TherapistId != null) a.TherapistId = request.TherapistId;
            if (request.Type.HasValue) a.Type = request.Type.Value;
            if (request.Title != null) a.Title = request.Title;
            if (request.Notes != null) a.Notes = request.Notes;
            a.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return Result<AppointmentDto>.Success(MapToDto(a));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating appointment {AppointmentId}", appointmentId);
            return Result<AppointmentDto>.Failure("Failed to update appointment");
        }
    }

    public async Task<Result<AppointmentDto>> ConfirmAsync(Guid appointmentId, CancellationToken ct = default)
        => await ChangeStatusAsync(appointmentId, AppointmentStatus.Confirmed, ct);

    public async Task<Result<AppointmentDto>> CompleteAsync(Guid appointmentId, CancellationToken ct = default)
        => await ChangeStatusAsync(appointmentId, AppointmentStatus.Completed, ct);

    public async Task<Result<AppointmentDto>> CancelAsync(Guid appointmentId, string reason, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();
            var a = await _context.Appointments.Include(x => x.Patient).FirstOrDefaultAsync(x => x.Id == appointmentId && x.ClinicId == clinicId, ct);
            if (a == null) return Result<AppointmentDto>.Failure("Appointment not found");

            a.Status = AppointmentStatus.Cancelled;
            a.CancellationReason = reason;
            a.CancelledAt = DateTime.UtcNow;
            a.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return Result<AppointmentDto>.Success(MapToDto(a));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling appointment {AppointmentId}", appointmentId);
            return Result<AppointmentDto>.Failure("Failed to cancel appointment");
        }
    }

    public async Task<Result> DeleteAsync(Guid appointmentId, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();
            var a = await _context.Appointments.FirstOrDefaultAsync(x => x.Id == appointmentId && x.ClinicId == clinicId, ct);
            if (a == null) return Result.Failure("Appointment not found");

            a.IsDeleted = true;
            a.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting appointment {AppointmentId}", appointmentId);
            return Result.Failure("Failed to delete appointment");
        }
    }

    // ============ Helpers ============

    private async Task<Result<AppointmentDto>> ChangeStatusAsync(Guid id, AppointmentStatus status, CancellationToken ct)
    {
        var clinicId = _tenantContext.GetCurrentTenantId();
        var a = await _context.Appointments.Include(x => x.Patient).FirstOrDefaultAsync(x => x.Id == id && x.ClinicId == clinicId, ct);
        if (a == null) return Result<AppointmentDto>.Failure("Appointment not found");

        a.Status = status;
        if (status == AppointmentStatus.Confirmed) a.ConfirmedAt = DateTime.UtcNow;
        if (status == AppointmentStatus.Completed) a.CompletedAt = DateTime.UtcNow;
        a.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        return Result<AppointmentDto>.Success(MapToDto(a));
    }

    private static AppointmentDto MapToDto(Appointment a) => new(
        a.Id,
        a.PatientId,
        a.Patient != null ? $"{a.Patient.FirstName} {a.Patient.LastName}" : "",
        a.TherapistId,
        a.TherapistName,
        a.TreatmentPlanId,
        a.TreatmentPlan?.Title,
        a.StartTime,
        a.EndTime,
        a.DurationMinutes,
        a.Status,
        a.Type,
        a.Title,
        a.Notes,
        a.CancellationReason,
        a.CreatedAt
    );
}
