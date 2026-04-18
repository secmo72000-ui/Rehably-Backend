using Rehably.Application.Common;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Application.Services.Clinic;

public interface IClinicUsageTrackingService
{
    Task<Result> IncrementUsageAsync(Guid clinicId, UsageMetric metric, long delta = 1, CancellationToken cancellationToken = default);

    Task<Result<long>> GetCurrentUsageAsync(Guid clinicId, UsageMetric metric, CancellationToken cancellationToken = default);

    Task<Result<Dictionary<UsageMetric, long>>> GetAllUsageAsync(Guid clinicId, CancellationToken cancellationToken = default);

    Task<Result<bool>> IsWithinLimitsAsync(Guid clinicId, CancellationToken cancellationToken = default);

    Task<Result> ResetMonthlyUsageAsync(Guid clinicId, CancellationToken cancellationToken = default);
}
