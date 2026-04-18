using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinic;
using Rehably.Application.DTOs.Payment;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Application.Services.Clinic;

public interface IClinicOnboardingService
{
    Task<Result<ClinicDocumentsResponseDto>> GetClinicDocumentsAsync(
        Guid clinicId,
        CancellationToken cancellationToken = default);

    Task<Result<ClinicOnboarding>> StartOnboardingAsync(
        string name,
        string email,
        string phone,
        string ownerId,
        CancellationToken cancellationToken = default);

    Task<Result> VerifyEmailAsync(
        Guid onboardingId,
        string otp,
        CancellationToken cancellationToken = default);

    Task<Result<ClinicDocument>> UploadDocumentAsync(
        Guid onboardingId,
        DocumentType documentType,
        string fileName,
        Stream stream,
        CancellationToken cancellationToken = default);

    Task<Result<ClinicOnboarding>> SubmitForApprovalAsync(
        Guid onboardingId,
        CancellationToken cancellationToken = default);

    Task<Result<PaymentResponse>> InitiatePaymentAsync(
        Guid onboardingId,
        Guid subscriptionPlanId,
        string returnUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default);

    Task<Result<ClinicOnboarding>> CompleteOnboardingAsync(
        Guid onboardingId,
        CancellationToken cancellationToken = default);

    Task<Result<ClinicOnboarding>> GetOnboardingAsync(
        Guid onboardingId,
        CancellationToken cancellationToken = default);

    Task<Result<ClinicOnboarding>> GetOnboardingByClinicIdAsync(
        Guid clinicId,
        CancellationToken cancellationToken = default);
}
