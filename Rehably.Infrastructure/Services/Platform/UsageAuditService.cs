using Rehably.Application.Services.Platform;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;

namespace Rehably.Infrastructure.Services.Platform;

public class UsageAuditService(IRepository<UsageAuditLog> repository, IUnitOfWork unitOfWork) : IUsageAuditService
{
    public async Task LogLimitWarningAsync(Guid clinicId, string featureCode, int used, int limit, string? userId = null, string? ipAddress = null)
    {
        var percentage = (int)Math.Round((double)used / limit * 100);
        var log = new UsageAuditLog
        {
            ClinicId = clinicId,
            EventType = "LimitWarning",
            FeatureCode = featureCode,
            Used = used,
            Limit = limit,
            Message = $"Usage warning for {featureCode}: {used}/{limit} ({percentage}%)",
            UserId = userId,
            IpAddress = ipAddress
        };

        await repository.AddAsync(log);
        await unitOfWork.SaveChangesAsync();
    }

    public async Task LogLimitExceededAsync(Guid clinicId, string featureCode, int requested, int limit, string? userId = null, string? ipAddress = null)
    {
        var log = new UsageAuditLog
        {
            ClinicId = clinicId,
            EventType = "LimitExceeded",
            FeatureCode = featureCode,
            Used = requested,
            Limit = limit,
            Message = $"Limit exceeded for {featureCode}: requested {requested}, limit is {limit}",
            UserId = userId,
            IpAddress = ipAddress
        };

        await repository.AddAsync(log);
        await unitOfWork.SaveChangesAsync();
    }

    public async Task LogUsageResetAsync(Guid clinicId, string featureCode, int previousValue, string? userId = null)
    {
        var log = new UsageAuditLog
        {
            ClinicId = clinicId,
            EventType = "UsageReset",
            FeatureCode = featureCode,
            Used = 0,
            Limit = null,
            Message = $"Usage reset for {featureCode}: was {previousValue}, now 0",
            UserId = userId
        };

        await repository.AddAsync(log);
        await unitOfWork.SaveChangesAsync();
    }

    public async Task LogSubscriptionChangedAsync(Guid clinicId, Guid oldSubscriptionId, Guid newSubscriptionId, string? userId = null)
    {
        var log = new UsageAuditLog
        {
            ClinicId = clinicId,
            EventType = "SubscriptionChanged",
            Message = $"Subscription changed from {oldSubscriptionId} to {newSubscriptionId}",
            UserId = userId
        };

        await repository.AddAsync(log);
        await unitOfWork.SaveChangesAsync();
    }
}
