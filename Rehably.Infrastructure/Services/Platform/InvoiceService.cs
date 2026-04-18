using Mapster;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Invoice;
using Rehably.Application.DTOs.Package;
using Rehably.Application.Interfaces;
using Rehably.Application.Services.Platform;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Audit;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;
using System.Text.Json;

namespace Rehably.Infrastructure.Services.Platform;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IRepository<InvoiceLineItem> _lineItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IAuditWriter _auditWriter;

    public InvoiceService(
        IInvoiceRepository invoiceRepository,
        ISubscriptionRepository subscriptionRepository,
        IRepository<InvoiceLineItem> lineItemRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IAuditWriter auditWriter)
    {
        _invoiceRepository = invoiceRepository;
        _subscriptionRepository = subscriptionRepository;
        _lineItemRepository = lineItemRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _auditWriter = auditWriter;
    }

    public async Task<Result<InvoiceDto>> GenerateInvoiceAsync(Guid subscriptionId)
    {
        var subscription = await _subscriptionRepository.GetFullSubscriptionDetailsAsync(subscriptionId);

        if (subscription == null)
            return Result<InvoiceDto>.Failure("Subscription not found");

        if (subscription.Status != SubscriptionStatus.Active && subscription.Status != SubscriptionStatus.Trial)
            return Result<InvoiceDto>.Failure("Subscription must be active or in trial to generate invoice");

        var priceSnapshot = JsonSerializer.Deserialize<PackageSnapshotDto>(subscription.PriceSnapshot);
        if (priceSnapshot == null)
            return Result<InvoiceDto>.Failure("Invalid price snapshot");

        var (periodStart, periodEnd) = CalculateBillingPeriod(subscription);
        var price = subscription.BillingCycle == BillingCycle.Yearly
            ? priceSnapshot.YearlyPrice
            : priceSnapshot.MonthlyPrice;

        var taxRate = 0m; // TODO: Tax configuration will move to appsettings
        var taxAmount = price * (taxRate / 100);
        var totalAmount = price + taxAmount;

        var invoice = new Invoice
        {
            ClinicId = subscription.ClinicId,
            SubscriptionId = subscription.Id,
            InvoiceNumber = await _invoiceRepository.GetNextInvoiceNumberAsync(),
            Amount = price,
            TaxRate = taxRate,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            BillingPeriodStart = periodStart,
            BillingPeriodEnd = periodEnd,
            DueDate = _clock.UtcNow.AddDays(7),
            LineItems = new List<InvoiceLineItem>
            {
                new InvoiceLineItem
                {
                    Description = $"{subscription.Package.Name} - {(subscription.BillingCycle == BillingCycle.Yearly ? "Yearly" : "Monthly")}",
                    ItemType = "package",
                    ReferenceId = subscription.PackageId,
                    Quantity = 1,
                    UnitPrice = price,
                    Amount = price,
                    SortOrder = 1
                },
                new InvoiceLineItem
                {
                    Description = $"Tax ({taxRate}%)",
                    ItemType = "tax",
                    ReferenceId = null,
                    Quantity = 1,
                    UnitPrice = taxAmount,
                    Amount = taxAmount,
                    SortOrder = 2
                }
            }
        };

        await _invoiceRepository.AddAsync(invoice);
        await _unitOfWork.SaveChangesAsync();

        return Result<InvoiceDto>.Success(invoice.Adapt<InvoiceDto>());
    }

    public async Task<Result<InvoiceDto>> GetInvoiceAsync(Guid invoiceId)
    {
        var invoice = await _invoiceRepository.GetWithDetailsAsync(invoiceId);

        if (invoice == null)
            return Result<InvoiceDto>.Failure("Invoice not found");

        return Result<InvoiceDto>.Success(invoice.Adapt<InvoiceDto>());
    }

    public async Task<Result<PagedResult<InvoiceDto>>> GetInvoicesAsync(
        Guid clinicId,
        int page = 1,
        int pageSize = 20,
        SubscriptionStatus? status = null)
    {
        var pagedResult = await _invoiceRepository.GetPagedByClinicAsync(clinicId, page, pageSize);
        var dtos = pagedResult.Items.Adapt<List<InvoiceDto>>();

        var result = new PagedResult<InvoiceDto>(dtos, pagedResult.TotalCount, page, pageSize);
        return Result<PagedResult<InvoiceDto>>.Success(result);
    }

    public async Task<Result<InvoiceDto>> MarkAsPaidAsync(Guid invoiceId, string paymentProvider, string transactionId)
    {
        var invoice = await _invoiceRepository.GetWithDetailsAsync(invoiceId);

        if (invoice == null)
            return Result<InvoiceDto>.Failure("Invoice not found");

        if (invoice.PaidAt != null)
            return Result<InvoiceDto>.Failure("Invoice already paid");

        invoice.PaidAt = _clock.UtcNow;
        invoice.PaidVia = paymentProvider;
        invoice.UpdatedAt = _clock.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        return Result<InvoiceDto>.Success(invoice.Adapt<InvoiceDto>());
    }

    public async Task<Result<bool>> OverdueInvoicesExistAsync(Guid clinicId)
    {
        var hasOverdue = await _invoiceRepository.HasOverdueInvoicesAsync(clinicId);
        return Result<bool>.Success(hasOverdue);
    }

    public async Task<Result<List<InvoiceDto>>> GenerateInvoicesForDueSubscriptionsAsync()
    {
        var dueSubscriptions = await _subscriptionRepository.GetDueForInvoiceGenerationAsync(7);

        var invoices = new List<InvoiceDto>();

        foreach (var subscription in dueSubscriptions)
        {
            var result = await GenerateInvoiceAsync(subscription.Id);
            if (result.IsSuccess)
            {
                invoices.Add(result.Value);
            }
        }

        return Result<List<InvoiceDto>>.Success(invoices);
    }

    private static (DateTime Start, DateTime End) CalculateBillingPeriod(Subscription subscription)
    {
        var start = subscription.EndDate.AddDays(1);
        var end = subscription.BillingCycle == BillingCycle.Yearly
            ? start.AddYears(1)
            : start.AddMonths(1);

        return (start, end);
    }

    public async Task<Result<InvoiceDto>> GenerateSubscriptionInvoiceAsync(Subscription subscription, Package package, PaymentType paymentType)
    {
        var price = subscription.BillingCycle == BillingCycle.Yearly
            ? package.YearlyPrice
            : package.MonthlyPrice;

        var taxRate = 0m; // TODO: Tax configuration will move to appsettings
        var taxAmount = price * (taxRate / 100);
        var totalAmount = price + taxAmount;

        var invoice = new Invoice
        {
            ClinicId = subscription.ClinicId,
            SubscriptionId = subscription.Id,
            InvoiceNumber = await _invoiceRepository.GetNextInvoiceNumberAsync(),
            Amount = price,
            TaxRate = taxRate,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            BillingPeriodStart = subscription.StartDate,
            BillingPeriodEnd = subscription.EndDate,
            DueDate = _clock.UtcNow.AddDays(7),
            LineItems = new List<InvoiceLineItem>
            {
                new InvoiceLineItem
                {
                    Description = $"{package.Name} - {(subscription.BillingCycle == BillingCycle.Yearly ? "Yearly" : "Monthly")}",
                    ItemType = InvoiceLineItemType.Subscription.ToString(),
                    ReferenceId = package.Id,
                    Quantity = 1,
                    UnitPrice = price,
                    Amount = price,
                    SortOrder = 1
                },
                new InvoiceLineItem
                {
                    Description = $"Tax ({taxRate}%)",
                    ItemType = "tax",
                    ReferenceId = null,
                    Quantity = 1,
                    UnitPrice = taxAmount,
                    Amount = taxAmount,
                    SortOrder = 2
                }
            }
        };

        await _invoiceRepository.AddAsync(invoice);
        await _unitOfWork.SaveChangesAsync();

        return Result<InvoiceDto>.Success(invoice.Adapt<InvoiceDto>());
    }

    public async Task<Result<InvoiceListResponseDto>> GetAllInvoicesAsync(
        Guid? clinicId,
        InvoiceStatus? status,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize)
    {
        pageSize = Math.Min(pageSize, 100);

        var (invoices, totalCount, grandTotal) = await _invoiceRepository.GetPagedForAdminAsync(
            clinicId, status, startDate, endDate, page, pageSize);

        var dtos = invoices.Select(MapToAdminDto).ToList();
        var pageTotal = dtos.Sum(d => d.TotalAmount);
        var totalRevenue = invoices
            .Where(i => i.PaidAt != null)
            .Sum(i => i.TotalAmount);

        var response = new InvoiceListResponseDto
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            PageTotal = pageTotal,
            GrandTotal = grandTotal,
            TotalRevenue = totalRevenue
        };

        return Result<InvoiceListResponseDto>.Success(response);
    }

    public async Task<Result<AdminInvoiceDto>> GetInvoiceDetailAsync(Guid invoiceId)
    {
        var invoice = await _invoiceRepository.GetWithAdminDetailsAsync(invoiceId);

        if (invoice == null)
            return Result<AdminInvoiceDto>.Failure("Invoice not found");

        return Result<AdminInvoiceDto>.Success(MapToAdminDto(invoice));
    }

    public async Task<Result<AdminInvoiceDto>> MarkInvoiceAsPaidByAdminAsync(Guid invoiceId, MarkInvoicePaidRequest request)
    {
        var invoice = await _invoiceRepository.GetWithAdminDetailsAsync(invoiceId);

        if (invoice == null)
            return Result<AdminInvoiceDto>.Failure("Invoice not found");

        if (invoice.PaidAt != null)
            return Result<AdminInvoiceDto>.Failure("Invoice already paid");

        invoice.PaidAt = _clock.UtcNow;
        invoice.PaidVia = request.PaymentMethod ?? "Manual";
        invoice.UpdatedAt = _clock.UtcNow;
        invoice.Notes = string.IsNullOrEmpty(invoice.Notes)
            ? $"Marked as paid by admin: {request.Notes}"
            : $"{invoice.Notes}\nMarked as paid by admin: {request.Notes}";

        await _unitOfWork.SaveChangesAsync();

        return Result<AdminInvoiceDto>.Success(MapToAdminDto(invoice));
    }

    public async Task<Result<bool>> DeleteInvoiceAsync(Guid id, Guid adminUserId)
    {
        var invoice = await _invoiceRepository.GetWithDetailsAsync(id);

        if (invoice == null)
            return Result<bool>.Failure("Invoice not found");

        var hasActiveTransaction = invoice.Payments.Any(p =>
            p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Processing);

        if (hasActiveTransaction)
            return Result<bool>.Failure("Cannot delete invoice with active (Pending or Processing) transactions");

        await _invoiceRepository.DeleteAsync(id);

        await _auditWriter.WriteAsync(new AuditLog
        {
            UserId = adminUserId.ToString(),
            ActionType = "Delete",
            EntityName = "Invoice",
            EntityId = id.ToString(),
            Timestamp = _clock.UtcNow
        });

        return Result<bool>.Success(true);
    }

    public async Task<Result<byte[]>> GenerateInvoicePdfAsync(Guid id)
    {
        var invoice = await _invoiceRepository.GetWithAdminDetailsAsync(id);

        if (invoice == null)
            return Result<byte[]>.Failure("Invoice not found");

        const string currency = "EGP";

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .PaddingBottom(10)
                    .Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("INVOICE").FontSize(24).Bold().FontColor(Colors.Blue.Medium);
                            col.Item().Text($"#{invoice.InvoiceNumber}").FontSize(14).FontColor(Colors.Grey.Medium);
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text($"Issue Date: {invoice.CreatedAt:dd MMM yyyy}").FontSize(10);
                            col.Item().Text($"Due Date: {invoice.DueDate:dd MMM yyyy}").FontSize(10);
                            col.Item().Text($"Period: {invoice.BillingPeriodStart:dd MMM yyyy} – {invoice.BillingPeriodEnd:dd MMM yyyy}").FontSize(10);
                        });
                    });

                page.Content()
                    .PaddingVertical(10)
                    .Column(col =>
                    {
                        col.Item().PaddingBottom(15).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Billed To:").Bold();
                                c.Item().Text(invoice.Clinic?.Name ?? string.Empty);
                                c.Item().Text(invoice.Clinic?.Email ?? string.Empty);
                                c.Item().Text(invoice.Clinic?.Phone ?? string.Empty);
                                c.Item().Text(invoice.Clinic?.Address ?? string.Empty);
                                if (!string.IsNullOrEmpty(invoice.Clinic?.Country))
                                    c.Item().Text(invoice.Clinic.Country);
                            });

                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text("Package:").Bold();
                                c.Item().Text(invoice.Subscription?.Package?.Name ?? string.Empty);
                                // VAT number would be added here if available on the Clinic entity
                            });
                        });

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Medium).Padding(5)
                                    .Text("Description").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Medium).Padding(5)
                                    .Text("Qty").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Medium).Padding(5)
                                    .Text("Unit Price").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Medium).Padding(5)
                                    .Text("Total").Bold().FontColor(Colors.White);
                            });

                            foreach (var item in invoice.LineItems.OrderBy(l => l.SortOrder))
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                    .Text(item.Description);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                    .Text(item.Quantity.ToString("0.##"));
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                    .Text($"{currency} {item.UnitPrice:N2}");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                    .Text($"{currency} {item.Amount:N2}");
                            }
                        });

                        col.Item().PaddingTop(10).AlignRight().Column(c =>
                        {
                            c.Item().Row(r =>
                            {
                                r.RelativeItem(3).AlignRight().Text("Subtotal:").Bold();
                                r.RelativeItem(1).AlignRight().Text($"{currency} {invoice.Amount:N2}");
                            });
                            c.Item().Row(r =>
                            {
                                r.RelativeItem(3).AlignRight().Text($"Tax ({invoice.TaxRate}%):").Bold();
                                r.RelativeItem(1).AlignRight().Text($"{currency} {invoice.TaxAmount:N2}");
                            });
                            if (invoice.AddOnsAmount > 0)
                            {
                                c.Item().Row(r =>
                                {
                                    r.RelativeItem(3).AlignRight().Text("Add-ons:").Bold();
                                    r.RelativeItem(1).AlignRight().Text($"{currency} {invoice.AddOnsAmount:N2}");
                                });
                            }
                            c.Item().BorderTop(2).BorderColor(Colors.Blue.Medium).PaddingTop(5).Row(r =>
                            {
                                r.RelativeItem(3).AlignRight().Text("Total:").Bold().FontSize(13);
                                r.RelativeItem(1).AlignRight().Text($"{currency} {invoice.TotalAmount:N2}").Bold().FontSize(13);
                            });
                        });

                        if (invoice.PaidAt.HasValue)
                        {
                            col.Item().PaddingTop(20)
                                .Background(Colors.Green.Lighten4)
                                .Padding(10)
                                .Text($"PAID on {invoice.PaidAt.Value:dd MMM yyyy} via {invoice.PaidVia ?? "N/A"}")
                                .Bold().FontColor(Colors.Green.Darken2);
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Page ").FontSize(9).FontColor(Colors.Grey.Medium);
                        text.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                        text.Span(" of ").FontSize(9).FontColor(Colors.Grey.Medium);
                        text.TotalPages().FontSize(9).FontColor(Colors.Grey.Medium);
                    });
            });
        }).GeneratePdf();

        return Result<byte[]>.Success(pdfBytes);
    }

    private static AdminInvoiceDto MapToAdminDto(Invoice invoice)
    {
        return new AdminInvoiceDto
        {
            Id = invoice.Id,
            ClinicId = invoice.ClinicId,
            ClinicName = invoice.Clinic?.Name ?? string.Empty,
            ClinicEmail = invoice.Clinic?.Email ?? string.Empty,
            ClinicPhone = invoice.Clinic?.Phone ?? string.Empty,
            ClinicVatNumber = null,
            ClinicAddress = invoice.Clinic?.Address ?? string.Empty,
            TaxIdentificationNumber = null,
            ClinicCountry = invoice.Clinic?.Country ?? string.Empty,
            PackageId = invoice.Subscription?.PackageId ?? Guid.Empty,
            PackageName = invoice.Subscription?.Package?.Name ?? string.Empty,
            InvoiceNumber = invoice.InvoiceNumber,
            Amount = invoice.Amount,
            TaxRate = invoice.TaxRate,
            TaxAmount = invoice.TaxAmount,
            AddOnsAmount = invoice.AddOnsAmount,
            TotalAmount = invoice.TotalAmount,
            BillingPeriodStart = invoice.BillingPeriodStart,
            BillingPeriodEnd = invoice.BillingPeriodEnd,
            DueDate = invoice.DueDate,
            PaidAt = invoice.PaidAt,
            PaidVia = invoice.PaidVia,
            PaymentStatus = invoice.PaidAt.HasValue ? "Paid" : "Pending",
            TransactionType = invoice.Subscription?.PaymentType.ToString() ?? string.Empty,
            LineItems = invoice.LineItems.Adapt<List<InvoiceLineItemDto>>()
        };
    }
}
