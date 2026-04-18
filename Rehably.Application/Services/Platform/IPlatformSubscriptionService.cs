using Rehably.Application.Common;
using Rehably.Application.DTOs.Subscription;
using Rehably.Domain.Enums;

namespace Rehably.Application.Services.Platform;

public interface IPlatformSubscriptionService
{
    Task<Result<SubscriptionDto>> GetSubscriptionByIdAsync(Guid id);
    Task<Result<SubscriptionDetailDto>> GetSubscriptionWithDetailsAsync(Guid id);
    Task<Result<List<SubscriptionDto>>> GetSubscriptionsAsync(Guid? clinicId = null);
    Task<Result<PagedResult<SubscriptionDto>>> GetSubscriptionsPagedAsync(int page, int pageSize, SubscriptionStatus? status = null, Guid? clinicId = null);
}
