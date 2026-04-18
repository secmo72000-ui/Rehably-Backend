using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;

namespace Rehably.Application.Repositories;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<Payment?> GetWithInvoiceAsync(Guid paymentId);
    Task<IEnumerable<Payment>> GetByInvoiceIdAsync(Guid invoiceId);
    Task<decimal> GetTotalPaidByInvoiceAsync(Guid invoiceId);
    Task<Payment?> GetByTransactionIdAsync(string transactionId);
    Task<Payment?> GetLatestByClinicIdAsync(Guid clinicId);
}
