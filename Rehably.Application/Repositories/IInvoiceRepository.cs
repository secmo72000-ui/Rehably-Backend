using Rehably.Domain.Entities.Platform;
using Rehably.Application.Common;
using Rehably.Domain.Enums;

namespace Rehably.Application.Repositories;

/// <summary>
/// Repository interface for Invoice entity with specialized queries
/// </summary>
public interface IInvoiceRepository : IRepository<Invoice>
{
    /// <summary>
    /// Gets invoice with line items
    /// </summary>
    Task<Invoice?> GetWithLineItemsAsync(Guid invoiceId);

    /// <summary>
    /// Gets invoice with line items and payments
    /// </summary>
    Task<Invoice?> GetWithDetailsAsync(Guid invoiceId);

    /// <summary>
    /// Gets invoice with clinic and subscription details for admin
    /// </summary>
    Task<Invoice?> GetWithAdminDetailsAsync(Guid invoiceId);

    /// <summary>
    /// Gets invoices by clinic ID
    /// </summary>
    Task<IEnumerable<Invoice>> GetByClinicIdAsync(Guid clinicId);

    /// <summary>
    /// Gets invoices by subscription ID
    /// </summary>
    Task<IEnumerable<Invoice>> GetBySubscriptionIdAsync(Guid subscriptionId);

    /// <summary>
    /// Gets unpaid invoices (not yet paid)
    /// </summary>
    Task<IEnumerable<Invoice>> GetUnpaidInvoicesAsync();

    /// <summary>
    /// Gets overdue invoices (past due date and not paid)
    /// </summary>
    Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync();

    /// <summary>
    /// Gets invoices for a date range
    /// </summary>
    Task<IEnumerable<Invoice>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets the latest invoice for a subscription
    /// </summary>
    Task<Invoice?> GetLatestBySubscriptionIdAsync(Guid subscriptionId);

    /// <summary>
    /// Gets invoice by invoice number
    /// </summary>
    Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber);

    /// <summary>
    /// Gets paid invoices for a clinic
    /// </summary>
    Task<IEnumerable<Invoice>> GetPaidInvoicesByClinicAsync(Guid clinicId);

    /// <summary>
    /// Gets the next invoice number in sequence
    /// </summary>
    Task<string> GetNextInvoiceNumberAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets paged invoices for a clinic
    /// </summary>
    Task<PagedResult<Invoice>> GetPagedByClinicAsync(Guid clinicId, int page, int pageSize);

    /// <summary>
    /// Checks if overdue invoices exist for a clinic
    /// </summary>
    Task<bool> HasOverdueInvoicesAsync(Guid clinicId);

    /// <summary>
    /// Gets paged invoices with filtering for admin
    /// </summary>
    Task<(List<Invoice> Items, int TotalCount, decimal GrandTotal)> GetPagedForAdminAsync(
        Guid? clinicId, InvoiceStatus? status, DateTime? startDate, DateTime? endDate, int page, int pageSize);

    /// <summary>
    /// Deletes an invoice by ID
    /// </summary>
    Task DeleteAsync(Guid id);
}
