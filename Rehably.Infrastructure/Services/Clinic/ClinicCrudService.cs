using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.Contexts;
using Rehably.Application.DTOs.Clinic;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Clinic;
using Rehably.Domain.Entities.Identity;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Infrastructure.Services.Clinic;

public class ClinicCrudService : IClinicCrudService
{
    private readonly IClinicRepository _clinicRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ClinicCrudService> _logger;

    public ClinicCrudService(
        IClinicRepository clinicRepository,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        ILogger<ClinicCrudService> logger)
    {
        _clinicRepository = clinicRepository;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Result<ClinicResponse>> GetClinicByIdAsync(Guid id)
    {
        var clinic = await _clinicRepository.GetWithSubscriptionAsync(id);

        if (clinic == null)
        {
            return Result<ClinicResponse>.Failure("Clinic not found");
        }

        var response = await EnrichWithOwnerAsync(MapToResponse(clinic), id);
        return Result<ClinicResponse>.Success(response);
    }

    public async Task<Result<ClinicResponse>> UpdateClinicAsync(Guid id, UpdateClinicRequest request)
    {
        ValidateTenantAccess(id);
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var clinic = await _clinicRepository.GetByIdAsync(id);
            if (clinic == null)
            {
                return Result<ClinicResponse>.Failure("Clinic not found");
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
                clinic.Name = request.Name;
            if (!string.IsNullOrWhiteSpace(request.NameArabic))
                clinic.NameArabic = request.NameArabic;
            if (!string.IsNullOrWhiteSpace(request.Description))
                clinic.Description = request.Description;
            if (!string.IsNullOrWhiteSpace(request.Phone))
                clinic.Phone = request.Phone;
            if (!string.IsNullOrWhiteSpace(request.Email))
                clinic.Email = request.Email;
            if (!string.IsNullOrWhiteSpace(request.Address))
                clinic.Address = request.Address;
            if (!string.IsNullOrWhiteSpace(request.City))
                clinic.City = request.City;
            if (!string.IsNullOrWhiteSpace(request.Country))
                clinic.Country = request.Country;
            clinic.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            clinic = await _clinicRepository.GetWithSubscriptionAsync(id);
            var response = await EnrichWithOwnerAsync(MapToResponse(clinic!), id);
            return Result<ClinicResponse>.Success(response);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<Result> DeleteClinicAsync(Guid id)
    {
        ValidateTenantAccess(id);
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var clinic = await _clinicRepository.GetByIdAsync(id);
            if (clinic == null)
            {
                return Result.Failure("Clinic not found");
            }

            clinic.IsDeleted = true;
            clinic.DeletedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return Result.Success();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<Result<ClinicResponse>> GetMyClinicAsync()
    {
        var clinicId = _tenantContext.TenantId;
        if (!clinicId.HasValue)
        {
            return Result<ClinicResponse>.Failure("No clinic associated with current user");
        }

        var clinic = await _clinicRepository.GetWithSubscriptionAsync(clinicId.Value);

        if (clinic == null)
        {
            return Result<ClinicResponse>.Failure("Clinic not found");
        }

        var response = await EnrichWithOwnerAsync(MapToResponse(clinic), clinicId.Value);
        return Result<ClinicResponse>.Success(response);
    }

    public async Task<Result<ClinicResponse>> UpdateMyClinicAsync(UpdateClinicRequest request)
    {
        var clinicId = _tenantContext.TenantId;
        if (!clinicId.HasValue)
        {
            return Result<ClinicResponse>.Failure("No clinic associated with current user");
        }

        return await UpdateClinicAsync(clinicId.Value, request);
    }

    private void ValidateTenantAccess(Guid requestedClinicId)
    {
        var currentTenantId = _tenantContext.TenantId;
        if (currentTenantId.HasValue && currentTenantId.Value != requestedClinicId)
        {
            _logger.LogWarning("Tenant {CurrentTenantId} attempted to access data for {RequestedClinicId}", currentTenantId, requestedClinicId);
            throw new UnauthorizedAccessException("Cross-tenant access denied.");
        }
    }

    private async Task<ClinicResponse> EnrichWithOwnerAsync(ClinicResponse response, Guid clinicId)
    {
        var owner = await _userManager.Users
            .Where(u => u.RoleType == RoleType.ClinicOwner && u.ClinicId == clinicId)
            .Select(u => new { u.FirstName, u.LastName, u.Email })
            .FirstOrDefaultAsync();

        if (owner != null)
        {
            return response with
            {
                OwnerFirstName = owner.FirstName,
                OwnerLastName = owner.LastName,
                OwnerEmail = owner.Email
            };
        }

        return response;
    }

    private ClinicResponse MapToResponse(Domain.Entities.Tenant.Clinic clinic)
    {
        return new ClinicResponse
        {
            Id = clinic.Id,
            Name = clinic.Name,
            NameArabic = clinic.NameArabic,
            Slug = clinic.Slug,
            LogoUrl = clinic.LogoUrl,
            Description = clinic.Description,
            Phone = clinic.Phone,
            Email = clinic.Email,
            Address = clinic.Address,
            City = clinic.City,
            Country = clinic.Country,
            Status = clinic.Status,
            IsDeleted = clinic.IsDeleted,
            DeletedAt = clinic.DeletedAt,
            IsBanned = clinic.IsBanned,
            BanReason = clinic.BanReason,
            BannedAt = clinic.BannedAt,
            BannedBy = clinic.BannedBy,
            SubscriptionPlanId = clinic.CurrentSubscription?.PackageId,
            SubscriptionPlanName = clinic.CurrentSubscription?.Package?.Name,
            SubscriptionStatus = clinic.CurrentSubscription?.Status ?? SubscriptionStatus.Expired,
            SubscriptionStartDate = clinic.CurrentSubscription?.StartDate ?? DateTime.MinValue,
            SubscriptionEndDate = clinic.CurrentSubscription?.EndDate,
            TrialEndDate = clinic.CurrentSubscription?.TrialEndsAt,
            StorageUsedBytes = clinic.StorageUsedBytes,
            StorageLimitBytes = clinic.StorageLimitBytes,
            PatientsCount = clinic.PatientsCount,
            PatientsLimit = clinic.PatientsLimit,
            UsersCount = clinic.UsersCount,
            UsersLimit = clinic.UsersLimit,
            PaymentMethod = clinic.CurrentSubscription?.PaymentType.ToString(),
            PackageFeatures = clinic.CurrentSubscription?.Package?.Features?
                .Where(pf => pf.IsIncluded && pf.Feature != null)
                .Select(pf => new ClinicSubscriptionFeatureDto
                {
                    FeatureId = pf.FeatureId,
                    Name = pf.Feature.Name,
                    Code = pf.Feature.Code,
                    IsIncluded = pf.IsIncluded,
                    Limit = pf.Quantity ?? pf.Limit
                }).ToList() ?? [],
            Documents = clinic.Documents?.Select(d => new ClinicDocumentDto
            {
                Id = d.Id,
                Type = d.DocumentType.ToString(),
                FileUrl = d.PublicUrl ?? d.StorageUrl,
                UploadedAt = d.UploadedAt,
                VerificationStatus = d.Status.ToString(),
                RejectionReason = d.RejectionReason
            }).ToList() ?? [],
            CreatedAt = clinic.CreatedAt,
            UpdatedAt = clinic.UpdatedAt
        };
    }
}
