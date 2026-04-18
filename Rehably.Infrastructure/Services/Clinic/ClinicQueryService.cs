using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.Contexts;
using Rehably.Application.DTOs.Clinic;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Clinic;
using Rehably.Domain.Entities.Identity;
using Rehably.Domain.Enums;

namespace Rehably.Infrastructure.Services.Clinic;

/// <summary>
/// Implementation of clinic query operations.
/// </summary>
public class ClinicQueryService : IClinicQueryService
{
    private readonly IClinicRepository _clinicRepository;
    private readonly ITenantContext _tenantContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ClinicQueryService> _logger;

    public ClinicQueryService(
        IClinicRepository clinicRepository,
        ITenantContext tenantContext,
        UserManager<ApplicationUser> userManager,
        ILogger<ClinicQueryService> logger)
    {
        _clinicRepository = clinicRepository;
        _tenantContext = tenantContext;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Result<PagedResult<ClinicResponse>>> GetAllClinicsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var (clinics, _) = await _clinicRepository.SearchAsync(
                includeDeleted: false,
                sortBy: "createdat",
                sortDesc: true,
                page: page,
                pageSize: pageSize);

            var responses = await EnrichWithOwnersAsync(clinics.Adapt<List<ClinicResponse>>(), clinics.Select(c => c.Id).ToList());
            var totalCount = await _clinicRepository.CountAsync();

            return Result<PagedResult<ClinicResponse>>.Success(
                PagedResult<ClinicResponse>.Create(responses, page, pageSize, totalCount));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all clinics");
            return Result<PagedResult<ClinicResponse>>.Failure("Failed to get clinics");
        }
    }

    public async Task<Result<PagedResult<ClinicResponse>>> SearchClinicsAsync(GetClinicsQuery query)
    {
        try
        {
            var (clinics, totalCount) = await _clinicRepository.SearchAsync(
                search: query.Search,
                status: query.Status,
                subscriptionStatus: query.SubscriptionStatus,
                packageId: query.PackageId,
                includeDeleted: query.IncludeDeleted,
                sortBy: query.SortBy,
                sortDesc: query.SortDesc,
                page: query.Page,
                pageSize: query.PageSize);

            var clinicResponses = await EnrichWithOwnersAsync(clinics.Adapt<List<ClinicResponse>>(), clinics.Select(c => c.Id).ToList());
            var pagedResult = PagedResult<ClinicResponse>.Create(
                clinicResponses,
                totalCount,
                query.Page,
                query.PageSize
            );

            return Result<PagedResult<ClinicResponse>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search clinics with query: {@Query}", query);
            return Result<PagedResult<ClinicResponse>>.Failure($"Failed to search clinics: {ex.Message}");
        }
    }

    public async Task<Result<List<ClinicResponse>>> GetPendingClinicsAsync()
    {
        try
        {
            var pendingClinics = await _clinicRepository.GetPendingApprovalAsync();
            var responses = await EnrichWithOwnersAsync(pendingClinics.Adapt<List<ClinicResponse>>(), pendingClinics.Select(c => c.Id).ToList());
            return Result<List<ClinicResponse>>.Success(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending clinics");
            return Result<List<ClinicResponse>>.Failure("Failed to get pending clinics");
        }
    }

    public async Task<bool> CanAddPatientAsync(Guid clinicId)
    {
        var clinic = await _clinicRepository.GetByIdAsync(clinicId);
        if (clinic == null) return false;
        if (!clinic.PatientsLimit.HasValue) return true;
        return clinic.PatientsCount < clinic.PatientsLimit.Value;
    }

    public async Task<bool> CanAddUserAsync(Guid clinicId)
    {
        var clinic = await _clinicRepository.GetByIdAsync(clinicId);
        if (clinic == null) return false;
        if (!clinic.UsersLimit.HasValue) return true;
        return clinic.UsersCount < clinic.UsersLimit.Value;
    }

    public async Task<bool> CanUploadStorageAsync(Guid clinicId, long bytes)
    {
        var clinic = await _clinicRepository.GetByIdAsync(clinicId);
        if (clinic == null) return false;
        return (clinic.StorageUsedBytes + bytes) <= clinic.StorageLimitBytes;
    }

    public void ValidateTenantAccess(Guid requestedClinicId)
    {
        var currentTenantId = _tenantContext.TenantId;
        if (currentTenantId.HasValue && currentTenantId.Value != requestedClinicId)
        {
            _logger.LogWarning("Tenant {CurrentTenantId} attempted to access data for {RequestedClinicId}", currentTenantId, requestedClinicId);
            throw new UnauthorizedAccessException("Cross-tenant access denied.");
        }
    }

    // Fetches clinic owners in a single batch query and merges them into the response list.
    // Using record `with` expressions since ClinicResponse is immutable.
    private async Task<List<ClinicResponse>> EnrichWithOwnersAsync(List<ClinicResponse> responses, List<Guid> clinicIds)
    {
        if (clinicIds.Count == 0) return responses;

        var owners = await _userManager.Users
            .Where(u => u.RoleType == RoleType.ClinicOwner && u.ClinicId.HasValue && clinicIds.Contains(u.ClinicId!.Value))
            .Select(u => new { u.ClinicId, u.FirstName, u.LastName, u.Email })
            .ToListAsync();

        var ownerMap = owners.ToDictionary(o => o.ClinicId!.Value);

        return responses
            .Select(r => ownerMap.TryGetValue(r.Id, out var owner)
                ? r with { OwnerFirstName = owner.FirstName, OwnerLastName = owner.LastName, OwnerEmail = owner.Email }
                : r)
            .ToList();
    }
}
