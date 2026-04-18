using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Application.Repositories;

public interface IClinicOnboardingRepository : IRepository<ClinicOnboarding>
{
    Task<ClinicOnboarding?> GetWithClinicAsync(Guid onboardingId, CancellationToken ct = default);
    Task<ClinicOnboarding?> GetByClinicIdAsync(Guid clinicId, CancellationToken ct = default);
    Task<ClinicOnboarding?> GetByClinicIdAndStepAsync(Guid clinicId, OnboardingStep step, CancellationToken ct = default);
    Task<IEnumerable<ClinicOnboarding>> GetPendingOnboardingsAsync(CancellationToken ct = default);
    Task<IEnumerable<ClinicOnboarding>> GetByStatusAsync(OnboardingStep step, CancellationToken ct = default);
    Task<bool> HasDocumentsAsync(Guid clinicId, CancellationToken ct = default);
}
