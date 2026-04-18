using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinic;

namespace Rehably.Application.Services.Clinic;

/// <summary>
/// Orchestrates the complete clinic activation saga:
/// RegisterClinic → CreateSubscription → RecordPayment (skipped for Free) → ActivateClinic → SendWelcomeEmail.
/// Fail-fast on any step — sets clinic Status=Failed on failure, no rollback.
/// </summary>
public interface IClinicActivationService
{
    Task<Result<ClinicCreatedDto>> ActivateNewClinicAsync(
        CreateClinicRequest request,
        CancellationToken cancellationToken = default);
}
