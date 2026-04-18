using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.API.Authorization;
using Rehably.Application.DTOs.Invoice;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Enums;

namespace Rehably.API.Controllers.Admin;

/// <summary>
/// Admin invoice management for platform administrators.
/// </summary>
[ApiController]
[Route("api/admin/[controller]")]
[Authorize]
[RequirePermission("invoices.view")]
[Produces("application/json")]
[Tags("Admin - Invoices")]
public class InvoicesController : BaseController
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(IInvoiceService invoiceService, ILogger<InvoicesController> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    /// <summary>
    /// Get all invoices with filtering and pagination. PageSize is capped at 100.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(InvoiceListResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InvoiceListResponseDto>> GetAllInvoices(
        [FromQuery] Guid? clinicId = null,
        [FromQuery] InvoiceStatus? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await _invoiceService.GetAllInvoicesAsync(clinicId, status, startDate, endDate, page, pageSize);
        return FromResult(result);
    }

    /// <summary>
    /// Get invoice detail by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminInvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminInvoiceDto>> GetInvoiceDetail(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _invoiceService.GetInvoiceDetailAsync(id);
        return FromResult(result);
    }

    /// <summary>
    /// Mark invoice as paid by admin (manual payment recording).
    /// </summary>
    [HttpPost("{id:guid}/mark-paid")]
    [RequirePermission("invoices.update")]
    [ProducesResponseType(typeof(AdminInvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminInvoiceDto>> MarkInvoicePaid(Guid id, [FromBody] MarkInvoicePaidRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _invoiceService.MarkInvoiceAsPaidByAdminAsync(id, request);
        return FromResult(result);
    }

    /// <summary>
    /// Delete an invoice. Blocked if the invoice has active (Pending or Processing) transactions.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission("invoices.delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteInvoice(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _invoiceService.DeleteInvoiceAsync(id, CurrentAdminId);
        if (!result.IsSuccess)
        {
            if (result.Error!.Contains("active", StringComparison.OrdinalIgnoreCase))
                return ConflictError(result.Error);
            if (result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFoundError(result.Error);
            return ValidationError(result.Error!);
        }

        return NoContent();
    }

    /// <summary>
    /// Generate a PDF for the specified invoice.
    /// </summary>
    [HttpGet("{id:guid}/pdf")]
    [RequirePermission("invoices.view")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvoicePdf(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _invoiceService.GenerateInvoicePdfAsync(id);
        if (!result.IsSuccess)
            return NotFoundError(result.Error ?? "Invoice not found");

        return File(result.Value!, "application/pdf", $"invoice-{id}.pdf");
    }
}

