using Microsoft.EntityFrameworkCore;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Repositories;

public class ClinicOnboardingRepository : Repository<ClinicOnboarding>, IClinicOnboardingRepository
{
    public ClinicOnboardingRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<ClinicOnboarding?> GetWithClinicAsync(Guid onboardingId, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(o => o.Clinic)
            .FirstOrDefaultAsync(o => o.Id == onboardingId, ct);
    }

    public async Task<ClinicOnboarding?> GetByClinicIdAsync(Guid clinicId, CancellationToken ct = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(o => o.ClinicId == clinicId, ct);
    }

    public async Task<ClinicOnboarding?> GetByClinicIdAndStepAsync(Guid clinicId, OnboardingStep step, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(o => o.Clinic)
            .FirstOrDefaultAsync(o => o.ClinicId == clinicId && o.CurrentStep == step, ct);
    }

    public async Task<IEnumerable<ClinicOnboarding>> GetPendingOnboardingsAsync(CancellationToken ct = default)
    {
        return await _dbSet
            .Include(o => o.Clinic)
            .Where(o => o.CurrentStep != OnboardingStep.Completed)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<ClinicOnboarding>> GetByStatusAsync(OnboardingStep step, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(o => o.Clinic)
            .Where(o => o.CurrentStep == step)
            .ToListAsync(ct);
    }

    public async Task<bool> HasDocumentsAsync(Guid clinicId, CancellationToken ct = default)
    {
        return await _context.ClinicDocuments
            .AnyAsync(d => d.ClinicId == clinicId, ct);
    }
}
