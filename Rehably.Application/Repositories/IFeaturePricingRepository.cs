using Rehably.Domain.Entities.Platform;

namespace Rehably.Application.Repositories;

public interface IFeaturePricingRepository : IRepository<FeaturePricing>
{
    Task<FeaturePricing?> GetCurrentPricingAsync(Guid featureId);
    Task<FeaturePricing?> GetPricingAtDateAsync(Guid featureId, DateTime effectiveDate);
    Task<IEnumerable<FeaturePricing>> GetPricingHistoryAsync(Guid featureId);
}
