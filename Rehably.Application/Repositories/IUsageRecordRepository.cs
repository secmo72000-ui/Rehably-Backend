using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Application.Repositories;

/// <summary>
/// Repository interface for UsageRecord entity with specialized queries
/// </summary>
public interface IUsageRecordRepository : IRepository<UsageRecord>
{
    /// <summary>
    /// Gets usage record for a clinic, metric, and period
    /// </summary>
    Task<UsageRecord?> GetByClinicMetricPeriodAsync(Guid clinicId, UsageMetric metric, DateTime period);

    /// <summary>
    /// Gets all usage records for a clinic in a period
    /// </summary>
    Task<IEnumerable<UsageRecord>> GetByClinicAndPeriodAsync(Guid clinicId, DateTime period);

    /// <summary>
    /// Gets usage records for a clinic in a previous period
    /// </summary>
    Task<IEnumerable<UsageRecord>> GetPreviousPeriodAsync(Guid clinicId);
}
