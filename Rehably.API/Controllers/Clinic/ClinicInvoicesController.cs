using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.Application.DTOs.Billing;
using Rehably.Application.Services.Billing;

namespace Rehably.API.Controllers.Clinic;

[Authorize]
[Route("api/clinic/invoices")]
public class ClinicInvoicesController : BaseController
{
    private readonly IClinicInvoiceService _invoices;
    public ClinicInvoicesController(IClinicInvoiceService invoices) => _invoices = invoices;

    private Guid ClinicId => TenantId.GetValueOrDefault();

    [HttpGet]
    public async Task<IActionResult> GetInvoices([FromQuery] InvoiceQueryParams query)
        => Ok(await _invoices.GetInvoicesAsync(ClinicId, query));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetInvoice(Guid id)
    {
        var result = await _invoices.GetInvoiceByIdAsync(ClinicId, id);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request)
        => Ok(await _invoices.CreateInvoiceAsync(ClinicId, request));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateInvoice(Guid id, [FromBody] UpdateInvoiceRequest request)
        => Ok(await _invoices.UpdateInvoiceAsync(ClinicId, id, request));

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelInvoice(Guid id)
    {
        await _invoices.CancelInvoiceAsync(ClinicId, id);
        return NoContent();
    }

    [HttpPost("{id:guid}/installments")]
    public async Task<IActionResult> CreateInstallmentPlan(Guid id, [FromBody] CreateInstallmentPlanRequest request)
        => Ok(await _invoices.CreateInstallmentPlanAsync(ClinicId, id, request));

    [HttpPost("breakdown")]
    public async Task<IActionResult> CalculateBreakdown([FromBody] BillingBreakdownRequest request)
        => Ok(await _invoices.CalculateBreakdownAsync(ClinicId, request));
}
