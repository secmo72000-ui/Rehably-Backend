using Rehably.Application.Common;
using Rehably.Application.DTOs.Invoice;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;

namespace Rehably.Application.Services.Platform;

public interface IInvoiceService
{
    Task<Result<InvoiceDto>> GenerateInvoiceAsync(Guid subscriptionId);

    Task<Result<InvoiceDto>> GetInvoiceAsync(Guid invoiceId);

    Task<Result<PagedResult<InvoiceDto>>> GetInvoicesAsync(
        Guid clinicId,
        int page = 1,
        int pageSize = 20,
        SubscriptionStatus? status = null);

    Task<Result<InvoiceDto>> MarkAsPaidAsync(Guid invoiceId, string paymentProvider, string transactionId);

    Task<Result<bool>> OverdueInvoicesExistAsync(Guid clinicId);

    Task<Result<List<InvoiceDto>>> GenerateInvoicesForDueSubscriptionsAsync();

    Task<Result<InvoiceDto>> GenerateSubscriptionInvoiceAsync(Subscription subscription, Package package, PaymentType paymentType);

    // Admin methods
    Task<Result<InvoiceListResponseDto>> GetAllInvoicesAsync(Guid? clinicId, InvoiceStatus? status, DateTime? startDate, DateTime? endDate, int page, int pageSize);
    Task<Result<AdminInvoiceDto>> GetInvoiceDetailAsync(Guid invoiceId);
    Task<Result<AdminInvoiceDto>> MarkInvoiceAsPaidByAdminAsync(Guid invoiceId, MarkInvoicePaidRequest request);
    Task<Result<bool>> DeleteInvoiceAsync(Guid id, Guid adminUserId);
    Task<Result<byte[]>> GenerateInvoicePdfAsync(Guid id);
}
