using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.Application.DTOs.Billing;
using Rehably.Application.Services.Billing;

namespace Rehably.API.Controllers.Clinic;

[Authorize]
[Route("api/clinic/payments")]
public class ClinicPaymentsController : BaseController
{
    private readonly IClinicPaymentService _payments;
    public ClinicPaymentsController(IClinicPaymentService payments) => _payments = payments;

    private Guid ClinicId => TenantId.GetValueOrDefault();

    [HttpGet]
    public async Task<IActionResult> GetPayments([FromQuery] PaymentQueryParams query)
        => Ok(await _payments.GetPaymentsAsync(ClinicId, query));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPayment(Guid id)
    {
        var result = await _payments.GetPaymentByIdAsync(ClinicId, id);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentRequest request)
        => Ok(await _payments.RecordPaymentAsync(ClinicId, UserId ?? string.Empty, request));

    [HttpPost("{id:guid}/refund")]
    public async Task<IActionResult> RefundPayment(Guid id, [FromBody] RefundPaymentRequest request)
        => Ok(await _payments.RefundPaymentAsync(ClinicId, id, request));

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(await _payments.GetSummaryAsync(ClinicId, from, to));

    [HttpGet("policy")]
    public async Task<IActionResult> GetPolicy()
        => Ok(await _payments.GetPolicyAsync(ClinicId));

    [HttpPut("policy")]
    public async Task<IActionResult> UpsertPolicy([FromBody] UpdateBillingPolicyRequest request)
        => Ok(await _payments.UpsertPolicyAsync(ClinicId, request));
}
