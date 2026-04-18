using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Communication;
using Rehably.Application.DTOs.Package;
using Rehably.Application.Interfaces;
using Rehably.Application.DTOs.Payment;
using Rehably.Application.DTOs.Clinic;
using Rehably.Application.DTOs.Registration;
using Rehably.Application.Services.Clinic;
using Rehably.Application.Repositories;
using Rehably.Application.Services;
using Rehably.Application.Services.Auth;
using Rehably.Application.Services.Communication;
using Rehably.Application.Services.Payment;
using Rehably.Application.Services.Platform;
using Rehably.Application.Services.Storage;
using Rehably.Application.Services.Clinic;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Services.Communication.Templates;
using System.Security.Cryptography;
using System.Text.Json;

namespace Rehably.Infrastructure.Services.Clinic;

public class ClinicOnboardingService : IClinicOnboardingService
{
    private readonly IClinicOnboardingRepository _onboardingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClinicService _clinicService;
    private readonly IOtpService _otpService;
    private readonly IDocumentService _documentService;
    private readonly IPaymentService _paymentService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ClinicOnboardingService> _logger;
    private readonly IClinicRepository _clinicRepository;
    private readonly IPackageRepository _packageRepository;
    private readonly IRepository<Subscription> _subscriptionRepository;
    private readonly IInvoiceService _invoiceService;
    private readonly IAuthPasswordService _authPasswordService;
    private readonly IClock _clock;

    public ClinicOnboardingService(
        IClinicOnboardingRepository onboardingRepository,
        IUnitOfWork unitOfWork,
        IClinicService clinicService,
        IOtpService otpService,
        IDocumentService documentService,
        IPaymentService paymentService,
        IEmailService emailService,
        ILogger<ClinicOnboardingService> logger,
        IClinicRepository clinicRepository,
        IPackageRepository packageRepository,
        IRepository<Subscription> subscriptionRepository,
        IInvoiceService invoiceService,
        IAuthPasswordService authPasswordService,
        IClock clock)
    {
        _onboardingRepository = onboardingRepository;
        _unitOfWork = unitOfWork;
        _clinicService = clinicService;
        _otpService = otpService;
        _documentService = documentService;
        _paymentService = paymentService;
        _emailService = emailService;
        _logger = logger;
        _clinicRepository = clinicRepository;
        _packageRepository = packageRepository;
        _subscriptionRepository = subscriptionRepository;
        _invoiceService = invoiceService;
        _authPasswordService = authPasswordService;
        _clock = clock;
    }

    public async Task<Result<ClinicOnboarding>> StartOnboardingAsync(
        string name,
        string email,
        string phone,
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result<ClinicOnboarding>.Failure("Clinic name is required");
            }

            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                return Result<ClinicOnboarding>.Failure("Valid email is required");
            }

            var tempPassword = GenerateTempPassword();

            var request = new RegisterClinicRequest
            {
                ClinicName = name,
                Email = email,
                Phone = phone,
                FirstName = ownerId,
                Password = tempPassword,
                LastName = "Owner"
            };

            var result = await _clinicService.RegisterClinicAsync(request);

            if (result.IsFailure)
            {
                return Result<ClinicOnboarding>.Failure(result.Error);
            }

            var clinicResponse = result.Data;
            var onboarding = new ClinicOnboarding
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicResponse.Clinic.Id,
                CurrentStep = OnboardingStep.PendingEmailVerification,
                CreatedAt = _clock.UtcNow
            };

            await _onboardingRepository.AddAsync(onboarding);
            await _unitOfWork.SaveChangesAsync();

            var otpResult = await _otpService.GenerateOtpAsync(email, OtpPurpose.EmailVerification);
            if (!otpResult.IsSuccess)
            {
                return Result<ClinicOnboarding>.Failure("Failed to generate verification code");
            }

            await _emailService.SendWithDefaultProviderAsync(new EmailMessage
            {
                To = email,
                Subject = "Verify Your Email - Rehably Clinic Registration",
                Body = AuthEmailTemplates.EmailVerificationOtp(otpResult.Value, 10),
                IsHtml = true
            });

            _logger.LogInformation("Started onboarding for clinic {ClinicId}, onboarding {OnboardingId}",
                clinicResponse.Clinic.Id, onboarding.Id);

            return Result<ClinicOnboarding>.Success(onboarding);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting onboarding");
            return Result<ClinicOnboarding>.Failure("An error occurred while starting registration. Please try again.");
        }
    }

    public async Task<Result> VerifyEmailAsync(
        Guid onboardingId,
        string otp,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var onboarding = await _onboardingRepository.GetWithClinicAsync(onboardingId, cancellationToken);

            if (onboarding == null)
            {
                return Result.Failure("Onboarding not found");
            }

            if (onboarding.CurrentStep != OnboardingStep.PendingEmailVerification)
            {
                return Result.Failure("Email already verified or invalid step");
            }

            var otpResult = await _otpService.VerifyOtpAsync(onboarding.Clinic!.Email, otp, OtpPurpose.EmailVerification);

            if (!otpResult.IsSuccess || !otpResult.Value!.IsValid)
            {
                var errorMessage = otpResult.Value?.AttemptsRemaining > 0
                    ? $"Invalid OTP. {otpResult.Value.AttemptsRemaining} attempts remaining"
                    : "Invalid or expired OTP";
                return Result.Failure(errorMessage);
            }

            onboarding.CurrentStep = OnboardingStep.PendingDocumentsAndPackage;
            onboarding.EmailVerifiedAt = _clock.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Email verified for onboarding {OnboardingId}", onboardingId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying email for onboarding {OnboardingId}", onboardingId);
            return Result.Failure("An error occurred. Please try again.");
        }
    }

    public async Task<Result<ClinicDocument>> UploadDocumentAsync(
        Guid onboardingId,
        DocumentType documentType,
        string fileName,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
            if (onboarding == null)
            {
                return Result<ClinicDocument>.Failure("Onboarding not found");
            }

            if (onboarding.CurrentStep != OnboardingStep.PendingDocumentsAndPackage)
            {
                return Result<ClinicDocument>.Failure("Documents not pending or invalid step");
            }

            var documentResult = await _documentService.UploadDocumentAsync(
                onboarding.ClinicId,
                documentType,
                fileName,
                stream,
                cancellationToken);

            if (!documentResult.IsSuccess)
            {
                return Result<ClinicDocument>.Failure(documentResult.Error ?? "Failed to upload document");
            }

            var hasDocuments = await _onboardingRepository.HasDocumentsAsync(onboarding.ClinicId, cancellationToken);

            if (hasDocuments)
            {
                onboarding.CurrentStep = OnboardingStep.PendingApproval;
                onboarding.DocumentsUploadedAt = _clock.UtcNow;
                await _unitOfWork.SaveChangesAsync();
            }

            _logger.LogInformation("Document uploaded for onboarding {OnboardingId}, type {DocumentType}",
                onboardingId, documentType);

            return Result<ClinicDocument>.Success(documentResult.Value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document for onboarding {OnboardingId}", onboardingId);
            return Result<ClinicDocument>.Failure("An error occurred while uploading the document. Please try again.");
        }
    }

    public async Task<Result<ClinicOnboarding>> SubmitForApprovalAsync(
        Guid onboardingId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
            if (onboarding == null)
            {
                return Result<ClinicOnboarding>.Failure("Onboarding not found");
            }

            if (onboarding.CurrentStep != OnboardingStep.PendingDocumentsAndPackage &&
                onboarding.CurrentStep != OnboardingStep.PendingApproval)
            {
                return Result<ClinicOnboarding>.Failure("Cannot submit for approval in current state");
            }

            var hasDocuments = await _onboardingRepository.HasDocumentsAsync(onboarding.ClinicId, cancellationToken);

            if (!hasDocuments)
            {
                return Result<ClinicOnboarding>.Failure("No documents uploaded");
            }

            onboarding.CurrentStep = OnboardingStep.PendingApproval;
            onboarding.DocumentsUploadedAt = _clock.UtcNow;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Onboarding {OnboardingId} submitted for approval", onboardingId);
            return Result<ClinicOnboarding>.Success(onboarding);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting onboarding {OnboardingId} for approval", onboardingId);
            return Result<ClinicOnboarding>.Failure("An error occurred. Please try again.");
        }
    }

    public async Task<Result<PaymentResponse>> InitiatePaymentAsync(
        Guid onboardingId,
        Guid subscriptionPlanId,
        string returnUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
            if (onboarding == null)
            {
                return Result<PaymentResponse>.Failure("Onboarding not found");
            }

            if (onboarding.CurrentStep != OnboardingStep.PendingPayment)
            {
                return Result<PaymentResponse>.Failure("Payment not pending or invalid step");
            }

            var result = await _paymentService.CreateSubscriptionPaymentAsync(
                onboarding.ClinicId,
                subscriptionPlanId,
                returnUrl,
                cancelUrl);

            if (result.IsFailure)
            {
                return Result<PaymentResponse>.Failure(result.Error);
            }

            _logger.LogInformation("Payment initiated for onboarding {OnboardingId}, transaction {TransactionId}",
                onboardingId, result.Value.TransactionId);

            return Result<PaymentResponse>.Success(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating payment for onboarding {OnboardingId}", onboardingId);
            return Result<PaymentResponse>.Failure("An error occurred while initiating payment. Please try again.");
        }
    }

    public async Task<Result<ClinicOnboarding>> CompleteOnboardingAsync(
        Guid onboardingId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var onboarding = await _onboardingRepository.GetWithClinicAsync(onboardingId, cancellationToken);

            if (onboarding == null)
            {
                return Result<ClinicOnboarding>.Failure("Onboarding not found");
            }

            if (onboarding.CurrentStep != OnboardingStep.PendingPayment)
            {
                return Result<ClinicOnboarding>.Failure("Cannot complete in current state");
            }

            onboarding.CurrentStep = OnboardingStep.Completed;
            onboarding.PaymentCompletedAt = _clock.UtcNow;

            onboarding.Clinic!.Status = ClinicStatus.Active;
            onboarding.Clinic.ActivatedAt = _clock.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Onboarding {OnboardingId} completed", onboardingId);
            return Result<ClinicOnboarding>.Success(onboarding);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing onboarding {OnboardingId}", onboardingId);
            return Result<ClinicOnboarding>.Failure("An error occurred. Please try again.");
        }
    }

    public async Task<Result<ClinicOnboarding>> GetOnboardingAsync(
        Guid onboardingId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var onboarding = await _onboardingRepository.GetWithClinicAsync(onboardingId, cancellationToken);

            if (onboarding == null)
            {
                return Result<ClinicOnboarding>.Failure("Onboarding not found");
            }

            return Result<ClinicOnboarding>.Success(onboarding);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting onboarding {OnboardingId}", onboardingId);
            return Result<ClinicOnboarding>.Failure("An error occurred. Please try again.");
        }
    }

    public async Task<Result<ClinicOnboarding>> GetOnboardingByClinicIdAsync(
        Guid clinicId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var onboarding = await _onboardingRepository.GetByClinicIdAsync(clinicId, cancellationToken);

            if (onboarding == null)
            {
                return Result<ClinicOnboarding>.Failure("Onboarding not found for this clinic");
            }

            return Result<ClinicOnboarding>.Success(onboarding);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting onboarding for clinic {ClinicId}", clinicId);
            return Result<ClinicOnboarding>.Failure("An error occurred. Please try again.");
        }
    }

    public async Task<Result<ClinicCreatedDto>> AdminCreateClinicAsync(
        AdminCreateClinicRequestDto request,
        string adminUserId)
    {
        try
        {
            var isSlugAvailable = await _clinicRepository.IsSubdomainAvailableAsync(request.Slug);
            if (!isSlugAvailable)
                return Result<ClinicCreatedDto>.Failure("Slug already exists. Please choose a different slug.");

            var tempPassword = GenerateTempPassword();
            var registerRequest = new RegisterClinicRequest
            {
                ClinicName = request.ClinicName,
                Email = request.OwnerEmail,
                Phone = request.Phone,
                FirstName = request.OwnerFirstName,
                LastName = request.OwnerLastName,
                PhoneNumber = request.OwnerPhone,
                Address = request.Address,
                Password = tempPassword
            };

            var registerResult = await _clinicService.RegisterClinicAsync(registerRequest);
            if (registerResult.IsFailure)
                return Result<ClinicCreatedDto>.Failure(registerResult.Error);

            var clinicId = registerResult.Value!.Clinic.Id;

            var clinic = await _clinicRepository.GetByIdAsync(clinicId);
            if (clinic == null)
                return Result<ClinicCreatedDto>.Failure("Failed to retrieve created clinic");

            clinic.Slug = request.Slug;

            Package package;
            if (request.PackageId.HasValue)
            {
                var existingPackage = await _packageRepository.GetWithFeaturesAsync(request.PackageId.Value);
                if (existingPackage == null)
                    return Result<ClinicCreatedDto>.Failure("Package not found");
                package = existingPackage;
            }
            else
            {
                var customPackage = new Package
                {
                    Id = Guid.NewGuid(),
                    Name = $"Custom - {request.ClinicName}",
                    Code = $"custom-{request.Slug}",
                    IsCustom = true,
                    ForClinicId = clinicId,
                    MonthlyPrice = request.CustomMonthlyPrice!.Value,
                    YearlyPrice = request.CustomYearlyPrice!.Value,
                    IsPublic = false,
                    Status = PackageStatus.Active,
                    Features = request.CustomFeatures!.Select(f => new PackageFeature
                    {
                        Id = Guid.NewGuid(),
                        FeatureId = f.FeatureId,
                        Limit = f.Limit
                    }).ToList()
                };
                await _packageRepository.AddAsync(customPackage);
                package = customPackage;
            }

            var isTrialSubscription = request.TrialDays > 0;
            var endDate = request.BillingCycle == BillingCycle.Yearly
                ? request.StartDate.AddYears(1)
                : request.StartDate.AddMonths(1);

            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                PackageId = package.Id,
                Status = isTrialSubscription ? SubscriptionStatus.Trial : SubscriptionStatus.Active,
                BillingCycle = request.BillingCycle,
                StartDate = request.StartDate,
                EndDate = endDate,
                TrialEndsAt = isTrialSubscription ? request.StartDate.AddDays(request.TrialDays) : null,
                AutoRenew = request.AutoRenew,
                PaymentType = request.PaymentType,
                PriceSnapshot = System.Text.Json.JsonSerializer.Serialize(new PackageSnapshotDto
                {
                    PackageId = package.Id,
                    PackageName = package.Name,
                    PackageCode = package.Code ?? package.Slug ?? string.Empty,
                    MonthlyPrice = package.MonthlyPrice,
                    YearlyPrice = package.YearlyPrice,
                    Features = package.Features?.Select(f => new PackageFeatureSnapshotDto
                    {
                        FeatureId = f.FeatureId,
                        FeatureName = f.Feature?.Name ?? string.Empty,
                        FeatureCode = f.Feature?.Code ?? string.Empty,
                        Limit = f.Limit
                    }).ToList() ?? new List<PackageFeatureSnapshotDto>()
                })
            };

            await _subscriptionRepository.AddAsync(subscription);

            clinic.CurrentSubscriptionId = subscription.Id;
            clinic.Status = ClinicStatus.Active;
            clinic.ActivatedAt = _clock.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            var invoiceResult = await _invoiceService.GenerateSubscriptionInvoiceAsync(
                subscription, package, request.PaymentType);
            Guid? invoiceId = invoiceResult.IsSuccess ? invoiceResult.Value?.Id : null;

            var passwordSent = false;
            try
            {
                await _authPasswordService.GeneratePasswordResetTokenAsync(request.OwnerEmail);
                passwordSent = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate temp password for {Email}", request.OwnerEmail);
            }

            _logger.LogInformation(
                "Admin {AdminUserId} created clinic {ClinicId} with subscription {SubscriptionId}",
                adminUserId, clinicId, subscription.Id);

            return Result<ClinicCreatedDto>.Success(new ClinicCreatedDto
            {
                Id = clinicId,
                Name = request.ClinicName,
                Slug = request.Slug,
                Email = request.OwnerEmail,
                Phone = clinic.Phone,
                Status = clinic.Status,
                CreatedAt = clinic.CreatedAt,
                SubscriptionId = subscription.Id,
                PackageName = package.Name,
                SubscriptionStatus = subscription.Status,
                SubscriptionStartDate = subscription.StartDate,
                SubscriptionEndDate = subscription.EndDate,
                PaymentType = request.PaymentType.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during admin clinic creation for slug {Slug}", request.Slug);
            return Result<ClinicCreatedDto>.Failure("An error occurred while creating the clinic. Please try again.");
        }
    }

    // ── Self-registration flow ────────────────────────────────────────────────

    public async Task<Result<RegistrationStartedDto>> StartRegistrationAsync(StartRegistrationRequestDto request)
    {
        try
        {
            var emailInUse = await _clinicRepository.AnyAsync(c => c.Email == request.Email && !c.IsDeleted);
            if (emailInUse)
                return Result<RegistrationStartedDto>.Failure("Email already registered");

            var tempPassword = GenerateTempPassword();
            var slug = request.PreferredSlug ?? GenerateSlug(request.ClinicName);

            var registerRequest = new RegisterClinicRequest
            {
                ClinicName = request.ClinicName,
                Email = request.Email,
                Phone = request.Phone,
                FirstName = request.OwnerFirstName,
                LastName = request.OwnerLastName,
                Password = tempPassword
            };

            var registerResult = await _clinicService.RegisterClinicAsync(registerRequest);
            if (registerResult.IsFailure)
                return Result<RegistrationStartedDto>.Failure(registerResult.Error);

            var clinicId = registerResult.Value!.Clinic.Id;

            var clinic = await _clinicRepository.GetByIdAsync(clinicId);
            if (clinic != null && !string.IsNullOrWhiteSpace(request.PreferredSlug))
                clinic.Slug = request.PreferredSlug;

            var onboarding = new ClinicOnboarding
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                CurrentStep = OnboardingStep.PendingEmailVerification,
                PreferredSlug = request.PreferredSlug,
                CreatedAt = _clock.UtcNow
            };

            await _onboardingRepository.AddAsync(onboarding);
            await _unitOfWork.SaveChangesAsync();

            var otpResult = await _otpService.GenerateOtpAsync(request.Email, OtpPurpose.EmailVerification);
            if (!otpResult.IsSuccess)
                return Result<RegistrationStartedDto>.Failure("Failed to generate verification code");

            await _emailService.SendWithDefaultProviderAsync(new EmailMessage
            {
                To = request.Email,
                Subject = "Verify Your Email - Rehably Clinic Registration",
                Body = AuthEmailTemplates.EmailVerificationOtp(otpResult.Value, 10),
                IsHtml = true
            });

            _logger.LogInformation("Self-registration started for clinic {ClinicId}, onboarding {OnboardingId}",
                clinicId, onboarding.Id);

            return Result<RegistrationStartedDto>.Success(new RegistrationStartedDto(
                onboarding.Id,
                clinicId,
                request.Email,
                600));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting self-registration for email {Email}", request.Email);
            return Result<RegistrationStartedDto>.Failure("An error occurred while starting registration. Please try again.");
        }
    }

    public async Task<Result> VerifyRegistrationEmailAsync(Guid onboardingId, string otp)
    {
        try
        {
            var onboarding = await _onboardingRepository.GetWithClinicAsync(onboardingId);
            if (onboarding == null)
                return Result.Failure("Onboarding not found");

            if (onboarding.CurrentStep != OnboardingStep.PendingEmailVerification)
                return Result.Failure("Email already verified or invalid step");

            var email = onboarding.Clinic?.Email;
            if (string.IsNullOrEmpty(email))
                return Result.Failure("Clinic email not found");

            var otpResult = await _otpService.VerifyOtpAsync(email, otp, OtpPurpose.EmailVerification);
            if (!otpResult.IsSuccess || !otpResult.Value!.IsValid)
            {
                var errorMessage = otpResult.Value?.AttemptsRemaining > 0
                    ? $"Invalid OTP. {otpResult.Value.AttemptsRemaining} attempts remaining"
                    : "Invalid or expired OTP";
                return Result.Failure(errorMessage);
            }

            onboarding.CurrentStep = OnboardingStep.PendingDocumentsAndPackage;
            onboarding.EmailVerifiedAt = _clock.UtcNow;

            if (onboarding.Clinic != null)
                onboarding.Clinic.Status = ClinicStatus.PendingDocumentsAndPackage;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Email verified for registration onboarding {OnboardingId}", onboardingId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying registration email for onboarding {OnboardingId}", onboardingId);
            return Result.Failure("An error occurred. Please try again.");
        }
    }

    public async Task<Result> SubmitGlobalPackageAsync(Guid onboardingId, SubmitGlobalPackageRequestDto request)
    {
        try
        {
            var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
            if (onboarding == null)
                return Result.Failure("Onboarding not found");

            if (onboarding.CurrentStep != OnboardingStep.PendingDocumentsAndPackage)
                return Result.Failure("Package selection not available in current step");

            var package = await _packageRepository.GetByIdAsync(request.PackageId);
            if (package == null)
                return Result.Failure("Package not found");

            onboarding.SelectedPackageId = request.PackageId;
            onboarding.SelectedBillingCycle = request.BillingCycle;
            onboarding.OnboardingType = OnboardingType.GlobalPackage;
            onboarding.PackageSelectedAt = _clock.UtcNow;
            onboarding.CurrentStep = OnboardingStep.PendingApproval;

            var clinic = await _clinicRepository.GetByIdAsync(onboarding.ClinicId);
            if (clinic != null)
                clinic.Status = ClinicStatus.PendingApproval;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Global package {PackageId} selected for onboarding {OnboardingId}",
                request.PackageId, onboardingId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting global package for onboarding {OnboardingId}", onboardingId);
            return Result.Failure("An error occurred. Please try again.");
        }
    }

    public async Task<Result> SubmitCustomRequestAsync(Guid onboardingId, SubmitCustomRequestDto request)
    {
        try
        {
            if (request.FeatureIds == null || request.FeatureIds.Count == 0)
                return Result.Failure("At least one feature is required");

            var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
            if (onboarding == null)
                return Result.Failure("Onboarding not found");

            if (onboarding.CurrentStep != OnboardingStep.PendingDocumentsAndPackage)
                return Result.Failure("Custom request not available in current step");

            onboarding.OnboardingType = OnboardingType.CustomRequest;
            onboarding.SelectedFeatures = JsonSerializer.Serialize(request.FeatureIds);
            onboarding.CurrentStep = OnboardingStep.PendingCustomPackageReview;

            var clinic = await _clinicRepository.GetByIdAsync(onboarding.ClinicId);
            if (clinic != null)
                clinic.Status = ClinicStatus.PendingCustomPackageReview;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Custom request submitted for onboarding {OnboardingId} with {Count} features",
                onboardingId, request.FeatureIds.Count);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting custom request for onboarding {OnboardingId}", onboardingId);
            return Result.Failure("An error occurred. Please try again.");
        }
    }

    public async Task<Result<RegistrationStatusDto>> GetRegistrationStatusAsync(Guid onboardingId)
    {
        try
        {
            var onboarding = await _onboardingRepository.GetWithClinicAsync(onboardingId);
            if (onboarding == null)
                return Result<RegistrationStatusDto>.Failure("Onboarding not found");

            var dto = new RegistrationStatusDto(
                onboarding.Id,
                onboarding.CurrentStep,
                onboarding.Clinic?.Status ?? ClinicStatus.PendingEmailVerification,
                onboarding.OnboardingType);

            return Result<RegistrationStatusDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting registration status for onboarding {OnboardingId}", onboardingId);
            return Result<RegistrationStatusDto>.Failure("An error occurred. Please try again.");
        }
    }

    public async Task<Result> ApproveRegistrationAsync(Guid clinicId, string? slugOverride, string adminUserId)
    {
        try
        {
            var onboarding = await _onboardingRepository.GetByClinicIdAsync(clinicId);
            if (onboarding == null)
                return Result.Failure("Onboarding not found");

            if (onboarding.CurrentStep != OnboardingStep.PendingApproval)
                return Result.Failure("Clinic is not in PendingApproval state");

            var clinic = await _clinicRepository.GetByIdAsync(clinicId);
            if (clinic == null)
                return Result.Failure("Clinic not found");

            if (!string.IsNullOrWhiteSpace(slugOverride))
                clinic.Slug = slugOverride;

            onboarding.CurrentStep = OnboardingStep.PendingPayment;
            onboarding.ApprovedAt = _clock.UtcNow;
            onboarding.ApprovedBy = adminUserId;
            clinic.Status = ClinicStatus.PendingPayment;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Admin {AdminUserId} approved registration for clinic {ClinicId}",
                adminUserId, clinicId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving registration for clinic {ClinicId}", clinicId);
            return Result.Failure("An error occurred. Please try again.");
        }
    }

    public async Task<Result> RejectRegistrationAsync(Guid clinicId, string reason, string adminUserId)
    {
        try
        {
            var onboarding = await _onboardingRepository.GetWithClinicAsync(
                (await _onboardingRepository.GetByClinicIdAsync(clinicId))?.Id ?? Guid.Empty);

            if (onboarding == null)
                return Result.Failure("Onboarding not found");

            onboarding.RejectionReason = reason;
            onboarding.RejectedAt = _clock.UtcNow;
            onboarding.CurrentStep = OnboardingStep.PendingDocumentsAndPackage;

            if (onboarding.Clinic != null)
                onboarding.Clinic.Status = ClinicStatus.PendingDocumentsAndPackage;

            await _unitOfWork.SaveChangesAsync();

            if (onboarding.Clinic?.Email != null)
            {
                await _emailService.SendWithDefaultProviderAsync(new EmailMessage
                {
                    To = onboarding.Clinic.Email,
                    Subject = "Registration Review Update - Rehably",
                    Body = $"<p>Your clinic registration was not approved at this time. Reason: {reason}</p><p>You may re-submit with updated information.</p>",
                    IsHtml = true
                });
            }

            _logger.LogInformation("Admin {AdminUserId} rejected registration for clinic {ClinicId}. Reason: {Reason}",
                adminUserId, clinicId, reason);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting registration for clinic {ClinicId}", clinicId);
            return Result.Failure("An error occurred. Please try again.");
        }
    }

    public async Task<Result> ProcessCustomRequestAsync(Guid clinicId, ProcessCustomRequestDto request, string adminUserId)
    {
        try
        {
            var onboarding = await _onboardingRepository.GetByClinicIdAsync(clinicId);
            if (onboarding == null)
                return Result.Failure("Onboarding not found");

            if (onboarding.CurrentStep != OnboardingStep.PendingCustomPackageReview)
                return Result.Failure("Clinic is not in PendingCustomPackageReview state");

            var clinic = await _clinicRepository.GetByIdAsync(clinicId);
            if (clinic == null)
                return Result.Failure("Clinic not found");

            var customPackage = new Package
            {
                Id = Guid.NewGuid(),
                Name = $"Custom - {clinic.Name}",
                Code = $"custom-{clinic.Slug}",
                IsCustom = true,
                ForClinicId = clinicId,
                MonthlyPrice = request.MonthlyPrice,
                YearlyPrice = request.YearlyPrice,
                IsPublic = false,
                Status = PackageStatus.Active,
                Features = request.Features.Select(f => new PackageFeature
                {
                    Id = Guid.NewGuid(),
                    FeatureId = f.FeatureId,
                    Limit = f.Limit
                }).ToList()
            };
            await _packageRepository.AddAsync(customPackage);

            var endDate = request.BillingCycle == BillingCycle.Yearly
                ? _clock.UtcNow.AddYears(1)
                : _clock.UtcNow.AddMonths(1);

            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                PackageId = customPackage.Id,
                Status = SubscriptionStatus.Active,
                BillingCycle = request.BillingCycle,
                StartDate = _clock.UtcNow,
                EndDate = endDate,
                AutoRenew = true,
                PaymentType = request.PaymentType,
                PriceSnapshot = JsonSerializer.Serialize(new PackageSnapshotDto
                {
                    PackageId = customPackage.Id,
                    PackageName = customPackage.Name,
                    PackageCode = customPackage.Code ?? customPackage.Slug ?? string.Empty,
                    MonthlyPrice = customPackage.MonthlyPrice,
                    YearlyPrice = customPackage.YearlyPrice,
                    Features = customPackage.Features?.Select(f => new PackageFeatureSnapshotDto
                    {
                        FeatureId = f.FeatureId,
                        FeatureName = f.Feature?.Name ?? string.Empty,
                        FeatureCode = f.Feature?.Code ?? string.Empty,
                        Limit = f.Limit
                    }).ToList() ?? new List<PackageFeatureSnapshotDto>()
                })
            };
            await _subscriptionRepository.AddAsync(subscription);

            clinic.CurrentSubscriptionId = subscription.Id;
            clinic.Status = ClinicStatus.Active;
            clinic.ActivatedAt = _clock.UtcNow;

            onboarding.SelectedPackageId = customPackage.Id;
            onboarding.SelectedBillingCycle = request.BillingCycle;
            onboarding.CurrentStep = OnboardingStep.Completed;
            onboarding.PaymentCompletedAt = _clock.UtcNow;
            onboarding.ApprovedBy = adminUserId;
            onboarding.ApprovedAt = _clock.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            await _invoiceService.GenerateSubscriptionInvoiceAsync(subscription, customPackage, request.PaymentType);

            _logger.LogInformation(
                "Admin {AdminUserId} processed custom request for clinic {ClinicId}, package {PackageId}",
                adminUserId, clinicId, customPackage.Id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing custom request for clinic {ClinicId}", clinicId);
            return Result.Failure("An error occurred. Please try again.");
        }
    }

    public async Task<Result<PaymentSessionDto>> InitiatePaymentAsync(
        Guid onboardingId,
        string paymentProvider,
        string returnUrl,
        string? cancelUrl)
    {
        try
        {
            var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
            if (onboarding == null)
                return Result<PaymentSessionDto>.Failure("Onboarding not found");

            if (onboarding.CurrentStep != OnboardingStep.PendingPayment)
                return Result<PaymentSessionDto>.Failure("Payment not pending or invalid step");

            if (onboarding.SelectedPackageId == null)
                return Result<PaymentSessionDto>.Failure("No package selected for payment");

            var effectiveCancelUrl = cancelUrl ?? returnUrl;

            var paymentResult = await _paymentService.CreateSubscriptionPaymentAsync(
                onboarding.ClinicId,
                onboarding.SelectedPackageId.Value,
                returnUrl,
                effectiveCancelUrl,
                paymentProvider);

            if (paymentResult.IsFailure)
                return Result<PaymentSessionDto>.Failure(paymentResult.Error);

            var session = new PaymentSessionDto(
                paymentResult.Value.PaymentUrl ?? string.Empty,
                paymentProvider,
                paymentResult.Value.TransactionId);

            _logger.LogInformation(
                "Payment session initiated for onboarding {OnboardingId} via provider {Provider}",
                onboardingId, paymentProvider);

            return Result<PaymentSessionDto>.Success(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating payment for onboarding {OnboardingId}", onboardingId);
            return Result<PaymentSessionDto>.Failure("An error occurred while initiating payment. Please try again.");
        }
    }

    public async Task<Result<ClinicDocumentsResponseDto>> GetClinicDocumentsAsync(
        Guid clinicId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var clinic = await _clinicRepository.GetWithDocumentsAsync(clinicId, cancellationToken);
            if (clinic == null)
                return Result<ClinicDocumentsResponseDto>.Failure("Clinic not found");

            var onboarding = await _onboardingRepository.GetByClinicIdAsync(clinicId, cancellationToken);

            var documents = clinic.Documents.Select(d => new ClinicDocumentDto
            {
                Id = d.Id,
                Type = d.DocumentType.ToString(),
                FileUrl = d.PublicUrl ?? d.StorageUrl,
                UploadedAt = d.UploadedAt,
                VerificationStatus = d.Status.ToString(),
                RejectionReason = d.RejectionReason
            }).ToList();

            var response = new ClinicDocumentsResponseDto
            {
                ClinicId = clinicId,
                ClinicName = clinic.Name,
                OnboardingStatus = onboarding?.CurrentStep.ToString() ?? "Unknown",
                DocumentsUploadedAt = onboarding?.DocumentsUploadedAt,
                Documents = documents
            };

            return Result<ClinicDocumentsResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving clinic documents for {ClinicId}", clinicId);
            return Result<ClinicDocumentsResponseDto>.Failure("An error occurred while retrieving clinic documents.");
        }
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("\"", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-").Trim('-');
        return slug.Length > 50 ? slug[..50] : slug;
    }

    private static string GenerateTempPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%";
        const string all = upper + lower + digits + special;

        var password = new char[12];

        password[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        password[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        password[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        password[3] = special[RandomNumberGenerator.GetInt32(special.Length)];

        for (int i = 4; i < 12; i++)
        {
            password[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        }

        for (int i = password.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }
}
