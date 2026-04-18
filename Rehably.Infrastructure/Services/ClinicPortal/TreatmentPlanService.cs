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

public class TreatmentPlanService : ITreatmentPlanService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TreatmentPlanService> _logger;

    public TreatmentPlanService(ApplicationDbContext context, ITenantContext tenantContext, ILogger<TreatmentPlanService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<PagedResult<TreatmentPlanDto>>> GetAllAsync(TreatmentPlanQueryParams query, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();
            var q = _context.TreatmentPlans.AsNoTracking()
                .Include(t => t.Patient)
                .Where(t => t.ClinicId == clinicId);

            if (query.PatientId.HasValue) q = q.Where(t => t.PatientId == query.PatientId.Value);
            if (!string.IsNullOrEmpty(query.TherapistId)) q = q.Where(t => t.TherapistId == query.TherapistId);
            if (query.Status.HasValue) q = q.Where(t => t.Status == query.Status.Value);

            var totalCount = await q.CountAsync(ct);
            var items = await q
                .OrderByDescending(t => t.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(t => MapToDto(t))
                .ToListAsync(ct);

            return Result<PagedResult<TreatmentPlanDto>>.Success(
                PagedResult<TreatmentPlanDto>.Create(items, totalCount, query.Page, query.PageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting treatment plans for clinic {ClinicId}", _tenantContext.TenantId);
            return Result<PagedResult<TreatmentPlanDto>>.Failure("Failed to retrieve treatment plans");
        }
    }

    public async Task<Result<TreatmentPlanDetailDto>> GetByIdAsync(Guid planId, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();
            var plan = await _context.TreatmentPlans.AsNoTracking()
                .Include(t => t.Patient)
                .Include(t => t.Sessions.Where(s => !s.IsDeleted))
                .FirstOrDefaultAsync(t => t.Id == planId && t.ClinicId == clinicId, ct);

            if (plan == null) return Result<TreatmentPlanDetailDto>.Failure("Treatment plan not found");
            return Result<TreatmentPlanDetailDto>.Success(MapToDetailDto(plan));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting treatment plan {PlanId}", planId);
            return Result<TreatmentPlanDetailDto>.Failure("Failed to retrieve treatment plan");
        }
    }

    public async Task<Result<List<TreatmentPlanDto>>> GetByPatientAsync(Guid patientId, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();
            var plans = await _context.TreatmentPlans.AsNoTracking()
                .Include(t => t.Patient)
                .Where(t => t.ClinicId == clinicId && t.PatientId == patientId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => MapToDto(t))
                .ToListAsync(ct);
            return Result<List<TreatmentPlanDto>>.Success(plans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting treatment plans for patient {PatientId}", patientId);
            return Result<List<TreatmentPlanDto>>.Failure("Failed to retrieve treatment plans");
        }
    }

    public async Task<Result<TreatmentPlanDetailDto>> CreateAsync(CreateTreatmentPlanRequest request, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();

            var patientExists = await _context.Patients.AnyAsync(p => p.Id == request.PatientId && p.ClinicId == clinicId, ct);
            if (!patientExists) return Result<TreatmentPlanDetailDto>.Failure("Patient not found");

            var plan = new TreatmentPlan
            {
                ClinicId = clinicId,
                PatientId = request.PatientId,
                TherapistId = request.TherapistId,
                LibraryTreatmentId = request.LibraryTreatmentId,
                Title = request.Title,
                Description = request.Description,
                Diagnosis = request.Diagnosis,
                Goals = request.Goals,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                TotalSessionsPlanned = request.TotalSessionsPlanned,
                Notes = request.Notes,
                Status = TreatmentPlanStatus.Draft,
            };

            _context.TreatmentPlans.Add(plan);
            await _context.SaveChangesAsync(ct);

            var saved = await _context.TreatmentPlans.AsNoTracking()
                .Include(t => t.Patient)
                .Include(t => t.Sessions)
                .FirstAsync(t => t.Id == plan.Id, ct);

            return Result<TreatmentPlanDetailDto>.Success(MapToDetailDto(saved));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating treatment plan for clinic {ClinicId}", _tenantContext.TenantId);
            return Result<TreatmentPlanDetailDto>.Failure("Failed to create treatment plan");
        }
    }

    public async Task<Result<TreatmentPlanDetailDto>> UpdateAsync(Guid planId, UpdateTreatmentPlanRequest request, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();
            var plan = await _context.TreatmentPlans.Include(t => t.Patient).Include(t => t.Sessions)
                .FirstOrDefaultAsync(t => t.Id == planId && t.ClinicId == clinicId, ct);
            if (plan == null) return Result<TreatmentPlanDetailDto>.Failure("Treatment plan not found");

            if (request.TherapistId != null) plan.TherapistId = request.TherapistId;
            if (request.Title != null) plan.Title = request.Title;
            if (request.Description != null) plan.Description = request.Description;
            if (request.Diagnosis != null) plan.Diagnosis = request.Diagnosis;
            if (request.Goals != null) plan.Goals = request.Goals;
            if (request.EndDate.HasValue) plan.EndDate = request.EndDate;
            if (request.TotalSessionsPlanned.HasValue) plan.TotalSessionsPlanned = request.TotalSessionsPlanned.Value;
            if (request.Notes != null) plan.Notes = request.Notes;
            plan.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return Result<TreatmentPlanDetailDto>.Success(MapToDetailDto(plan));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating treatment plan {PlanId}", planId);
            return Result<TreatmentPlanDetailDto>.Failure("Failed to update treatment plan");
        }
    }

    public async Task<Result<TreatmentPlanDetailDto>> ActivateAsync(Guid planId, CancellationToken ct = default)
        => await ChangeStatusAsync(planId, TreatmentPlanStatus.Active, ct);

    public async Task<Result<TreatmentPlanDetailDto>> CompleteAsync(Guid planId, CancellationToken ct = default)
        => await ChangeStatusAsync(planId, TreatmentPlanStatus.Completed, ct);

    public async Task<Result> DeleteAsync(Guid planId, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();
            var plan = await _context.TreatmentPlans.FirstOrDefaultAsync(t => t.Id == planId && t.ClinicId == clinicId, ct);
            if (plan == null) return Result.Failure("Treatment plan not found");
            plan.IsDeleted = true;
            plan.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting treatment plan {PlanId}", planId);
            return Result.Failure("Failed to delete treatment plan");
        }
    }

    public async Task<Result<List<SessionDto>>> GetSessionsAsync(Guid planId, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();
            var sessions = await _context.TherapySessions.AsNoTracking()
                .Where(s => s.TreatmentPlanId == planId && s.ClinicId == clinicId)
                .OrderBy(s => s.SessionNumber)
                .Select(s => MapSessionToDto(s))
                .ToListAsync(ct);
            return Result<List<SessionDto>>.Success(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions for plan {PlanId}", planId);
            return Result<List<SessionDto>>.Failure("Failed to retrieve sessions");
        }
    }

    public async Task<Result<SessionDto>> AddSessionAsync(Guid planId, CreateSessionRequest request, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();
            var plan = await _context.TreatmentPlans.FirstOrDefaultAsync(t => t.Id == planId && t.ClinicId == clinicId, ct);
            if (plan == null) return Result<SessionDto>.Failure("Treatment plan not found");

            var nextNumber = await _context.TherapySessions.Where(s => s.TreatmentPlanId == planId).CountAsync(ct) + 1;

            var session = new TherapySession
            {
                ClinicId = clinicId,
                TreatmentPlanId = planId,
                PatientId = plan.PatientId,
                TherapistId = request.TherapistId,
                SessionNumber = nextNumber,
                SessionDate = request.SessionDate,
                DurationMinutes = request.DurationMinutes,
                Notes = request.Notes,
                Status = SessionStatus.Scheduled,
            };

            _context.TherapySessions.Add(session);
            await _context.SaveChangesAsync(ct);
            return Result<SessionDto>.Success(MapSessionToDto(session));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding session to plan {PlanId}", planId);
            return Result<SessionDto>.Failure("Failed to add session");
        }
    }

    public async Task<Result<SessionDto>> UpdateSessionAsync(Guid sessionId, UpdateSessionRequest request, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();
            var s = await _context.TherapySessions.FirstOrDefaultAsync(x => x.Id == sessionId && x.ClinicId == clinicId, ct);
            if (s == null) return Result<SessionDto>.Failure("Session not found");

            if (request.SessionDate.HasValue) s.SessionDate = request.SessionDate.Value;
            if (request.DurationMinutes.HasValue) s.DurationMinutes = request.DurationMinutes.Value;
            if (request.TherapistId != null) s.TherapistId = request.TherapistId;
            if (request.Notes != null) s.Notes = request.Notes;
            if (request.Status.HasValue) s.Status = request.Status.Value;
            s.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return Result<SessionDto>.Success(MapSessionToDto(s));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session {SessionId}", sessionId);
            return Result<SessionDto>.Failure("Failed to update session");
        }
    }

    public async Task<Result<SessionDto>> CompleteSessionAsync(Guid sessionId, CompleteSessionRequest request, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();
            var s = await _context.TherapySessions.FirstOrDefaultAsync(x => x.Id == sessionId && x.ClinicId == clinicId, ct);
            if (s == null) return Result<SessionDto>.Failure("Session not found");

            s.Status = SessionStatus.Completed;
            s.Notes = request.Notes ?? s.Notes;
            s.PatientProgress = request.PatientProgress;
            s.ExercisesPerformed = request.ExercisesPerformed;
            s.PainLevel = request.PainLevel;
            s.PatientSatisfaction = request.PatientSatisfaction;
            s.CompletedAt = DateTime.UtcNow;
            s.UpdatedAt = DateTime.UtcNow;

            var plan = await _context.TreatmentPlans.FirstOrDefaultAsync(t => t.Id == s.TreatmentPlanId, ct);
            if (plan != null) { plan.CompletedSessions++; plan.UpdatedAt = DateTime.UtcNow; }

            await _context.SaveChangesAsync(ct);
            return Result<SessionDto>.Success(MapSessionToDto(s));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing session {SessionId}", sessionId);
            return Result<SessionDto>.Failure("Failed to complete session");
        }
    }

    // ============ Helpers ============

    private async Task<Result<TreatmentPlanDetailDto>> ChangeStatusAsync(Guid id, TreatmentPlanStatus status, CancellationToken ct)
    {
        var clinicId = _tenantContext.GetCurrentTenantId();
        var plan = await _context.TreatmentPlans.Include(t => t.Patient).Include(t => t.Sessions)
            .FirstOrDefaultAsync(t => t.Id == id && t.ClinicId == clinicId, ct);
        if (plan == null) return Result<TreatmentPlanDetailDto>.Failure("Treatment plan not found");
        plan.Status = status;
        plan.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        return Result<TreatmentPlanDetailDto>.Success(MapToDetailDto(plan));
    }

    private static TreatmentPlanDto MapToDto(TreatmentPlan t) => new(
        t.Id, t.PatientId,
        t.Patient != null ? $"{t.Patient.FirstName} {t.Patient.LastName}" : "",
        t.TherapistId, t.TherapistName, t.Title, t.Diagnosis,
        t.Status, t.StartDate, t.EndDate,
        t.TotalSessionsPlanned, t.CompletedSessions, t.CreatedAt
    );

    private static TreatmentPlanDetailDto MapToDetailDto(TreatmentPlan t) => new(
        t.Id, t.PatientId,
        t.Patient != null ? $"{t.Patient.FirstName} {t.Patient.LastName}" : "",
        t.TherapistId, t.TherapistName, t.LibraryTreatmentId,
        t.Title, t.Description, t.Diagnosis, t.Goals,
        t.Status, t.StartDate, t.EndDate,
        t.TotalSessionsPlanned, t.CompletedSessions, t.Notes,
        t.Sessions?.Where(s => !s.IsDeleted).Select(s => MapSessionToDto(s)).ToList() ?? new(),
        t.CreatedAt, t.UpdatedAt
    );

    private static SessionDto MapSessionToDto(TherapySession s) => new(
        s.Id, s.TreatmentPlanId, s.PatientId,
        s.TherapistId, s.TherapistName, s.SessionNumber,
        s.SessionDate, s.DurationMinutes, s.Status,
        s.Notes, s.PatientProgress, s.PainLevel, s.PatientSatisfaction, s.CompletedAt
    );
}
