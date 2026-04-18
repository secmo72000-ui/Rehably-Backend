using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinic;
using Rehably.Domain.Entities.Tenant;

namespace Rehably.Application.Services.Clinic;

public interface IClinicOnboardingApprovalService
{
    Task<Result<ClinicOnboarding>> ApproveOnboardingAsync(
        Guid onboardingId,
        ApproveClinicRequest? request = null,
        CancellationToken cancellationToken = default);

    Task<Result> RejectOnboardingAsync(
        Guid onboardingId,
        string reason,
        CancellationToken cancellationToken = default);

    public Task<Result<ClinicDocument>> AcceptDocumentAsync(
        Guid clinicId,
        Guid documentId,
        CancellationToken cancellationToken = default);

    public Task<Result<ClinicDocument>> RejectDocumentAsync(
        Guid clinicId,
        Guid documentId,
        string reason,
        CancellationToken cancellationToken = default);
}
