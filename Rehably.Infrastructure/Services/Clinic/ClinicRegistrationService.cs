using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinic;
using Rehably.Application.Services.Clinic;
using Rehably.Application.Repositories;
using Rehably.Application.Services;
using Rehably.Application.Services.Auth;
using Rehably.Application.Services.Storage;
using Rehably.Application.Services.Clinic;
using Rehably.Domain.Entities.Identity;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Infrastructure.Services.Clinic;

public class ClinicRegistrationService : IClinicRegistrationService
{
    private readonly IClinicRepository _clinicRepository;
    private readonly IPackageRepository _packageRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IAuthService _authService;
    private readonly IAuthPasswordService _authPasswordService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IDocumentService _documentService;
    private readonly IUsageTrackingService _usageTrackingService;
    private readonly ILogger<ClinicRegistrationService> _logger;

    public ClinicRegistrationService(
        IClinicRepository clinicRepository,
        IPackageRepository packageRepository,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IAuthService authService,
        IAuthPasswordService authPasswordService,
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IDocumentService documentService,
        IUsageTrackingService usageTrackingService,
        ILogger<ClinicRegistrationService> logger)
    {
        _clinicRepository = clinicRepository;
        _packageRepository = packageRepository;
        _userManager = userManager;
        _roleManager = roleManager;
        _authService = authService;
        _authPasswordService = authPasswordService;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _documentService = documentService;
        _usageTrackingService = usageTrackingService;
        _logger = logger;
    }

    public async Task<Result<RegisterClinicResponse>> RegisterClinicAsync(RegisterClinicRequest request)
    {
        try
        {
            var plan = await GetPlanOrDefaultAsync(request.SubscriptionPlanId);
            if (plan is null)
            {
                var allPlans = (await _packageRepository.GetAllAsync())
                    .Select(p => new { p.Id, p.Name, p.TrialDays, p.Status })
                    .ToList();
                _logger.LogWarning("Package not found. RequestedPlanId: {PlanId}. Available packages: {Packages}",
                    request.SubscriptionPlanId ?? Guid.Empty, System.Text.Json.JsonSerializer.Serialize(allPlans));
                return Result<RegisterClinicResponse>.Failure($"Package not found. Requested ID: {request.SubscriptionPlanId ?? Guid.Empty}. Please contact support.");
            }

            var slug = GenerateSlug(request.ClinicName);
            if (!await _clinicRepository.IsSubdomainAvailableAsync(slug))
            {
                slug = $"{slug}-{Guid.NewGuid().ToString("N")[..8]}";
            }

            var clinic = new Domain.Entities.Tenant.Clinic
            {
                Name = request.ClinicName,
                NameArabic = request.ClinicNameArabic,
                Slug = slug,
                Phone = request.Phone,
                Email = request.Email,
                Address = request.Address,
                City = request.City,
                Country = request.Country,
                StorageLimitBytes = 10737418240,
                PatientsLimit = 1000,
                UsersLimit = 10
            };

            await _clinicRepository.AddAsync(clinic);
            await _unitOfWork.SaveChangesAsync();

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                TenantId = clinic.Id,
                ClinicId = clinic.Id,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                RoleType = RoleType.ClinicOwner,
                EmailVerified = false,
                MustChangePassword = false,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return Result<RegisterClinicResponse>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            var ownerRole = new ApplicationRole
            {
                Name = $"ClinicOwner_{clinic.Id}",
                TenantId = clinic.Id,
                IsCustom = true,
                Description = "Clinic owner with full permissions"
            };
            await _roleManager.CreateAsync(ownerRole);

            await AssignPlanPermissionsToRoleAsync(ownerRole, plan);

            await _userManager.AddToRoleAsync(user, ownerRole.Name);

            var verifyToken = await _authService.GenerateEmailVerificationTokenAsync(user.Id, user.Email!);
            await _authService.SendVerificationEmailAsync(user.Email!, verifyToken);

            var roles = new List<string> { ownerRole.Name };
            var token = _tokenService.GenerateAccessToken(user.Id, clinic.Id, clinic.Id, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();
            await _tokenService.SaveRefreshTokenAsync(user.Id, refreshToken);

            await _usageTrackingService.RecordUserCountAsync(clinic.Id, 1);

            return Result<RegisterClinicResponse>.Success(new RegisterClinicResponse
            {
                Clinic = MapToResponse(clinic),
                Token = token
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create clinic for {Email}. Error: {Error}", request.Email, ex.Message);
            return Result<RegisterClinicResponse>.Failure($"Failed to create clinic: {ex.Message}");
        }
    }

    public async Task<Result<ClinicResponse>> CreateClinicAsync(CreateClinicRequest request)
    {
        // Clean up any orphaned user from a previous failed attempt with the same email
        var existingUser = await _userManager.FindByEmailAsync(request.OwnerEmail);
        if (existingUser != null)
        {
            var existingClinic = await _clinicRepository.GetByIdAsync(existingUser.ClinicId ?? Guid.Empty);
            if (existingClinic != null)
                return Result<ClinicResponse>.Failure($"Email '{request.OwnerEmail}' is already assigned to clinic '{existingClinic.Name}'");

            _logger.LogWarning("Cleaning up orphaned user {Email} from previous failed creation", request.OwnerEmail);
            await _userManager.DeleteAsync(existingUser);
        }

        await _unitOfWork.BeginTransactionAsync();
        ApplicationUser? createdUser = null;

        try
        {
            var slug = !string.IsNullOrWhiteSpace(request.Slug)
                ? request.Slug.ToLower().Trim()
                : GenerateSlug(request.ClinicName);

            if (!await _clinicRepository.IsSubdomainAvailableAsync(slug))
            {
                slug = $"{slug}-{Guid.NewGuid().ToString("N")[..8]}";
            }

            var city = !string.IsNullOrWhiteSpace(request.Governorate)
                ? $"{request.Governorate}, {request.City}".Trim(',', ' ')
                : request.City;

            var clinic = new Domain.Entities.Tenant.Clinic
            {
                Name = request.ClinicName,
                NameArabic = request.ClinicNameArabic,
                Slug = slug,
                Phone = request.Phone,
                Email = request.Email,
                Address = request.Address,
                City = city,
                Country = request.Country,
                LogoUrl = request.LogoUrl
            };

            await _clinicRepository.AddAsync(clinic);
            await _unitOfWork.SaveChangesAsync();

            var tempPassword = GenerateTempPassword();
            createdUser = new ApplicationUser
            {
                UserName = request.OwnerEmail,
                Email = request.OwnerEmail,
                TenantId = clinic.Id,
                ClinicId = clinic.Id,
                FirstName = request.OwnerFirstName,
                LastName = request.OwnerLastName,
                RoleType = RoleType.ClinicOwner,
                EmailVerified = true,
                MustChangePassword = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(createdUser, tempPassword);
            if (!result.Succeeded)
            {
                await _unitOfWork.RollbackTransactionAsync();
                createdUser = null;
                return Result<ClinicResponse>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            var ownerRole = new ApplicationRole
            {
                Name = $"ClinicOwner_{clinic.Id}",
                TenantId = clinic.Id,
                IsCustom = true,
                Description = "Clinic owner with full permissions"
            };
            await _roleManager.CreateAsync(ownerRole);

            await _userManager.AddToRoleAsync(createdUser, ownerRole.Name);

            var fullName = $"{request.OwnerFirstName} {request.OwnerLastName}".Trim();
            try
            {
                var token = await _authPasswordService.GeneratePasswordResetTokenAsync(createdUser.Email!);
                await _authService.SendWelcomeEmailAsync(createdUser.Email!, token, clinic.Name, fullName);
            }
            catch (Exception emailEx)
            {
                _logger.LogWarning(emailEx, "Welcome email failed for clinic {ClinicId} — continuing", clinic.Id);
            }

            await ProcessFormDocumentsAsync(clinic.Id, request.OwnerIdDocument, request.MedicalLicenseDocument);

            await _unitOfWork.CommitTransactionAsync();

            return Result<ClinicResponse>.Success(MapToResponse(clinic));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();

            // Identity commits outside EF transaction — clean up orphaned user
            if (createdUser != null)
            {
                try
                {
                    await _userManager.DeleteAsync(createdUser);
                    _logger.LogWarning("Cleaned up orphaned user {Email} after failed clinic creation", request.OwnerEmail);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, "Failed to clean up orphaned user {Email}", request.OwnerEmail);
                }
            }

            _logger.LogError(ex, "Failed to create clinic for {Email}", request.OwnerEmail);
            return Result<ClinicResponse>.Failure("Failed to create clinic. Please try again.");
        }
    }

    private async Task ProcessFormDocumentsAsync(Guid clinicId, IFormFile? ownerIdDoc, IFormFile? medicalLicenseDoc)
    {
        var files = new (IFormFile? File, DocumentType Type)[]
        {
            (ownerIdDoc, DocumentType.OwnerId),
            (medicalLicenseDoc, DocumentType.MedicalLicense)
        };

        foreach (var (file, docType) in files)
        {
            if (file == null || file.Length == 0) continue;

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _documentService.UploadDocumentAsync(
                    clinicId, docType, file.FileName, stream);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to upload document {FileName} for clinic {ClinicId}: {Error}",
                        file.FileName, clinicId, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process document {FileName} for clinic {ClinicId}",
                    file.FileName, clinicId);
            }
        }
    }

    private async Task<Package?> GetPlanOrDefaultAsync(Guid? planId)
    {
        if (planId.HasValue)
        {
            return await _packageRepository.GetByIdAsync(planId.Value);
        }

        var packages = await _packageRepository.GetActivePackagesAsync();
        return packages.FirstOrDefault(p => p.TrialDays > 0);
    }

    private async Task AssignPlanPermissionsToRoleAsync(ApplicationRole role, Package package)
    {
        try
        {
            var permissions = new List<string>
            {
                "clinics.view",
                "clinics.update",
                "patients.view",
                "patients.create",
                "patients.update",
                "patients.delete",
                "appointments.view",
                "appointments.create",
                "appointments.update",
                "appointments.delete"
            };

            foreach (var permission in permissions)
            {
                await _roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("Permission", permission));
                _logger.LogDebug("Assigned permission {Permission} to role {RoleName}", permission, role.Name);
            }

            _logger.LogInformation("Successfully assigned {Count} permissions to role {RoleName}", permissions.Count, role.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign permissions to role {RoleName}", role.Name);
            throw;
        }
    }

    private string GenerateSlug(string name)
    {
        var slug = name.ToLower()
            .Replace(" ", "-")
            .Replace("&", "and")
            .Replace("/", "-")
            .Replace("\\", "-");

        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');

        return slug.Length > 50 ? slug[..50] : slug;
    }

    private string GenerateTempPassword()
    {
        const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lowercase = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%^&*";

        var password = new List<char>
        {
            uppercase[System.Security.Cryptography.RandomNumberGenerator.GetInt32(uppercase.Length)],
            lowercase[System.Security.Cryptography.RandomNumberGenerator.GetInt32(lowercase.Length)],
            digits[System.Security.Cryptography.RandomNumberGenerator.GetInt32(digits.Length)],
            special[System.Security.Cryptography.RandomNumberGenerator.GetInt32(special.Length)]
        };

        const string allChars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$%^&*";
        for (int i = password.Count; i < 12; i++)
        {
            password.Add(allChars[System.Security.Cryptography.RandomNumberGenerator.GetInt32(allChars.Length)]);
        }

        return new string(password.OrderBy(x => System.Security.Cryptography.RandomNumberGenerator.GetInt32(100)).ToArray());
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
                    Limit = pf.Limit
                }).ToList() ?? [],
            CreatedAt = clinic.CreatedAt,
            UpdatedAt = clinic.UpdatedAt
        };
    }
}
