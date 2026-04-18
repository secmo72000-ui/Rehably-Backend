using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.Application.DTOs.Billing;
using Rehably.Application.Services.Billing;

namespace Rehably.API.Controllers.Clinic;

[Authorize]
[Route("api/clinic/discounts")]
public class ClinicDiscountsController : BaseController
{
    private readonly IDiscountService _discounts;
    public ClinicDiscountsController(IDiscountService discounts) => _discounts = discounts;

    private Guid ClinicId => TenantId.GetValueOrDefault();

    [HttpGet]
    public async Task<IActionResult> GetDiscounts([FromQuery] DiscountQueryParams query)
        => Ok(await _discounts.GetDiscountsAsync(ClinicId, query));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDiscount(Guid id)
    {
        var result = await _discounts.GetDiscountByIdAsync(ClinicId, id);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateDiscount([FromBody] CreateDiscountRequest request)
        => Ok(await _discounts.CreateDiscountAsync(ClinicId, request));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDiscount(Guid id, [FromBody] UpdateDiscountRequest request)
        => Ok(await _discounts.UpdateDiscountAsync(ClinicId, id, request));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDiscount(Guid id)
    {
        await _discounts.DeleteDiscountAsync(ClinicId, id);
        return NoContent();
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateCode([FromBody] ValidateDiscountRequest request)
        => Ok(await _discounts.ValidateCodeAsync(ClinicId, request));

    [HttpGet("{id:guid}/usages")]
    public async Task<IActionResult> GetUsages(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _discounts.GetUsagesAsync(ClinicId, id, page, pageSize));
}
