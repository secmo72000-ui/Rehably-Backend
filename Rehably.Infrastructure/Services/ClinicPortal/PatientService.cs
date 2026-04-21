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

public class PatientService : IPatientService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<PatientService> _logger;

    public PatientService(ApplicationDbContext context, ITenantContext tenantContext, ILogger<PatientService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<PagedResult<PatientListDto>>> GetAllAsync(PatientQueryParams query, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();

            var q = _context.Patients.AsNoTracking().Where(p => p.ClinicId == clinicId);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.ToLower();
                q = q.Where(p =>
                    p.FirstName.ToLower().Contains(search) ||
                    p.LastName.ToLower().Contains(search) ||
                    (p.Phone != null && p.Phone.Contains(search)) ||
                    (p.Email != null && p.Email.ToLower().Contains(search)));
            }

            if (query.Status.HasValue)
                q = q.Where(p => p.Status == query.Status.Value);

            var totalCount = await q.CountAsync(ct);

            var items = await q
                .OrderByDescending(p => p.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(p => new PatientListDto(
                    p.Id,
                    p.FirstName,
                    p.LastName,
                    p.FirstNameArabic,
                    p.LastNameArabic,
                    p.Phone,
                    p.Email,
                    p.DateOfBirth,
                    p.Gender.ToString(),
                    p.Status,
                    p.Diagnosis,
                    p.Appointments.Count(a => !a.IsDeleted),
                    p.TreatmentPlans.Count(t => !t.IsDeleted && t.Status == TreatmentPlanStatus.Active),
                    p.CreatedAt
                ))
                .ToListAsync(ct);

            return Result<PagedResult<PatientListDto>>.Success(
                PagedResult<PatientListDto>.Create(items, totalCount, query.Page, query.PageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting patients for clinic {ClinicId}", _tenantContext.TenantId);
            return Result<PagedResult<PatientListDto>>.Failure("Failed to retrieve patients");
        }
    }

    public async Task<Result<PatientDetailDto>> GetByIdAsync(Guid patientId, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();
            var patient = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == patientId && p.ClinicId == clinicId, ct);

            if (patient == null)
                return Result<PatientDetailDto>.Failure("Patient not found");

            return Result<PatientDetailDto>.Success(MapToDetail(patient));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting patient {PatientId}", patientId);
            return Result<PatientDetailDto>.Failure("Failed to retrieve patient");
        }
    }

    public async Task<Result<PatientDetailDto>> CreateAsync(CreatePatientRequest request, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();

            var patient = new Patient
            {
                ClinicId = clinicId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                FirstNameArabic = request.FirstNameArabic,
                LastNameArabic = request.LastNameArabic,
                NationalId = request.NationalId,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                Phone = request.Phone,
                Email = request.Email,
                Address = request.Address,
                City = request.City,
                EmergencyContactName = request.EmergencyContactName,
                EmergencyContactPhone = request.EmergencyContactPhone,
                EmergencyContactRelation = request.EmergencyContactRelation,
                Diagnosis = request.Diagnosis,
                MedicalHistory = request.MedicalHistory,
                Allergies = request.Allergies,
                CurrentMedications = request.CurrentMedications,
                Notes = request.Notes,
                Status = PatientStatus.Active,
            };

            _context.Patients.Add(patient);

            var clinic = await _context.Clinics.FindAsync(new object[] { clinicId }, ct);
            if (clinic != null) clinic.PatientsCount++;

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Patient {PatientId} created for clinic {ClinicId}", patient.Id, clinicId);
            return Result<PatientDetailDto>.Success(MapToDetail(patient));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating patient for clinic {ClinicId}", _tenantContext.TenantId);
            return Result<PatientDetailDto>.Failure("Failed to create patient");
        }
    }

    public async Task<Result<PatientDetailDto>> UpdateAsync(Guid patientId, UpdatePatientRequest request, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == patientId && p.ClinicId == clinicId, ct);

            if (patient == null)
                return Result<PatientDetailDto>.Failure("Patient not found");

            if (request.FirstName != null) patient.FirstName = request.FirstName;
            if (request.LastName != null) patient.LastName = request.LastName;
            if (request.FirstNameArabic != null) patient.FirstNameArabic = request.FirstNameArabic;
            if (request.LastNameArabic != null) patient.LastNameArabic = request.LastNameArabic;
            if (request.NationalId != null) patient.NationalId = request.NationalId;
            if (request.DateOfBirth.HasValue) patient.DateOfBirth = request.DateOfBirth;
            if (request.Gender.HasValue) patient.Gender = request.Gender.Value;
            if (request.Phone != null) patient.Phone = request.Phone;
            if (request.Email != null) patient.Email = request.Email;
            if (request.Address != null) patient.Address = request.Address;
            if (request.City != null) patient.City = request.City;
            if (request.EmergencyContactName != null) patient.EmergencyContactName = request.EmergencyContactName;
            if (request.EmergencyContactPhone != null) patient.EmergencyContactPhone = request.EmergencyContactPhone;
            if (request.EmergencyContactRelation != null) patient.EmergencyContactRelation = request.EmergencyContactRelation;
            if (request.Diagnosis != null) patient.Diagnosis = request.Diagnosis;
            if (request.MedicalHistory != null) patient.MedicalHistory = request.MedicalHistory;
            if (request.Allergies != null) patient.Allergies = request.Allergies;
            if (request.CurrentMedications != null) patient.CurrentMedications = request.CurrentMedications;
            if (request.Notes != null) patient.Notes = request.Notes;
            patient.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return Result<PatientDetailDto>.Success(MapToDetail(patient));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating patient {PatientId}", patientId);
            return Result<PatientDetailDto>.Failure("Failed to update patient");
        }
    }

    public async Task<Result> DeleteAsync(Guid patientId, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == patientId && p.ClinicId == clinicId, ct);

            if (patient == null)
                return Result.Failure("Patient not found");

            patient.IsDeleted = true;
            patient.DeletedAt = DateTime.UtcNow;
            patient.UpdatedAt = DateTime.UtcNow;

            var clinic = await _context.Clinics.FindAsync(new object[] { clinicId }, ct);
            if (clinic != null && clinic.PatientsCount > 0) clinic.PatientsCount--;

            await _context.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting patient {PatientId}", patientId);
            return Result.Failure("Failed to delete patient");
        }
    }

    public async Task<Result<PatientDetailDto>> DischargeAsync(Guid patientId, CancellationToken ct = default)
    {
        try
        {
            var clinicId = _tenantContext.GetCurrentTenantId();
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == patientId && p.ClinicId == clinicId, ct);

            if (patient == null)
                return Result<PatientDetailDto>.Failure("Patient not found");

            patient.Status = PatientStatus.Discharged;
            patient.DischargedAt = DateTime.UtcNow;
            patient.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return Result<PatientDetailDto>.Success(MapToDetail(patient));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discharging patient {PatientId}", patientId);
            return Result<PatientDetailDto>.Failure("Failed to discharge patient");
        }
    }

    // ============ Mapping ============

    private static PatientDetailDto MapToDetail(Patient p) => new(
        p.Id, p.ClinicId, p.FirstName, p.LastName,
        p.FirstNameArabic, p.LastNameArabic, p.NationalId,
        p.DateOfBirth, p.Gender.ToString(), p.Phone, p.Email,
        p.Address, p.City,
        p.EmergencyContactName, p.EmergencyContactPhone, p.EmergencyContactRelation,
        p.Diagnosis, p.MedicalHistory, p.Allergies, p.CurrentMedications,
        p.Notes, p.ProfileImageUrl, p.Status, p.DischargedAt,
        p.CreatedAt, p.UpdatedAt
    );
}
