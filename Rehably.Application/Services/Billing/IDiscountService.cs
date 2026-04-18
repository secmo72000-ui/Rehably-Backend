using Rehably.Application.Common;
using Rehably.Application.DTOs.Billing;

namespace Rehably.Application.Services.Billing;

public interface IDiscountService
{
    Task<PagedResult<DiscountDto>> GetDiscountsAsync(Guid clinicId, DiscountQueryParams query);
    Task<DiscountDto?> GetDiscountByIdAsync(Guid clinicId, Guid id);
    Task<DiscountDto> CreateDiscountAsync(Guid clinicId, CreateDiscountRequest request);
    Task<DiscountDto> UpdateDiscountAsync(Guid clinicId, Guid id, UpdateDiscountRequest request);
    Task DeleteDiscountAsync(Guid clinicId, Guid id);
    Task<ValidateDiscountResponse> ValidateCodeAsync(Guid clinicId, ValidateDiscountRequest request);
    Task<PagedResult<DiscountUsageDto>> GetUsagesAsync(Guid clinicId, Guid discountId, int page, int pageSize);
}
