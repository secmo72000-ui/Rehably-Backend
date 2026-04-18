using Microsoft.EntityFrameworkCore;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Billing;
using Rehably.Application.Services.Billing;
using Rehably.Domain.Entities.Billing;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Services.Billing;

public class ClinicInvoiceService : IClinicInvoiceService
{
    private readonly ApplicationDbContext _db;
    public ClinicInvoiceService(ApplicationDbContext db) => _db = db;

    public async Task<PagedResult<InvoiceSummaryDto>> GetInvoicesAsync(Guid clinicId, InvoiceQueryParams query)
    {
        var q = _db.ClinicInvoices.Where(i => i.ClinicId == clinicId);
        if (query.Status.HasValue) q = q.Where(i => i.Status == query.Status);
        if (query.PatientId.HasValue) q = q.Where(i => i.PatientId == query.PatientId);
        if (query.FromDate.HasValue) q = q.Where(i => i.CreatedAt >= query.FromDate);
        if (query.ToDate.HasValue) q = q.Where(i => i.CreatedAt <= query.ToDate);
        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(i => i.InvoiceNumber.Contains(query.Search));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(i => i.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(i => new InvoiceSummaryDto(i.Id, i.InvoiceNumber, i.PatientId, string.Empty,
                i.Status, i.TotalDue, i.TotalPaid, i.TotalDue - i.TotalPaid,
                i.Currency, i.DueDate, i.CreatedAt))
            .ToListAsync();
        return new PagedResult<InvoiceSummaryDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<ClinicInvoiceDto?> GetInvoiceByIdAsync(Guid clinicId, Guid id)
    {
        var e = await _db.ClinicInvoices
            .Include(i => i.LineItems)
            .Include(i => i.InstallmentPlan).ThenInclude(p => p!.Schedule)
            .FirstOrDefaultAsync(i => i.ClinicId == clinicId && i.Id == id);
        return e == null ? null : MapInvoice(e);
    }

    public async Task<ClinicInvoiceDto> CreateInvoiceAsync(Guid clinicId, CreateInvoiceRequest request)
    {
        var policy = await GetOrCreatePolicyAsync(clinicId);
        var invoiceNumber = await GenerateInvoiceNumberAsync(clinicId);

        var lineItems = request.LineItems.Select(l => new ClinicInvoiceLineItem
        {
            Id = Guid.NewGuid(),
            Description = l.Description, DescriptionArabic = l.DescriptionArabic,
            Quantity = l.Quantity, UnitPrice = l.UnitPrice,
            ServiceType = l.ServiceType,
            LineTotal = l.Quantity * l.UnitPrice
        }).ToList();

        var subTotal = lineItems.Sum(l => l.LineTotal);
        decimal insuranceCoverage = 0;
        decimal discountAmount = 0;

        // Apply insurance
        if (request.PatientInsuranceId.HasValue)
        {
            var pi = await _db.PatientInsurances
                .Include(p => p.ClinicInsuranceProvider).ThenInclude(c => c.ServiceRules)
                .FirstOrDefaultAsync(p => p.Id == request.PatientInsuranceId);
            if (pi != null && pi.IsActive)
            {
                foreach (var li in lineItems)
                {
                    var rule = pi.ClinicInsuranceProvider.ServiceRules.FirstOrDefault(r => r.ServiceType == li.ServiceType);
                    var pct = rule != null
                        ? (rule.CoverageType == CoverageType.Percentage ? rule.CoverageValue : rule.CoverageValue / li.LineTotal * 100)
                        : pi.CoveragePercent;
                    li.InsuranceCoverageAmount = Math.Round(li.LineTotal * pct / 100, 2);
                    insuranceCoverage += li.InsuranceCoverageAmount;
                }
            }
        }

        // Apply discounts (stacking controlled by policy)
        if (request.DiscountIds != null && request.DiscountIds.Count > 0)
        {
            var idsToApply = policy.AllowMultipleDiscounts ? request.DiscountIds : request.DiscountIds.Take(1).ToList();
            var discounts = await _db.Discounts.Where(d => idsToApply.Contains(d.Id) && d.IsActive).ToListAsync();
            foreach (var d in discounts)
            {
                var da = d.Type == DiscountType.Percentage
                    ? (subTotal - insuranceCoverage) * d.Value / 100
                    : d.Value;
                discountAmount += Math.Round(da, 2);
            }
        }

        var taxAmount = policy.TaxRatePercent.HasValue
            ? Math.Round((subTotal - insuranceCoverage - discountAmount) * policy.TaxRatePercent.Value / 100, 2)
            : 0;
        var totalDue = subTotal - insuranceCoverage - discountAmount + taxAmount;

        var invoice = new ClinicInvoice
        {
            Id = Guid.NewGuid(), ClinicId = clinicId,
            PatientId = request.PatientId,
            AppointmentId = request.AppointmentId,
            TreatmentPlanId = request.TreatmentPlanId,
            InvoiceNumber = invoiceNumber,
            Status = ClinicInvoiceStatus.Issued,
            SubTotal = subTotal,
            InsuranceCoverageAmount = insuranceCoverage,
            DiscountAmount = discountAmount,
            TaxAmount = taxAmount,
            TotalDue = Math.Max(totalDue, 0),
            Currency = request.Currency ?? policy.DefaultCurrency,
            DueDate = request.DueDate,
            Notes = request.Notes,
            IssuedAt = DateTime.UtcNow,
            LineItems = lineItems
        };

        _db.ClinicInvoices.Add(invoice);
        await _db.SaveChangesAsync();
        return MapInvoice(invoice);
    }

    public async Task<ClinicInvoiceDto> UpdateInvoiceAsync(Guid clinicId, Guid id, UpdateInvoiceRequest request)
    {
        var invoice = await _db.ClinicInvoices.Include(i => i.LineItems).Include(i => i.InstallmentPlan).ThenInclude(p => p!.Schedule)
            .FirstOrDefaultAsync(i => i.ClinicId == clinicId && i.Id == id)
            ?? throw new KeyNotFoundException("Invoice not found");

        invoice.Status = request.Status;
        invoice.DueDate = request.DueDate;
        invoice.Notes = request.Notes;
        invoice.UpdatedAt = DateTime.UtcNow;
        if (request.Status == ClinicInvoiceStatus.Paid) invoice.PaidAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return MapInvoice(invoice);
    }

    public async Task CancelInvoiceAsync(Guid clinicId, Guid id)
    {
        var invoice = await _db.ClinicInvoices.FirstOrDefaultAsync(i => i.ClinicId == clinicId && i.Id == id)
            ?? throw new KeyNotFoundException("Invoice not found");
        invoice.Status = ClinicInvoiceStatus.Cancelled;
        invoice.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<ClinicInvoiceDto> CreateInstallmentPlanAsync(Guid clinicId, Guid invoiceId, CreateInstallmentPlanRequest request)
    {
        var invoice = await _db.ClinicInvoices.Include(i => i.LineItems).Include(i => i.InstallmentPlan).ThenInclude(p => p!.Schedule)
            .FirstOrDefaultAsync(i => i.ClinicId == clinicId && i.Id == invoiceId)
            ?? throw new KeyNotFoundException("Invoice not found");

        var installmentAmount = Math.Round(invoice.TotalDue / request.NumberOfInstallments, 2);
        var plan = new InstallmentPlan
        {
            Id = Guid.NewGuid(), InvoiceId = invoiceId,
            TotalAmount = invoice.TotalDue,
            NumberOfInstallments = request.NumberOfInstallments,
            StartDate = request.StartDate, Notes = request.Notes,
            Schedule = Enumerable.Range(0, request.NumberOfInstallments).Select(i => new InstallmentSchedule
            {
                Id = Guid.NewGuid(),
                DueDate = request.StartDate.AddMonths(i),
                Amount = installmentAmount,
                Status = InstallmentStatus.Pending
            }).ToList()
        };

        _db.InstallmentPlans.Add(plan);
        await _db.SaveChangesAsync();
        invoice.InstallmentPlan = plan;
        return MapInvoice(invoice);
    }

    public async Task<BillingBreakdownResponse> CalculateBreakdownAsync(Guid clinicId, BillingBreakdownRequest request)
    {
        var policy = await GetOrCreatePolicyAsync(clinicId);
        var lines = new List<BillingBreakdownLineDto>();
        decimal subTotal = 0, insuranceCoverage = 0, discountAmount = 0;

        Domain.Entities.Billing.PatientInsurance? pi = null;
        if (request.PatientInsuranceId.HasValue)
            pi = await _db.PatientInsurances
                .Include(p => p.ClinicInsuranceProvider).ThenInclude(c => c.ServiceRules)
                .FirstOrDefaultAsync(p => p.Id == request.PatientInsuranceId);

        foreach (var l in request.LineItems)
        {
            var lineTotal = l.Quantity * l.UnitPrice;
            subTotal += lineTotal;
            decimal lineCoverage = 0;

            if (pi != null && pi.IsActive)
            {
                var rule = pi.ClinicInsuranceProvider.ServiceRules.FirstOrDefault(r => r.ServiceType == l.ServiceType);
                var pct = rule != null
                    ? (rule.CoverageType == CoverageType.Percentage ? rule.CoverageValue : rule.CoverageValue / lineTotal * 100)
                    : pi.CoveragePercent;
                lineCoverage = Math.Round(lineTotal * pct / 100, 2);
                insuranceCoverage += lineCoverage;
            }
            lines.Add(new BillingBreakdownLineDto(l.Description, l.Quantity, l.UnitPrice, lineCoverage, 0, lineTotal - lineCoverage));
        }

        // Discounts
        if (!string.IsNullOrWhiteSpace(request.PromoCode))
        {
            var d = await _db.Discounts.FirstOrDefaultAsync(d => d.ClinicId == clinicId && d.Code == request.PromoCode && d.IsActive);
            if (d != null)
                discountAmount = d.Type == DiscountType.Percentage
                    ? Math.Round((subTotal - insuranceCoverage) * d.Value / 100, 2)
                    : d.Value;
        }
        if (request.DiscountIds != null)
        {
            var idsToApply = policy.AllowMultipleDiscounts ? request.DiscountIds : request.DiscountIds.Take(1).ToList();
            var discounts = await _db.Discounts.Where(d => idsToApply.Contains(d.Id) && d.IsActive).ToListAsync();
            foreach (var d in discounts)
                discountAmount += d.Type == DiscountType.Percentage
                    ? Math.Round((subTotal - insuranceCoverage) * d.Value / 100, 2)
                    : d.Value;
        }

        var taxAmount = policy.TaxRatePercent.HasValue
            ? Math.Round((subTotal - insuranceCoverage - discountAmount) * policy.TaxRatePercent.Value / 100, 2)
            : 0;
        var totalDue = Math.Max(subTotal - insuranceCoverage - discountAmount + taxAmount, 0);

        return new BillingBreakdownResponse(subTotal, insuranceCoverage, discountAmount, taxAmount,
            totalDue, totalDue - insuranceCoverage, insuranceCoverage, lines, policy.DefaultCurrency);
    }

    public async Task<string> GenerateInvoiceNumberAsync(Guid clinicId)
    {
        var policy = await GetOrCreatePolicyAsync(clinicId);
        var number = $"{policy.InvoicePrefix}{policy.NextInvoiceNumber:D5}";
        policy.NextInvoiceNumber++;
        await _db.SaveChangesAsync();
        return number;
    }

    private async Task<ClinicBillingPolicy> GetOrCreatePolicyAsync(Guid clinicId)
    {
        var policy = await _db.ClinicBillingPolicies.FirstOrDefaultAsync(p => p.ClinicId == clinicId);
        if (policy != null) return policy;
        policy = new ClinicBillingPolicy { Id = Guid.NewGuid(), ClinicId = clinicId };
        _db.ClinicBillingPolicies.Add(policy);
        await _db.SaveChangesAsync();
        return policy;
    }

    private static ClinicInvoiceDto MapInvoice(ClinicInvoice i) =>
        new(i.Id, i.InvoiceNumber, i.PatientId, string.Empty,
            i.AppointmentId, i.TreatmentPlanId, i.Status,
            i.SubTotal, i.InsuranceCoverageAmount, i.DiscountAmount, i.TaxAmount,
            i.TotalDue, i.TotalPaid, i.TotalDue - i.TotalPaid,
            i.Currency, i.DueDate, i.Notes, i.IssuedAt, i.PaidAt, i.CreatedAt,
            i.LineItems.Select(l => new InvoiceLineItemDto(l.Id, l.Description, l.DescriptionArabic,
                l.Quantity, l.UnitPrice, l.InsuranceCoverageAmount, l.DiscountAmount, l.LineTotal, l.ServiceType)).ToList(),
            i.InstallmentPlan == null ? null : new InstallmentPlanDto(
                i.InstallmentPlan.Id, i.InstallmentPlan.TotalAmount,
                i.InstallmentPlan.NumberOfInstallments, i.InstallmentPlan.StartDate,
                i.InstallmentPlan.Notes,
                i.InstallmentPlan.Schedule.Select(s => new InstallmentScheduleDto(s.Id, s.DueDate, s.Amount, s.Status, s.PaymentId)).ToList()));
}
