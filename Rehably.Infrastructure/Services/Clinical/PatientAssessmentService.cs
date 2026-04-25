using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.Contexts;
using Rehably.Application.DTOs.Clinical;
using Rehably.Application.Services.Clinical;
using Rehably.Domain.Entities.Clinical;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Services.Clinical;

public class PatientAssessmentService : IPatientAssessmentService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ILogger<PatientAssessmentService> _logger;

    public PatientAssessmentService(
        ApplicationDbContext db,
        ITenantContext tenant,
        ILogger<PatientAssessmentService> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    // ─── Start ────────────────────────────────────────────────────────────────

    public async Task<Result<AssessmentDetailDto>> StartAsync(
        Guid appointmentId,
        CreateAssessmentRequest req,
        CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenant.GetCurrentTenantId();
            var appointment = await _db.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.ClinicId == clinicId, ct);

            if (appointment is null)
                return Result<AssessmentDetailDto>.Failure("Appointment not found.");

            if (appointment.Status != AppointmentStatus.CheckedIn)
                return Result<AssessmentDetailDto>.Failure(
                    "Assessment can only be started for a checked-in appointment.");

            // Idempotent — return existing draft if already started
            var existing = await LoadFullAsync(appointmentId, clinicId, ct);
            if (existing is not null)
                return Result<AssessmentDetailDto>.Success(MapToDetail(existing));

            var assessment = new PatientAssessment
            {
                ClinicId = clinicId,
                AppointmentId = appointmentId,
                PatientId = appointment.PatientId,
                TherapistId = appointment.TherapistId,
                TherapistName = appointment.TherapistName,
                SpecialityId = req.SpecialityId,
                BodyRegionCategoryId = req.BodyRegionCategoryId,
                DiagnosisId = req.DiagnosisId,
                DiagnosisFreeText = req.DiagnosisFreeText,
                PatientAge = req.PatientAge,
                Gender = req.Gender,
                HasPostOp = req.HasPostOp,
                AdditionalNotes = req.AdditionalNotes,
                Status = AssessmentStatus.Draft,
            };

            _db.PatientAssessments.Add(assessment);

            // Transition appointment to InProgress
            appointment.Status = AppointmentStatus.InProgress;

            await _db.SaveChangesAsync(ct);

            var loaded = await LoadFullAsync(assessment.Id, ct);
            return Result<AssessmentDetailDto>.Success(MapToDetail(loaded!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StartAsync failed for appointment {Id}", appointmentId);
            return Result<AssessmentDetailDto>.Failure("Failed to start assessment.");
        }
    }

    // ─── Get ──────────────────────────────────────────────────────────────────

    public async Task<Result<AssessmentDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var clinicId = _tenant.GetCurrentTenantId();
        var assessment = await LoadFullAsync(id, ct);

        if (assessment is null || assessment.ClinicId != clinicId)
            return Result<AssessmentDetailDto>.Failure("Assessment not found.");

        return Result<AssessmentDetailDto>.Success(MapToDetail(assessment));
    }

    public async Task<Result<AssessmentDetailDto?>> GetByAppointmentAsync(Guid appointmentId, CancellationToken ct = default)
    {
        var clinicId = _tenant.GetCurrentTenantId();
        var assessment = await LoadFullAsync(appointmentId, clinicId, ct);
        return Result<AssessmentDetailDto?>.Success(assessment is null ? null : MapToDetail(assessment));
    }

    public async Task<Result<List<AssessmentSummaryDto>>> GetByPatientAsync(Guid patientId, CancellationToken ct = default)
    {
        var clinicId = _tenant.GetCurrentTenantId();
        var list = await _db.PatientAssessments
            .AsNoTracking()
            .Include(a => a.Speciality)
            .Include(a => a.Diagnosis)
            .Where(a => a.ClinicId == clinicId && a.PatientId == patientId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AssessmentSummaryDto(
                a.Id,
                a.AppointmentId,
                a.PatientId,
                a.TherapistName,
                a.Speciality.NameAr,
                a.Diagnosis != null ? a.Diagnosis.NameAr : null,
                a.DiagnosisFreeText,
                a.Status.ToString(),
                a.CreatedAt,
                a.SubmittedAt))
            .ToListAsync(ct);

        return Result<List<AssessmentSummaryDto>>.Success(list);
    }

    // ─── Step saves ───────────────────────────────────────────────────────────

    public async Task<Result<AssessmentDetailDto>> SaveStep1Async(
        Guid id, UpdateStep1Request req, CancellationToken ct = default)
    {
        var (assessment, err) = await GetDraftAsync(id, ct);
        if (assessment is null) return Result<AssessmentDetailDto>.Failure(err!);

        assessment.SpecialityId = req.SpecialityId;
        assessment.BodyRegionCategoryId = req.BodyRegionCategoryId;
        assessment.DiagnosisId = req.DiagnosisId;
        assessment.DiagnosisFreeText = req.DiagnosisFreeText;
        assessment.PatientAge = req.PatientAge;
        assessment.Gender = req.Gender;
        assessment.HasPostOp = req.HasPostOp;
        assessment.AdditionalNotes = req.AdditionalNotes;
        assessment.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Result<AssessmentDetailDto>.Success(MapToDetail(await LoadFullAsync(id, ct)!));
    }

    public async Task<Result<AssessmentDetailDto>> SaveStep2Async(
        Guid id, StepPostOpDto req, CancellationToken ct = default)
    {
        var (assessment, err) = await GetDraftAsync(id, ct);
        if (assessment is null) return Result<AssessmentDetailDto>.Failure(err!);

        var postOp = assessment.PostOp ?? new AssessmentPostOp { AssessmentId = id };
        postOp.ProcedureName = req.ProcedureName;
        postOp.ProcedureSide = req.ProcedureSide;
        postOp.SurgeryDate = req.SurgeryDate;
        postOp.DaysPostOp = req.DaysPostOp;
        postOp.SurgeonFacility = req.SurgeonFacility;
        postOp.WeightBearingStatus = req.WeightBearingStatus;
        postOp.RomRestriction = req.RomRestriction;
        postOp.PostOpPrecautions = req.PostOpPrecautions;
        postOp.WoundStatus = req.WoundStatus;
        postOp.Notes = req.Notes;

        if (assessment.PostOp is null) _db.Set<AssessmentPostOp>().Add(postOp);

        assessment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result<AssessmentDetailDto>.Success(MapToDetail(await LoadFullAsync(id, ct)!));
    }

    public async Task<Result<AssessmentDetailDto>> SaveStep3Async(
        Guid id, StepRedFlagsDto req, CancellationToken ct = default)
    {
        var (assessment, err) = await GetDraftAsync(id, ct);
        if (assessment is null) return Result<AssessmentDetailDto>.Failure(err!);

        var redFlags = assessment.RedFlags ?? new AssessmentRedFlags { AssessmentId = id };
        redFlags.Flags = req.Flags;
        redFlags.Decision = req.Decision;
        redFlags.DecisionNotes = req.DecisionNotes;
        redFlags.ActionsTaken = req.ActionsTaken;
        redFlags.ActionNotes = req.ActionNotes;

        if (assessment.RedFlags is null) _db.Set<AssessmentRedFlags>().Add(redFlags);

        assessment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result<AssessmentDetailDto>.Success(MapToDetail(await LoadFullAsync(id, ct)!));
    }

    public async Task<Result<AssessmentDetailDto>> SaveStep4Async(
        Guid id, StepSubjectiveDto req, CancellationToken ct = default)
    {
        var (assessment, err) = await GetDraftAsync(id, ct);
        if (assessment is null) return Result<AssessmentDetailDto>.Failure(err!);

        var sub = assessment.Subjective ?? new AssessmentSubjective { AssessmentId = id };
        sub.ChiefComplaint = req.ChiefComplaint;
        sub.OnsetMechanism = req.OnsetMechanism;
        sub.PainNow = req.PainNow;
        sub.PainBest = req.PainBest;
        sub.PainWorst = req.PainWorst;
        sub.NightPain = req.NightPain;
        sub.MorningStiffness = req.MorningStiffness;
        sub.PainPattern24h = req.PainPattern24h;
        sub.AggravatIngFactors = req.AggravatIngFactors;
        sub.EasingFactors = req.EasingFactors;
        sub.FunctionalLimits = req.FunctionalLimits;
        sub.PreviousInjuries = req.PreviousInjuries;
        sub.MedicalHistory = req.MedicalHistory;
        sub.Medications = req.Medications;
        sub.ScreeningFlags = req.ScreeningFlags;
        sub.PatientGoals = req.PatientGoals;
        sub.AdditionalNotes = req.AdditionalNotes;

        if (assessment.Subjective is null) _db.Set<AssessmentSubjective>().Add(sub);

        assessment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result<AssessmentDetailDto>.Success(MapToDetail(await LoadFullAsync(id, ct)!));
    }

    public async Task<Result<AssessmentDetailDto>> SaveStep5Async(
        Guid id, StepObjectiveDto req, CancellationToken ct = default)
    {
        var (assessment, err) = await GetDraftAsync(id, ct);
        if (assessment is null) return Result<AssessmentDetailDto>.Failure(err!);

        var obj = assessment.Objective ?? new AssessmentObjective { AssessmentId = id };
        obj.Posture = req.Posture;
        obj.Swelling = req.Swelling;
        obj.Redness = req.Redness;
        obj.Deformity = req.Deformity;
        obj.Gait = req.Gait;
        obj.Transfers = req.Transfers;
        obj.AssistiveDevices = req.AssistiveDevices;
        obj.FunctionalTests = req.FunctionalTests;
        obj.StrengthData = req.StrengthData;
        obj.RomData = req.RomData;
        obj.AdditionalNotes = req.AdditionalNotes;

        if (assessment.Objective is null) _db.Set<AssessmentObjective>().Add(obj);

        assessment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result<AssessmentDetailDto>.Success(MapToDetail(await LoadFullAsync(id, ct)!));
    }

    public async Task<Result<AssessmentDetailDto>> SaveStep6Async(
        Guid id, StepNeuroDto req, CancellationToken ct = default)
    {
        var (assessment, err) = await GetDraftAsync(id, ct);
        if (assessment is null) return Result<AssessmentDetailDto>.Failure(err!);

        var neuro = assessment.Neuro ?? new AssessmentNeuro { AssessmentId = id };
        neuro.Sensation = req.Sensation;
        neuro.Numbness = req.Numbness;
        neuro.Tingling = req.Tingling;
        neuro.Myotomes = req.Myotomes;
        neuro.KeyMuscleWeakness = req.KeyMuscleWeakness;
        neuro.Reflexes = req.Reflexes;
        neuro.NeurovascularChecks = req.NeurovascularChecks;
        neuro.SpecialTests = req.SpecialTests;
        neuro.AdditionalNotes = req.AdditionalNotes;

        if (assessment.Neuro is null) _db.Set<AssessmentNeuro>().Add(neuro);

        assessment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result<AssessmentDetailDto>.Success(MapToDetail(await LoadFullAsync(id, ct)!));
    }

    public async Task<Result<AssessmentDetailDto>> SaveStep7Async(
        Guid id, StepClinicalReasoningDto req, CancellationToken ct = default)
    {
        var (assessment, err) = await GetDraftAsync(id, ct);
        if (assessment is null) return Result<AssessmentDetailDto>.Failure(err!);

        var cr = assessment.ClinicalReasoning ?? new AssessmentClinicalReasoning { AssessmentId = id };
        cr.ProblemList = req.ProblemList;
        cr.WorkingHypothesis = req.WorkingHypothesis;
        cr.SeverityIrritability = req.SeverityIrritability;
        cr.DifferentialConsiderations = req.DifferentialConsiderations;
        cr.DecisionPoints = req.DecisionPoints;
        cr.ImagingRequested = req.ImagingRequested;
        cr.ImagingReason = req.ImagingReason;
        cr.ReferralRequired = req.ReferralRequired;
        cr.ReferralTo = req.ReferralTo;
        cr.Urgency = req.Urgency;
        cr.BreakGlassUsed = req.BreakGlassUsed;
        cr.BreakGlassReason = req.BreakGlassReason;
        cr.AdditionalNotes = req.AdditionalNotes;

        if (assessment.ClinicalReasoning is null) _db.Set<AssessmentClinicalReasoning>().Add(cr);

        assessment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result<AssessmentDetailDto>.Success(MapToDetail(await LoadFullAsync(id, ct)!));
    }

    // ─── Submit ───────────────────────────────────────────────────────────────

    public async Task<Result<AssessmentDetailDto>> SubmitAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenant.GetCurrentTenantId();
            var assessment = await _db.PatientAssessments
                .Include(a => a.Speciality)
                .Include(a => a.Diagnosis)
                .FirstOrDefaultAsync(a => a.Id == id && a.ClinicId == clinicId, ct);

            if (assessment is null)
                return Result<AssessmentDetailDto>.Failure("Assessment not found.");

            if (assessment.Status != AssessmentStatus.Draft)
                return Result<AssessmentDetailDto>.Failure("Only draft assessments can be submitted.");

            assessment.Status = AssessmentStatus.Submitted;
            assessment.SubmittedAt = DateTime.UtcNow;
            assessment.UpdatedAt = DateTime.UtcNow;

            // Transition appointment to Completed
            var appointment = await _db.Appointments.FindAsync(new object[] { assessment.AppointmentId }, ct);
            if (appointment is not null && appointment.Status == AppointmentStatus.InProgress)
                appointment.Status = AppointmentStatus.Completed;

            await _db.SaveChangesAsync(ct);

            var loaded = await LoadFullAsync(id, ct);
            return Result<AssessmentDetailDto>.Success(MapToDetail(loaded!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SubmitAsync failed for assessment {Id}", id);
            return Result<AssessmentDetailDto>.Failure("Failed to submit assessment.");
        }
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    /// Load by assessment ID
    private Task<PatientAssessment?> LoadFullAsync(Guid assessmentId, CancellationToken ct) =>
        _db.PatientAssessments
            .Include(a => a.Speciality)
            .Include(a => a.Diagnosis)
            .Include(a => a.PostOp)
            .Include(a => a.RedFlags)
            .Include(a => a.Subjective)
            .Include(a => a.Objective)
            .Include(a => a.Neuro)
            .Include(a => a.ClinicalReasoning)
            .FirstOrDefaultAsync(a => a.Id == assessmentId, ct);

    /// Load by appointmentId + clinicId (for idempotent start)
    private Task<PatientAssessment?> LoadFullAsync(Guid appointmentId, Guid clinicId, CancellationToken ct) =>
        _db.PatientAssessments
            .Include(a => a.Speciality)
            .Include(a => a.Diagnosis)
            .Include(a => a.PostOp)
            .Include(a => a.RedFlags)
            .Include(a => a.Subjective)
            .Include(a => a.Objective)
            .Include(a => a.Neuro)
            .Include(a => a.ClinicalReasoning)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.ClinicId == clinicId, ct);

    private async Task<(PatientAssessment? assessment, string? error)> GetDraftAsync(Guid id, CancellationToken ct)
    {
        var clinicId = _tenant.GetCurrentTenantId();
        var a = await LoadFullAsync(id, ct);
        if (a is null || a.ClinicId != clinicId) return (null, "Assessment not found.");
        if (a.Status != AssessmentStatus.Draft) return (null, "Assessment is already submitted.");
        return (a, null);
    }

    // ─── Mapping ──────────────────────────────────────────────────────────────

    private static AssessmentDetailDto MapToDetail(PatientAssessment a) => new(
        a.Id,
        a.AppointmentId,
        a.PatientId,
        a.TherapistId,
        a.TherapistName,
        a.SpecialityId,
        a.Speciality?.NameAr,
        a.BodyRegionCategoryId,
        a.DiagnosisId,
        a.Diagnosis?.NameAr,
        a.DiagnosisFreeText,
        a.PatientAge,
        a.Gender,
        a.HasPostOp,
        a.AdditionalNotes,
        a.Status.ToString(),
        a.CreatedAt,
        a.SubmittedAt,
        a.PostOp is null ? null : new StepPostOpDto
        {
            ProcedureName = a.PostOp.ProcedureName,
            ProcedureSide = a.PostOp.ProcedureSide,
            SurgeryDate = a.PostOp.SurgeryDate,
            DaysPostOp = a.PostOp.DaysPostOp,
            SurgeonFacility = a.PostOp.SurgeonFacility,
            WeightBearingStatus = a.PostOp.WeightBearingStatus,
            RomRestriction = a.PostOp.RomRestriction,
            PostOpPrecautions = a.PostOp.PostOpPrecautions,
            WoundStatus = a.PostOp.WoundStatus,
            Notes = a.PostOp.Notes,
        },
        a.RedFlags is null ? null : new StepRedFlagsDto
        {
            Flags = a.RedFlags.Flags,
            Decision = a.RedFlags.Decision,
            DecisionNotes = a.RedFlags.DecisionNotes,
            ActionsTaken = a.RedFlags.ActionsTaken,
            ActionNotes = a.RedFlags.ActionNotes,
        },
        a.Subjective is null ? null : new StepSubjectiveDto
        {
            ChiefComplaint = a.Subjective.ChiefComplaint,
            OnsetMechanism = a.Subjective.OnsetMechanism,
            PainNow = a.Subjective.PainNow,
            PainBest = a.Subjective.PainBest,
            PainWorst = a.Subjective.PainWorst,
            NightPain = a.Subjective.NightPain,
            MorningStiffness = a.Subjective.MorningStiffness,
            PainPattern24h = a.Subjective.PainPattern24h,
            AggravatIngFactors = a.Subjective.AggravatIngFactors,
            EasingFactors = a.Subjective.EasingFactors,
            FunctionalLimits = a.Subjective.FunctionalLimits,
            PreviousInjuries = a.Subjective.PreviousInjuries,
            MedicalHistory = a.Subjective.MedicalHistory,
            Medications = a.Subjective.Medications,
            ScreeningFlags = a.Subjective.ScreeningFlags,
            PatientGoals = a.Subjective.PatientGoals,
            AdditionalNotes = a.Subjective.AdditionalNotes,
        },
        a.Objective is null ? null : new StepObjectiveDto
        {
            Posture = a.Objective.Posture,
            Swelling = a.Objective.Swelling,
            Redness = a.Objective.Redness,
            Deformity = a.Objective.Deformity,
            Gait = a.Objective.Gait,
            Transfers = a.Objective.Transfers,
            AssistiveDevices = a.Objective.AssistiveDevices,
            FunctionalTests = a.Objective.FunctionalTests,
            StrengthData = a.Objective.StrengthData,
            RomData = a.Objective.RomData,
            AdditionalNotes = a.Objective.AdditionalNotes,
        },
        a.Neuro is null ? null : new StepNeuroDto
        {
            Sensation = a.Neuro.Sensation,
            Numbness = a.Neuro.Numbness,
            Tingling = a.Neuro.Tingling,
            Myotomes = a.Neuro.Myotomes,
            KeyMuscleWeakness = a.Neuro.KeyMuscleWeakness,
            Reflexes = a.Neuro.Reflexes,
            NeurovascularChecks = a.Neuro.NeurovascularChecks,
            SpecialTests = a.Neuro.SpecialTests,
            AdditionalNotes = a.Neuro.AdditionalNotes,
        },
        a.ClinicalReasoning is null ? null : new StepClinicalReasoningDto
        {
            ProblemList = a.ClinicalReasoning.ProblemList,
            WorkingHypothesis = a.ClinicalReasoning.WorkingHypothesis,
            SeverityIrritability = a.ClinicalReasoning.SeverityIrritability,
            DifferentialConsiderations = a.ClinicalReasoning.DifferentialConsiderations,
            DecisionPoints = a.ClinicalReasoning.DecisionPoints,
            ImagingRequested = a.ClinicalReasoning.ImagingRequested,
            ImagingReason = a.ClinicalReasoning.ImagingReason,
            ReferralRequired = a.ClinicalReasoning.ReferralRequired,
            ReferralTo = a.ClinicalReasoning.ReferralTo,
            Urgency = a.ClinicalReasoning.Urgency,
            BreakGlassUsed = a.ClinicalReasoning.BreakGlassUsed,
            BreakGlassReason = a.ClinicalReasoning.BreakGlassReason,
            AdditionalNotes = a.ClinicalReasoning.AdditionalNotes,
        }
    );
}
