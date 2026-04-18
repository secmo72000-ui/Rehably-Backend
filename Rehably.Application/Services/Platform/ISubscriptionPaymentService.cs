using Rehably.Application.Common;
using Rehably.Application.DTOs.Platform;
using Rehably.Domain.Entities.Platform;

namespace Rehably.Application.Services.Platform;

public interface ISubscriptionPaymentService
{
    Task<Result<PaymentDto>> CreatePaymentAsync(Guid invoiceId, string provider, decimal amount, string currency);

    Task<Result<PaymentDto>> GetPaymentAsync(Guid paymentId);

    Task<Result<List<PaymentDto>>> GetInvoicePaymentsAsync(Guid invoiceId);

    Task<Result<PaymentDto>> ProcessPaymentCallbackAsync(Guid paymentId, string payload);

    Task<Result<PaymentDto>> RefundPaymentAsync(Guid paymentId, decimal? amount = null);
}
