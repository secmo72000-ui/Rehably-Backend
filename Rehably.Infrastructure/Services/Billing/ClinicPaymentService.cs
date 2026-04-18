using Microsoft.EntityFrameworkCore;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Billing;
using Rehably.Application.Services.Billing;
using Rehably.Domain.Entities.Billing;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Services.Billing;

public class ClinicPaymentService : IClinicPaymentService
{
    private readonly ApplicationDbContext _db;
    public ClinicPaymentService(ApplicationDbContext db) => _db = db;

    public async Task<PagedResult<ClinicPaymentDto>> GetPaymentsAsync(Guid clinicId, PaymentQueryParams query)
    {
        var q = _db.ClinicPayments.Include(p => p.Invoice).Where(p => p.ClinicId == clinicId);
        if (query.PatientId.HasValue) q = q.Where(p => p.PatientId == query.PatientId);
        if (query.InvoiceId.HasValue) q = q.Where(p => p.InvoiceId == query.InvoiceId);
        if (query.Method.HasValue) q = q.Where(p => p.Method == query.Method);
        if (query.Status.HasValue) q = q.Where(p => p.Status == query.Status);
        if (query.FromDate.HasValue) q = q.Where(p => p.CreatedAt >= query.FromDate);
        if (query.ToDate.HasValue) q = q.Where(p => p.CreatedAt <= query.ToDate);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(p => p.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .ToListAsync();
        return new PagedResult<ClinicPaymentDto>(items.Select(MapPayment).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<ClinicPaymentDto?> GetPaymentByIdAsync(Guid clinicId, Guid id)
    {
        var e = await _db.ClinicPayments.Include(p => p.Invoice)
            .FirstOrDefaultAsync(p => p.ClinicId == clinicId && p.Id == id);
        return e == null ? null : MapPayment(e);
    }

    public async Task<ClinicPaymentDto> RecordPaymentAsync(Guid clinicId, string recordedByUserId, RecordPaymentRequest request)
    {
        var invoice = await _db.ClinicInvoices.FindAsync(request.InvoiceId)
            ?? throw new KeyNotFoundException("Invoice not found");
        if (invoice.ClinicId != clinicId) throw new UnauthorizedAccessException();

        var payment = new ClinicPayment
        {
            Id = Guid.NewGuid(), ClinicId = clinicId,
            InvoiceId = request.InvoiceId, PatientId = invoice.PatientId,
            Amount = request.Amount, Method = request.Method,
            Status = PaymentStatus.Completed,
            TransactionReference = request.TransactionReference,
            Notes = request.Notes,
            RecordedByUserId = recordedByUserId,
            PaidAt = DateTime.UtcNow
        };

        _db.ClinicPayments.Add(payment);

        // Update invoice totals
        invoice.TotalPaid += request.Amount;
        invoice.UpdatedAt = DateTime.UtcNow;
        if (invoice.TotalPaid >= invoice.TotalDue)
        {
            invoice.Status = ClinicInvoiceStatus.Paid;
            invoice.PaidAt = DateTime.UtcNow;
        }
        else if (invoice.TotalPaid > 0)
        {
            invoice.Status = ClinicInvoiceStatus.PartiallyPaid;
        }

        await _db.SaveChangesAsync();
        await _db.Entry(payment).Reference(p => p.Invoice).LoadAsync();
        return MapPayment(payment);
    }

    public async Task<ClinicPaymentDto> RefundPaymentAsync(Guid clinicId, Guid id, RefundPaymentRequest request)
    {
        var payment = await _db.ClinicPayments.Include(p => p.Invoice)
            .FirstOrDefaultAsync(p => p.ClinicId == clinicId && p.Id == id)
            ?? throw new KeyNotFoundException("Payment not found");

        payment.Status = PaymentStatus.Refunded;
        payment.Notes = string.IsNullOrWhiteSpace(request.Reason)
            ? payment.Notes
            : $"{payment.Notes} | استرداد: {request.Reason}";

        // Reverse invoice totals
        payment.Invoice.TotalPaid -= payment.Amount;
        payment.Invoice.Status = payment.Invoice.TotalPaid <= 0
            ? ClinicInvoiceStatus.Refunded
            : ClinicInvoiceStatus.PartiallyPaid;
        payment.Invoice.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapPayment(payment);
    }

    public async Task<PaymentSummaryDto> GetSummaryAsync(Guid clinicId, DateTime? from, DateTime? to)
    {
        var q = _db.ClinicPayments.Where(p => p.ClinicId == clinicId);
        if (from.HasValue) q = q.Where(p => p.CreatedAt >= from);
        if (to.HasValue) q = q.Where(p => p.CreatedAt <= to);

        var payments = await q.ToListAsync();
        return new PaymentSummaryDto(
            payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount),
            payments.Where(p => p.Status == PaymentStatus.Pending).Sum(p => p.Amount),
            payments.Where(p => p.Status == PaymentStatus.Refunded).Sum(p => p.Amount),
            payments.Count,
            payments.Where(p => p.Status == PaymentStatus.Completed)
                .GroupBy(p => p.Method.ToString())
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount))
        );
    }

    public async Task<BillingPolicyDto> GetPolicyAsync(Guid clinicId)
    {
        var policy = await _db.ClinicBillingPolicies.FirstOrDefaultAsync(p => p.ClinicId == clinicId);
        if (policy == null)
        {
            policy = new ClinicBillingPolicy { Id = Guid.NewGuid(), ClinicId = clinicId };
            _db.ClinicBillingPolicies.Add(policy);
            await _db.SaveChangesAsync();
        }
        return MapPolicy(policy);
    }

    public async Task<BillingPolicyDto> UpsertPolicyAsync(Guid clinicId, UpdateBillingPolicyRequest request)
    {
        var policy = await _db.ClinicBillingPolicies.FirstOrDefaultAsync(p => p.ClinicId == clinicId);
        if (policy == null)
        {
            policy = new ClinicBillingPolicy { Id = Guid.NewGuid(), ClinicId = clinicId };
            _db.ClinicBillingPolicies.Add(policy);
        }

        policy.DefaultPaymentTiming = request.DefaultPaymentTiming;
        policy.AllowInstallments = request.AllowInstallments;
        policy.AllowDiscountStackWithInsurance = request.AllowDiscountStackWithInsurance;
        policy.AllowMultipleDiscounts = request.AllowMultipleDiscounts;
        policy.RequirePreAuthForInsurance = request.RequirePreAuthForInsurance;
        policy.DefaultCurrency = request.DefaultCurrency;
        policy.TaxRatePercent = request.TaxRatePercent;
        policy.InvoicePrefix = request.InvoicePrefix;
        policy.AutoGenerateInvoice = request.AutoGenerateInvoice;
        policy.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return MapPolicy(policy);
    }

    private static ClinicPaymentDto MapPayment(ClinicPayment p) =>
        new(p.Id, p.InvoiceId, p.Invoice.InvoiceNumber, p.PatientId, string.Empty,
            p.Amount, p.Method, p.Status, p.TransactionReference,
            p.PaymentGateway, p.PaidAt, p.Notes, p.RecordedByUserId, p.CreatedAt);

    private static BillingPolicyDto MapPolicy(ClinicBillingPolicy p) =>
        new(p.Id, p.DefaultPaymentTiming, p.AllowInstallments,
            p.AllowDiscountStackWithInsurance, p.AllowMultipleDiscounts,
            p.RequirePreAuthForInsurance, p.DefaultCurrency,
            p.TaxRatePercent, p.InvoicePrefix, p.AutoGenerateInvoice);
}
