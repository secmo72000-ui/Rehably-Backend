using Microsoft.EntityFrameworkCore;
using Rehably.Application.Common;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Repositories;

/// <summary>
/// Implementation of IInvoiceRepository
/// </summary>
public class InvoiceRepository : Repository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Invoice?> GetWithLineItemsAsync(Guid invoiceId)
    {
        return await _dbSet
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
    }

    public async Task<Invoice?> GetWithDetailsAsync(Guid invoiceId)
    {
        return await _dbSet
            .Include(i => i.Payments)
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
    }

    public async Task<Invoice?> GetWithAdminDetailsAsync(Guid invoiceId)
    {
        return await _dbSet
            .Include(i => i.LineItems)
            .Include(i => i.Clinic)
            .Include(i => i.Subscription)
                .ThenInclude(s => s.Package)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
    }

    public async Task<IEnumerable<Invoice>> GetByClinicIdAsync(Guid clinicId)
    {
        return await _dbSet
            .Include(i => i.LineItems)
            .Where(i => i.ClinicId == clinicId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Invoice>> GetBySubscriptionIdAsync(Guid subscriptionId)
    {
        return await _dbSet
            .Where(i => i.SubscriptionId == subscriptionId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Invoice>> GetUnpaidInvoicesAsync()
    {
        return await _dbSet
            .Include(i => i.Clinic)
            .Where(i => i.PaidAt == null)
            .OrderByDescending(i => i.DueDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync()
    {
        return await _dbSet
            .Include(i => i.Clinic)
            .Where(i => i.PaidAt == null && i.DueDate < DateTime.UtcNow)
            .OrderByDescending(i => i.DueDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Invoice>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(i => i.CreatedAt >= startDate && i.CreatedAt <= endDate)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<Invoice?> GetLatestBySubscriptionIdAsync(Guid subscriptionId)
    {
        return await _dbSet
            .Where(i => i.SubscriptionId == subscriptionId)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber)
    {
        return await _dbSet
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);
    }

    public async Task<IEnumerable<Invoice>> GetPaidInvoicesByClinicAsync(Guid clinicId)
    {
        return await _dbSet
            .Where(i => i.ClinicId == clinicId && i.PaidAt != null)
            .OrderByDescending(i => i.PaidAt)
            .ToListAsync();
    }

    public async Task<string> GetNextInvoiceNumberAsync(CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;
        var connection = _context.Database.GetDbConnection();
        await _context.Database.OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT nextval('invoice_number_seq')";
        var result = await command.ExecuteScalarAsync(ct);
        var nextNumber = Convert.ToInt64(result);
        return $"INV-{year}-{nextNumber:D5}";
    }

    public async Task<PagedResult<Invoice>> GetPagedByClinicAsync(Guid clinicId, int page, int pageSize)
    {
        var query = _dbSet
            .Include(i => i.Payments)
            .Include(i => i.LineItems)
            .Where(i => i.ClinicId == clinicId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Invoice>(items, totalCount, page, pageSize);
    }

    public async Task<bool> HasOverdueInvoicesAsync(Guid clinicId)
    {
        return await _dbSet
            .AnyAsync(i => i.ClinicId == clinicId
                && i.PaidAt == null
                && i.DueDate < DateTime.UtcNow);
    }

    public async Task<(List<Invoice> Items, int TotalCount, decimal GrandTotal)> GetPagedForAdminAsync(
        Guid? clinicId, InvoiceStatus? status, DateTime? startDate, DateTime? endDate, int page, int pageSize)
    {
        var query = _dbSet
            .Include(i => i.LineItems)
            .Include(i => i.Clinic)
            .Include(i => i.Subscription)
                .ThenInclude(s => s.Package)
            .Include(i => i.Payments)
            .Where(i => !i.Clinic.IsDeleted)
            .AsQueryable();

        if (clinicId.HasValue)
            query = query.Where(i => i.ClinicId == clinicId.Value);

        if (status.HasValue)
        {
            query = status.Value switch
            {
                InvoiceStatus.Paid => query.Where(i => i.PaidAt != null),
                InvoiceStatus.Pending => query.Where(i => i.PaidAt == null && i.DueDate >= DateTime.UtcNow),
                InvoiceStatus.Overdue => query.Where(i => i.PaidAt == null && i.DueDate < DateTime.UtcNow),
                InvoiceStatus.Cancelled => query.Where(i => false),
                _ => query
            };
        }

        if (startDate.HasValue)
            query = query.Where(i => i.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(i => i.CreatedAt <= endDate.Value);

        var totalCount = await query.CountAsync();
        var grandTotal = await query.SumAsync(i => i.TotalAmount);

        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount, grandTotal);
    }

    public async Task DeleteAsync(Guid id)
    {
        var invoice = await _dbSet.FirstOrDefaultAsync(i => i.Id == id);
        if (invoice != null)
        {
            _dbSet.Remove(invoice);
        }
    }
}
