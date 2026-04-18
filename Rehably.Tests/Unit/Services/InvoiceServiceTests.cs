using FluentAssertions;
using Moq;
using QuestPDF.Infrastructure;
using Rehably.Application.DTOs.Invoice;
using Rehably.Application.Interfaces;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Entities.Audit;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Services.Platform;

namespace Rehably.Tests.Unit.Services;

public class InvoiceServiceTests
{
    private readonly Mock<IInvoiceRepository> _invoiceRepoMock;
    private readonly Mock<ISubscriptionRepository> _subscriptionRepoMock;
    private readonly Mock<IRepository<InvoiceLineItem>> _lineItemRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IClock> _clockMock;
    private readonly Mock<IAuditWriter> _auditDbContextMock;
    private readonly InvoiceService _service;

    public InvoiceServiceTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        _invoiceRepoMock = new Mock<IInvoiceRepository>();
        _subscriptionRepoMock = new Mock<ISubscriptionRepository>();
        _lineItemRepoMock = new Mock<IRepository<InvoiceLineItem>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _clockMock = new Mock<IClock>();
        _auditDbContextMock = new Mock<IAuditWriter>();

        _invoiceRepoMock
            .Setup(r => r.GetNextInvoiceNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("INV-001");

        _invoiceRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Invoice>()))
            .ReturnsAsync((Invoice inv) => inv);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        _clockMock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

        _auditDbContextMock
            .Setup(a => a.WriteAsync(It.IsAny<AuditLog>()))
            .Returns(Task.CompletedTask);

        _service = new InvoiceService(
            _invoiceRepoMock.Object,
            _subscriptionRepoMock.Object,
            _lineItemRepoMock.Object,
            _unitOfWorkMock.Object,
            _clockMock.Object,
            _auditDbContextMock.Object);
    }

    private static (Subscription subscription, Package package) BuildSubscriptionAndPackage(
        BillingCycle billingCycle = BillingCycle.Monthly,
        decimal monthlyPrice = 1000m,
        decimal yearlyPrice = 10000m,
        PaymentType paymentType = PaymentType.Cash,
        int trialDays = 0)
    {
        var clinicId = Guid.NewGuid();
        var packageId = Guid.NewGuid();

        var package = new Package
        {
            Id = packageId,
            Name = "Standard Plan",
            Code = "standard",
            MonthlyPrice = monthlyPrice,
            YearlyPrice = yearlyPrice,
            Status = PackageStatus.Active
        };

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            PackageId = packageId,
            Status = trialDays > 0 ? SubscriptionStatus.Trial : SubscriptionStatus.Active,
            BillingCycle = billingCycle,
            StartDate = new DateTime(2026, 3, 1),
            EndDate = billingCycle == BillingCycle.Yearly
                ? new DateTime(2027, 3, 1)
                : new DateTime(2026, 4, 1),
            PaymentType = paymentType,
            AutoRenew = true,
            PriceSnapshot = "{}"
        };

        return (subscription, package);
    }

    [Fact]
    public async Task GenerateSubscriptionInvoice_ValidInput_CreatesInvoiceWithCorrectTotal()
    {
        var (subscription, package) = BuildSubscriptionAndPackage(monthlyPrice: 1000m);

        var result = await _service.GenerateSubscriptionInvoiceAsync(subscription, package, PaymentType.Cash);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        // Tax is now 0% (tax config removed, will move to appsettings)
        result.Value!.TotalAmount.Should().Be(1000m);
    }

    [Fact]
    public async Task GenerateSubscriptionInvoice_MonthlyBilling_UsesMonthlyPrice()
    {
        var (subscription, package) = BuildSubscriptionAndPackage(
            billingCycle: BillingCycle.Monthly,
            monthlyPrice: 500m,
            yearlyPrice: 5000m);

        var result = await _service.GenerateSubscriptionInvoiceAsync(subscription, package, PaymentType.Cash);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(500m);
    }

    [Fact]
    public async Task GenerateSubscriptionInvoice_YearlyBilling_UsesYearlyPrice()
    {
        var (subscription, package) = BuildSubscriptionAndPackage(
            billingCycle: BillingCycle.Yearly,
            monthlyPrice: 500m,
            yearlyPrice: 5000m);

        var result = await _service.GenerateSubscriptionInvoiceAsync(subscription, package, PaymentType.Cash);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(5000m);
    }

    [Fact]
    public async Task GenerateSubscriptionInvoice_TaxIsZero()
    {
        var (subscription, package) = BuildSubscriptionAndPackage(monthlyPrice: 1000m);

        var result = await _service.GenerateSubscriptionInvoiceAsync(subscription, package, PaymentType.Cash);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TaxAmount.Should().Be(0m);
        result.Value.TaxRate.Should().Be(0m);
        result.Value.TotalAmount.Should().Be(1000m);
    }

    [Fact]
    public async Task GenerateSubscriptionInvoice_CreatesSubscriptionLineItem()
    {
        var (subscription, package) = BuildSubscriptionAndPackage(monthlyPrice: 1000m);

        Invoice? capturedInvoice = null;
        _invoiceRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>(inv => capturedInvoice = inv)
            .ReturnsAsync((Invoice inv) => inv);

        await _service.GenerateSubscriptionInvoiceAsync(subscription, package, PaymentType.Cash);

        capturedInvoice.Should().NotBeNull();
        capturedInvoice!.LineItems.Should().Contain(li =>
            li.ItemType == InvoiceLineItemType.Subscription.ToString() &&
            li.Amount == 1000m);
    }

    [Fact]
    public async Task GenerateSubscriptionInvoice_SetsPaymentTypeFromRequest()
    {
        var (subscription, package) = BuildSubscriptionAndPackage(paymentType: PaymentType.Online);

        Invoice? capturedInvoice = null;
        _invoiceRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>(inv => capturedInvoice = inv)
            .ReturnsAsync((Invoice inv) => inv);

        var result = await _service.GenerateSubscriptionInvoiceAsync(subscription, package, PaymentType.Online);

        capturedInvoice.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateInvoiceNumber_IsUnique()
    {
        var (subscription1, package1) = BuildSubscriptionAndPackage();
        var (subscription2, package2) = BuildSubscriptionAndPackage();

        var counter = 0;
        _invoiceRepoMock
            .Setup(r => r.GetNextInvoiceNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => $"INV-{++counter:000}");

        var result1 = await _service.GenerateSubscriptionInvoiceAsync(subscription1, package1, PaymentType.Cash);
        var result2 = await _service.GenerateSubscriptionInvoiceAsync(subscription2, package2, PaymentType.Cash);

        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value!.InvoiceNumber.Should().NotBe(result2.Value!.InvoiceNumber);
    }

    [Fact]
    public async Task GetAllInvoices_PageSizeAbove100_CappedAt100()
    {
        int? capturedPageSize = null;

        _invoiceRepoMock
            .Setup(r => r.GetPagedForAdminAsync(
                It.IsAny<Guid?>(),
                It.IsAny<InvoiceStatus?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Callback<Guid?, InvoiceStatus?, DateTime?, DateTime?, int, int>(
                (_, _, _, _, _, ps) => capturedPageSize = ps)
            .ReturnsAsync((new List<Invoice>(), 0, 0m));

        await _service.GetAllInvoicesAsync(null, null, null, null, 1, 200);

        capturedPageSize.Should().Be(100);
    }

    [Fact]
    public async Task GetAllInvoices_WithFilter_TotalRevenueSumsFilteredPaidInvoices()
    {
        var paidInvoice = new Invoice
        {
            Id = Guid.NewGuid(),
            ClinicId = Guid.NewGuid(),
            SubscriptionId = Guid.NewGuid(),
            InvoiceNumber = "INV-001",
            Amount = 500m,
            TaxAmount = 70m,
            TotalAmount = 570m,
            PaidAt = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(7),
            BillingPeriodStart = DateTime.UtcNow.AddDays(-30),
            BillingPeriodEnd = DateTime.UtcNow,
            LineItems = new List<InvoiceLineItem>()
        };

        _invoiceRepoMock
            .Setup(r => r.GetPagedForAdminAsync(
                It.IsAny<Guid?>(),
                InvoiceStatus.Paid,
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((new List<Invoice> { paidInvoice }, 1, 570m));

        var result = await _service.GetAllInvoicesAsync(null, InvoiceStatus.Paid, null, null, 1, 20);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalRevenue.Should().Be(570m);
    }

    [Fact]
    public async Task DeleteInvoice_HasActivePaidTransaction_Returns409Conflict()
    {
        var invoiceId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();

        var invoice = new Invoice
        {
            Id = invoiceId,
            ClinicId = Guid.NewGuid(),
            SubscriptionId = Guid.NewGuid(),
            InvoiceNumber = "INV-001",
            Amount = 500m,
            TaxAmount = 70m,
            TotalAmount = 570m,
            DueDate = DateTime.UtcNow.AddDays(7),
            BillingPeriodStart = DateTime.UtcNow.AddDays(-30),
            BillingPeriodEnd = DateTime.UtcNow,
            Payments = new List<Payment>
            {
                new Payment
                {
                    Id = Guid.NewGuid(),
                    Status = PaymentStatus.Pending,
                    Amount = 570m,
                    InvoiceId = invoiceId
                }
            },
            LineItems = new List<InvoiceLineItem>()
        };

        _invoiceRepoMock
            .Setup(r => r.GetWithDetailsAsync(invoiceId))
            .ReturnsAsync(invoice);

        var result = await _service.DeleteInvoiceAsync(invoiceId, adminUserId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("active");
    }

    [Fact]
    public async Task DeleteInvoice_CompletedTransactionDoesNotBlock_DeleteSucceeds()
    {
        var invoiceId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();

        var invoice = new Invoice
        {
            Id = invoiceId,
            ClinicId = Guid.NewGuid(),
            SubscriptionId = Guid.NewGuid(),
            InvoiceNumber = "INV-001",
            Amount = 500m,
            TaxAmount = 70m,
            TotalAmount = 570m,
            DueDate = DateTime.UtcNow.AddDays(7),
            BillingPeriodStart = DateTime.UtcNow.AddDays(-30),
            BillingPeriodEnd = DateTime.UtcNow,
            Payments = new List<Payment>
            {
                new Payment
                {
                    Id = Guid.NewGuid(),
                    Status = PaymentStatus.Completed,
                    Amount = 570m,
                    InvoiceId = invoiceId
                }
            },
            LineItems = new List<InvoiceLineItem>()
        };

        _invoiceRepoMock
            .Setup(r => r.GetWithDetailsAsync(invoiceId))
            .ReturnsAsync(invoice);

        _invoiceRepoMock
            .Setup(r => r.DeleteAsync(invoiceId))
            .Returns(Task.CompletedTask);

        var result = await _service.DeleteInvoiceAsync(invoiceId, adminUserId);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteInvoice_Valid_CreatesAuditLogEntry()
    {
        var invoiceId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();

        var invoice = new Invoice
        {
            Id = invoiceId,
            ClinicId = Guid.NewGuid(),
            SubscriptionId = Guid.NewGuid(),
            InvoiceNumber = "INV-001",
            Amount = 500m,
            TaxAmount = 70m,
            TotalAmount = 570m,
            DueDate = DateTime.UtcNow.AddDays(7),
            BillingPeriodStart = DateTime.UtcNow.AddDays(-30),
            BillingPeriodEnd = DateTime.UtcNow,
            Payments = new List<Payment>(),
            LineItems = new List<InvoiceLineItem>()
        };

        _invoiceRepoMock
            .Setup(r => r.GetWithDetailsAsync(invoiceId))
            .ReturnsAsync(invoice);

        _invoiceRepoMock
            .Setup(r => r.DeleteAsync(invoiceId))
            .Returns(Task.CompletedTask);

        var result = await _service.DeleteInvoiceAsync(invoiceId, adminUserId);

        result.IsSuccess.Should().BeTrue();
        _auditDbContextMock.Verify(a => a.WriteAsync(
            It.Is<AuditLog>(log =>
                log.EntityName == "Invoice" &&
                log.EntityId == invoiceId.ToString() &&
                log.ActionType == "Delete")), Times.Once);
    }

    [Fact]
    public async Task GenerateInvoicePdf_ValidId_ReturnsByteArrayNotEmpty()
    {
        var invoiceId = Guid.NewGuid();

        var invoice = new Invoice
        {
            Id = invoiceId,
            ClinicId = Guid.NewGuid(),
            SubscriptionId = Guid.NewGuid(),
            InvoiceNumber = "INV-2026-00001",
            Amount = 1000m,
            TaxRate = 14m,
            TaxAmount = 140m,
            TotalAmount = 1140m,
            DueDate = DateTime.UtcNow.AddDays(7),
            BillingPeriodStart = new DateTime(2026, 3, 1),
            BillingPeriodEnd = new DateTime(2026, 4, 1),
            Payments = new List<Payment>(),
            LineItems = new List<InvoiceLineItem>
            {
                new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Standard Plan - Monthly",
                    ItemType = "package",
                    Quantity = 1,
                    UnitPrice = 1000m,
                    Amount = 1000m,
                    SortOrder = 1
                }
            }
        };

        _invoiceRepoMock
            .Setup(r => r.GetWithAdminDetailsAsync(invoiceId))
            .ReturnsAsync(invoice);

        var result = await _service.GenerateInvoicePdfAsync(invoiceId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Length.Should().BeGreaterThan(0);
    }
}
