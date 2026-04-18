using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinic;
using Rehably.Application.DTOs.Communication;
using Rehably.Application.Services.Clinic;
using Rehably.Application.Repositories;
using Rehably.Application.Services;
using Rehably.Application.Services.Communication;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Infrastructure.Services.Clinic;

public class ClinicOnboardingApprovalService : IClinicOnboardingApprovalService
{
    private readonly IClinicOnboardingRepository _onboardingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<ClinicOnboardingApprovalService> _logger;

    public ClinicOnboardingApprovalService(
        IClinicOnboardingRepository onboardingRepository,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<ClinicOnboardingApprovalService> logger)
    {
        _onboardingRepository = onboardingRepository;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<ClinicOnboarding>> ApproveOnboardingAsync(
        Guid onboardingId,
        ApproveClinicRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var onboarding = await _onboardingRepository.GetWithClinicAsync(onboardingId, cancellationToken);

            if (onboarding == null)
            {
                return Result<ClinicOnboarding>.Failure("Onboarding not found");
            }

            if (onboarding.CurrentStep != OnboardingStep.PendingApproval)
            {
                return Result<ClinicOnboarding>.Failure("Cannot approve in current state");
            }

            onboarding.CurrentStep = OnboardingStep.PendingPayment;
            onboarding.ApprovedAt = DateTime.UtcNow;
            onboarding.Clinic!.Status = ClinicStatus.PendingPayment;

            await _unitOfWork.SaveChangesAsync();

            if (_logger.IsEnabled(LogLevel.Information))
            {
                var hasCustomPricing = request?.CustomMonthlyPrice.HasValue ?? false;
                _logger.LogInformation(
                    "Onboarding {OnboardingId} approved. PackageId={PackageId}, PaymentType={PaymentType}, HasCustomPricing={HasCustom}",
                    onboardingId,
                    request?.SubscriptionPlanId,
                    request?.PaymentType,
                    hasCustomPricing);
            }

            if (!string.IsNullOrEmpty(onboarding.Clinic!.Email))
            {
                await _emailService.SendWithDefaultProviderAsync(new EmailMessage
                {
                    To = onboarding.Clinic.Email,
                    Subject = "Congratulations! Your Clinic is Approved - Rehably",
                    Body = $"<h2>Great News!</h2><p>Your clinic <strong>{onboarding.Clinic.Name}</strong> has been approved.</p><p>Please complete your payment to activate your subscription and start using Rehably.</p>",
                    IsHtml = true
                }, cancellationToken);
            }

            return Result<ClinicOnboarding>.Success(onboarding);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving onboarding {OnboardingId}", onboardingId);
            return Result<ClinicOnboarding>.Failure("An error occurred. Please try again.");
        }
    }

    public async Task<Result> RejectOnboardingAsync(
        Guid onboardingId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return Result.Failure("Rejection reason is required");
            }

            var onboarding = await _onboardingRepository.GetWithClinicAsync(onboardingId, cancellationToken);

            if (onboarding == null)
            {
                return Result.Failure("Onboarding not found");
            }

            onboarding.Clinic!.Status = ClinicStatus.Cancelled;

            await _unitOfWork.SaveChangesAsync();

            if (!string.IsNullOrEmpty(onboarding.Clinic!.Email))
            {
                await _emailService.SendWithDefaultProviderAsync(new EmailMessage
                {
                    To = onboarding.Clinic.Email,
                    Subject = "Clinic Registration Update - Rehably",
                    Body = $"<h2>Registration Update</h2><p>We regret to inform you that your clinic registration was not approved.</p><p><strong>Reason:</strong> {reason}</p><p>If you have questions, please contact our support team.</p>",
                    IsHtml = true
                });
            }

            _logger.LogInformation("Onboarding {OnboardingId} rejected. Reason: {Reason}", onboardingId, reason);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting onboarding {OnboardingId}", onboardingId);
            return Result.Failure("An error occurred. Please try again.");
        }
    }

    public async Task<Result<ClinicDocument>> AcceptDocumentAsync(
        Guid clinicId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var docRepo = _unitOfWork.Repository<ClinicDocument>();
            var doc = await docRepo.GetByIdAsync(documentId);

            if (doc == null || doc.ClinicId != clinicId)
            {
                return Result<ClinicDocument>.Failure("Document not found");
            }

            doc.Status = DocumentStatus.Verified;
            doc.VerifiedAt = DateTime.UtcNow;
            doc.RejectionReason = null;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Document {DocumentId} accepted for clinic {ClinicId}", documentId, clinicId);
            return Result<ClinicDocument>.Success(doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting document {DocumentId} for clinic {ClinicId}", documentId, clinicId);
            return Result<ClinicDocument>.Failure("An error occurred. Please try again.");
        }
    }

    public async Task<Result<ClinicDocument>> RejectDocumentAsync(
        Guid clinicId,
        Guid documentId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return Result<ClinicDocument>.Failure("Rejection reason is required");
            }

            var docRepo = _unitOfWork.Repository<ClinicDocument>();
            var doc = await docRepo.GetByIdAsync(documentId);

            if (doc == null || doc.ClinicId != clinicId)
            {
                return Result<ClinicDocument>.Failure("Document not found");
            }

            doc.Status = DocumentStatus.Rejected;
            doc.RejectionReason = reason;
            doc.VerifiedAt = null;

            await _unitOfWork.SaveChangesAsync();

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Document {DocumentId} rejected for clinic {ClinicId}. Reason: {Reason}", documentId, clinicId, reason);
            }
            return Result<ClinicDocument>.Success(doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting document {DocumentId} for clinic {ClinicId}", documentId, clinicId);
            return Result<ClinicDocument>.Failure("An error occurred. Please try again.");
        }
    }
}
