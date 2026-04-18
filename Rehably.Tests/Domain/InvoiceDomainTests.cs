using FluentAssertions;
using Rehably.Domain.Entities.Platform;
using Xunit;

namespace Rehably.Tests.Domain;

public class InvoiceDomainTests
{
    private static Invoice CreateUnpaidInvoice(DateTime dueDate) => new Invoice
    {
        ClinicId = Guid.NewGuid(),
        SubscriptionId = Guid.NewGuid(),
        InvoiceNumber = "INV-0001",
        Amount = 299.00m,
        TaxRate = 14.00m,
        TaxAmount = 41.86m,
        TotalAmount = 340.86m,
        BillingPeriodStart = dueDate.AddMonths(-1),
        BillingPeriodEnd = dueDate,
        DueDate = dueDate,
        PaidAt = null
    };

    #region CanBePaid

    [Fact]
    public void CanBePaid_WhenUnpaidAndDueDateIsInFuture_ShouldReturnTrue()
    {
        var invoice = CreateUnpaidInvoice(DateTime.UtcNow.AddDays(7));

        invoice.CanBePaid().Should().BeTrue();
    }

    [Fact]
    public void CanBePaid_WhenAlreadyPaid_ShouldReturnFalse()
    {
        var invoice = CreateUnpaidInvoice(DateTime.UtcNow.AddDays(7));
        invoice.PaidAt = DateTime.UtcNow.AddDays(-1);

        invoice.CanBePaid().Should().BeFalse();
    }

    [Fact]
    public void CanBePaid_WhenUnpaidButDueDateIsInPast_ShouldReturnFalse()
    {
        var invoice = CreateUnpaidInvoice(DateTime.UtcNow.AddDays(-3));

        invoice.CanBePaid().Should().BeFalse();
    }

    #endregion

    #region IsOverdue

    [Fact]
    public void IsOverdue_WhenUnpaidAndDueDateIsInPast_ShouldReturnTrue()
    {
        var invoice = CreateUnpaidInvoice(DateTime.UtcNow.AddDays(-3));

        invoice.IsOverdue().Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_WhenPaidAndDueDateIsInPast_ShouldReturnFalse()
    {
        var invoice = CreateUnpaidInvoice(DateTime.UtcNow.AddDays(-3));
        invoice.PaidAt = DateTime.UtcNow.AddDays(-4);

        invoice.IsOverdue().Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_WhenUnpaidAndDueDateIsInFuture_ShouldReturnFalse()
    {
        var invoice = CreateUnpaidInvoice(DateTime.UtcNow.AddDays(7));

        invoice.IsOverdue().Should().BeFalse();
    }

    #endregion
}
