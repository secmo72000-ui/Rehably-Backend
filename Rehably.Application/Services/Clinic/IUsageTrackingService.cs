using Rehably.Application.DTOs.Subscription;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Application.Services.Clinic;

public interface IUsageTrackingService
{
    Task RecordStorageUsageAsync(Guid clinicId, long bytesUsed);
    Task RecordPatientCountAsync(Guid clinicId, int count);
    Task RecordUserCountAsync(Guid clinicId, int count);
    Task RecordApiCallAsync(Guid clinicId);
    Task<UsageStatisticsResponse> GetUsageStatisticsAsync(Guid clinicId, int days = 30);
    Task<bool> IsStorageLimitExceededAsync(Guid clinicId);
    Task<bool> IsPatientsLimitExceededAsync(Guid clinicId);
    Task<bool> IsUsersLimitExceededAsync(Guid clinicId);
    Task<List<UsageHistory>> GetUsageHistoryAsync(Guid clinicId, MetricType metricType, DateTime fromDate, DateTime toDate);
}
