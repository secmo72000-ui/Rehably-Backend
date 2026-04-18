using Microsoft.EntityFrameworkCore;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Repositories;

public class PaymentRepository : Repository<Payment>, IPaymentRepository
{
    public PaymentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Payment?> GetWithInvoiceAsync(Guid paymentId)
    {
        return await _context.Payments
            .Include(p => p.Invoice)
            .FirstOrDefaultAsync(p => p.Id == paymentId);
    }

    public async Task<IEnumerable<Payment>> GetByInvoiceIdAsync(Guid invoiceId)
    {
        return await _context.Payments
            .Where(p => p.InvoiceId == invoiceId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalPaidByInvoiceAsync(Guid invoiceId)
    {
        return await _context.Payments
            .Where(p => p.InvoiceId == invoiceId && p.Status == PaymentStatus.Completed)
            .SumAsync(p => p.Amount);
    }

    public async Task<Payment?> GetByTransactionIdAsync(string transactionId)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(t => t.Id.ToString() == transactionId || t.ProviderTransactionId == transactionId);
    }

    public async Task<Payment?> GetLatestByClinicIdAsync(Guid clinicId)
    {
        return await _context.Payments
            .Where(t => t.ClinicId == clinicId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }
}
