using Microsoft.EntityFrameworkCore;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Billing;
using Rehably.Application.Services.Billing;
using Rehably.Domain.Entities.Billing;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Services.Billing;

public class InsuranceService : IInsuranceService
{
    private readonly ApplicationDbContext _db;

    public InsuranceService(ApplicationDbContext db) => _db = db;

    public async Task<PagedResult<InsuranceProviderDto>> GetGlobalProvidersAsync(InsuranceQueryParams query)
    {
        var q = _db.InsuranceProviders.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(p => p.Name.Contains(query.Search) || (p.NameArabic != null && p.NameArabic.Contains(query.Search)));
        if (query.IsActive.HasValue) q = q.Where(p => p.IsActive == query.IsActive);
        if (!string.IsNullOrWhiteSpace(query.Country)) q = q.Where(p => p.Country == query.Country);

        var total = await q.CountAsync();
        var items = await q.OrderBy(p => p.Name)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(p => new InsuranceProviderDto(p.Id, p.Name, p.NameArabic, p.Country, p.LogoUrl, p.IsGlobal, p.IsActive))
            .ToListAsync();
        return new PagedResult<InsuranceProviderDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<List<ClinicInsuranceProviderDto>> GetClinicProvidersAsync(Guid clinicId)
    {
        return await _db.ClinicInsuranceProviders
            .Include(c => c.Provider).Include(c => c.ServiceRules)
            .Where(c => c.ClinicId == clinicId).OrderBy(c => c.Provider.Name)
            .Select(c => MapClinicProvider(c)).ToListAsync();
    }

    public async Task<ClinicInsuranceProviderDto?> GetClinicProviderByIdAsync(Guid clinicId, Guid id)
    {
        var e = await _db.ClinicInsuranceProviders
            .Include(c => c.Provider).Include(c => c.ServiceRules)
            .FirstOrDefaultAsync(c => c.ClinicId == clinicId && c.Id == id);
        return e == null ? null : MapClinicProvider(e);
    }

    public async Task<ClinicInsuranceProviderDto> ActivateProviderAsync(Guid clinicId, ActivateInsuranceProviderRequest request)
    {
        var entity = new ClinicInsuranceProvider
        {
            Id = Guid.NewGuid(), ClinicId = clinicId,
            InsuranceProviderId = request.InsuranceProviderId,
            PreAuthRequired = request.PreAuthRequired,
            DefaultCoveragePercent = request.DefaultCoveragePercent,
            Notes = request.Notes, IsActive = true
        };
        if (request.ServiceRules != null)
            entity.ServiceRules = request.ServiceRules.Select(r => new InsuranceServiceRule
            {
                Id = Guid.NewGuid(), ClinicInsuranceProviderId = entity.Id,
                ServiceType = r.ServiceType, CoverageType = r.CoverageType,
                CoverageValue = r.CoverageValue, Notes = r.Notes
            }).ToList();

        _db.ClinicInsuranceProviders.Add(entity);
        await _db.SaveChangesAsync();
        await _db.Entry(entity).Reference(e => e.Provider).LoadAsync();
        return MapClinicProvider(entity);
    }

    public async Task<ClinicInsuranceProviderDto> UpdateClinicProviderAsync(Guid clinicId, Guid id, UpdateClinicInsuranceProviderRequest request)
    {
        var entity = await _db.ClinicInsuranceProviders
            .Include(c => c.Provider).Include(c => c.ServiceRules)
            .FirstOrDefaultAsync(c => c.ClinicId == clinicId && c.Id == id)
            ?? throw new KeyNotFoundException("Insurance provider not found");

        entity.PreAuthRequired = request.PreAuthRequired;
        entity.DefaultCoveragePercent = request.DefaultCoveragePercent;
        entity.Notes = request.Notes;

        if (request.ServiceRules != null)
        {
            _db.InsuranceServiceRules.RemoveRange(entity.ServiceRules);
            entity.ServiceRules = request.ServiceRules.Select(r => new InsuranceServiceRule
            {
                Id = Guid.NewGuid(), ClinicInsuranceProviderId = entity.Id,
                ServiceType = r.ServiceType, CoverageType = r.CoverageType,
                CoverageValue = r.CoverageValue, Notes = r.Notes
            }).ToList();
        }
        await _db.SaveChangesAsync();
        return MapClinicProvider(entity);
    }

    public async Task DeactivateClinicProviderAsync(Guid clinicId, Guid id)
    {
        var entity = await _db.ClinicInsuranceProviders.FirstOrDefaultAsync(c => c.ClinicId == clinicId && c.Id == id)
            ?? throw new KeyNotFoundException("Insurance provider not found");
        entity.IsActive = false;
        await _db.SaveChangesAsync();
    }

    public async Task<List<PatientInsuranceDto>> GetPatientInsurancesAsync(Guid clinicId, Guid patientId)
    {
        return await _db.PatientInsurances
            .Include(p => p.ClinicInsuranceProvider).ThenInclude(c => c.Provider)
            .Where(p => p.PatientId == patientId && p.ClinicInsuranceProvider.ClinicId == clinicId)
            .Select(p => MapPatientInsurance(p)).ToListAsync();
    }

    public async Task<PatientInsuranceDto> AddPatientInsuranceAsync(Guid clinicId, AddPatientInsuranceRequest request)
    {
        var entity = new PatientInsurance
        {
            Id = Guid.NewGuid(), PatientId = request.PatientId,
            ClinicInsuranceProviderId = request.ClinicInsuranceProviderId,
            PolicyNumber = request.PolicyNumber, MembershipId = request.MembershipId,
            HolderName = request.HolderName, CoveragePercent = request.CoveragePercent,
            MaxAnnualCoverageAmount = request.MaxAnnualCoverageAmount,
            ExpiryDate = request.ExpiryDate, IsActive = true
        };
        _db.PatientInsurances.Add(entity);
        await _db.SaveChangesAsync();
        await _db.Entry(entity).Reference(e => e.ClinicInsuranceProvider).LoadAsync();
        await _db.Entry(entity.ClinicInsuranceProvider).Reference(e => e.Provider).LoadAsync();
        return MapPatientInsurance(entity);
    }

    public async Task<PatientInsuranceDto> UpdatePatientInsuranceAsync(Guid clinicId, Guid id, UpdatePatientInsuranceRequest request)
    {
        var entity = await _db.PatientInsurances
            .Include(p => p.ClinicInsuranceProvider).ThenInclude(c => c.Provider)
            .FirstOrDefaultAsync(p => p.Id == id && p.ClinicInsuranceProvider.ClinicId == clinicId)
            ?? throw new KeyNotFoundException("Patient insurance not found");

        entity.PolicyNumber = request.PolicyNumber;
        entity.MembershipId = request.MembershipId;
        entity.HolderName = request.HolderName;
        entity.CoveragePercent = request.CoveragePercent;
        entity.MaxAnnualCoverageAmount = request.MaxAnnualCoverageAmount;
        entity.ExpiryDate = request.ExpiryDate;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync();
        return MapPatientInsurance(entity);
    }

    public async Task DeletePatientInsuranceAsync(Guid clinicId, Guid id)
    {
        var entity = await _db.PatientInsurances
            .Include(p => p.ClinicInsuranceProvider)
            .FirstOrDefaultAsync(p => p.Id == id && p.ClinicInsuranceProvider.ClinicId == clinicId)
            ?? throw new KeyNotFoundException("Patient insurance not found");
        _db.PatientInsurances.Remove(entity);
        await _db.SaveChangesAsync();
    }

    public async Task<PagedResult<InsuranceClaimDto>> GetClaimsAsync(Guid clinicId, ClaimQueryParams query)
    {
        var q = _db.InsuranceClaims
            .Include(c => c.PatientInsurance).ThenInclude(p => p.ClinicInsuranceProvider).ThenInclude(cp => cp.Provider)
            .Where(c => c.ClinicId == clinicId);

        if (query.Status.HasValue) q = q.Where(c => c.Status == query.Status);
        if (query.PatientId.HasValue) q = q.Where(c => c.PatientId == query.PatientId);
        if (query.FromDate.HasValue) q = q.Where(c => c.CreatedAt >= query.FromDate);
        if (query.ToDate.HasValue) q = q.Where(c => c.CreatedAt <= query.ToDate);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(c => c.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync();
        return new PagedResult<InsuranceClaimDto>(items.Select(MapClaim).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<InsuranceClaimDto?> GetClaimByIdAsync(Guid clinicId, Guid id)
    {
        var e = await _db.InsuranceClaims
            .Include(c => c.PatientInsurance).ThenInclude(p => p.ClinicInsuranceProvider).ThenInclude(cp => cp.Provider)
            .FirstOrDefaultAsync(c => c.ClinicId == clinicId && c.Id == id);
        return e == null ? null : MapClaim(e);
    }

    public async Task<InsuranceClaimDto> SubmitClaimAsync(Guid clinicId, SubmitClaimRequest request)
    {
        var pi = await _db.PatientInsurances.FindAsync(request.PatientInsuranceId)
            ?? throw new KeyNotFoundException("Patient insurance not found");

        var entity = new InsuranceClaim
        {
            Id = Guid.NewGuid(), ClinicId = clinicId,
            PatientId = pi.PatientId,
            PatientInsuranceId = request.PatientInsuranceId,
            InvoiceId = request.InvoiceId, Notes = request.Notes,
            Status = ClaimStatus.Submitted, SubmittedAt = DateTime.UtcNow
        };
        _db.InsuranceClaims.Add(entity);
        await _db.SaveChangesAsync();
        await _db.Entry(entity).Reference(e => e.PatientInsurance).LoadAsync();
        await _db.Entry(entity.PatientInsurance).Reference(e => e.ClinicInsuranceProvider).LoadAsync();
        await _db.Entry(entity.PatientInsurance.ClinicInsuranceProvider).Reference(e => e.Provider).LoadAsync();
        return MapClaim(entity);
    }

    public async Task<InsuranceClaimDto> UpdateClaimAsync(Guid clinicId, Guid id, UpdateClaimRequest request)
    {
        var entity = await _db.InsuranceClaims
            .Include(c => c.PatientInsurance).ThenInclude(p => p.ClinicInsuranceProvider).ThenInclude(cp => cp.Provider)
            .FirstOrDefaultAsync(c => c.ClinicId == clinicId && c.Id == id)
            ?? throw new KeyNotFoundException("Claim not found");

        entity.Status = request.Status;
        entity.ClaimNumber = request.ClaimNumber;
        entity.ApprovedAmount = request.ApprovedAmount;
        entity.PaidAmount = request.PaidAmount;
        entity.RejectedReason = request.RejectedReason;
        entity.Notes = request.Notes;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return MapClaim(entity);
    }

    private static ClinicInsuranceProviderDto MapClinicProvider(ClinicInsuranceProvider c) =>
        new(c.Id, c.InsuranceProviderId, c.Provider.Name, c.Provider.NameArabic,
            c.Provider.Country, c.Provider.LogoUrl, c.PreAuthRequired, c.DefaultCoveragePercent,
            c.IsActive, c.Notes,
            c.ServiceRules.Select(r => new InsuranceServiceRuleDto(r.Id, r.ServiceType, r.CoverageType, r.CoverageValue, r.Notes)).ToList());

    private static PatientInsuranceDto MapPatientInsurance(PatientInsurance p) =>
        new(p.Id, p.PatientId, p.ClinicInsuranceProviderId,
            p.ClinicInsuranceProvider.Provider.Name, p.ClinicInsuranceProvider.Provider.NameArabic,
            p.PolicyNumber, p.MembershipId, p.HolderName,
            p.CoveragePercent, p.MaxAnnualCoverageAmount, p.ExpiryDate, p.IsActive);

    private static InsuranceClaimDto MapClaim(InsuranceClaim c) =>
        new(c.Id, c.PatientId, string.Empty,
            c.PatientInsuranceId,
            c.PatientInsurance.ClinicInsuranceProvider.Provider.Name,
            c.InvoiceId, null, c.ClaimNumber, c.Status,
            c.SubmittedAt, c.ApprovedAmount, c.PaidAmount,
            c.RejectedReason, c.Notes, c.CreatedAt, c.UpdatedAt);
}
