namespace Rehably.Application.Services.Platform;

public interface IUsageAuditService
{
    Task LogLimitWarningAsync(Guid clinicId, string featureCode, int used, int limit, string? userId = null, string? ipAddress = null);
    Task LogLimitExceededAsync(Guid clinicId, string featureCode, int requested, int limit, string? userId = null, string? ipAddress = null);
    Task LogUsageResetAsync(Guid clinicId, string featureCode, int previousValue, string? userId = null);
    Task LogSubscriptionChangedAsync(Guid clinicId, Guid oldSubscriptionId, Guid newSubscriptionId, string? userId = null);
}
