using Rehably.Application.Common;
using Rehably.Application.DTOs.Billing;

namespace Rehably.Application.Services.Billing;

public interface IClinicInvoiceService
{
    Task<PagedResult<InvoiceSummaryDto>> GetInvoicesAsync(Guid clinicId, InvoiceQueryParams query);
    Task<ClinicInvoiceDto?> GetInvoiceByIdAsync(Guid clinicId, Guid id);
    Task<ClinicInvoiceDto> CreateInvoiceAsync(Guid clinicId, CreateInvoiceRequest request);
    Task<ClinicInvoiceDto> UpdateInvoiceAsync(Guid clinicId, Guid id, UpdateInvoiceRequest request);
    Task CancelInvoiceAsync(Guid clinicId, Guid id);
    Task<ClinicInvoiceDto> CreateInstallmentPlanAsync(Guid clinicId, Guid invoiceId, CreateInstallmentPlanRequest request);
    Task<BillingBreakdownResponse> CalculateBreakdownAsync(Guid clinicId, BillingBreakdownRequest request);
    Task<string> GenerateInvoiceNumberAsync(Guid clinicId);
}
