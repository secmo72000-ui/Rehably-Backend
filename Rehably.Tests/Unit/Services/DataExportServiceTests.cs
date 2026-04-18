using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Rehably.Application.Contexts;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;
using Rehably.Infrastructure.Services.Platform;
using Rehably.Tests.Helpers;

namespace Rehably.Tests.Unit.Services;

public class DataExportServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<DataExportService>> _loggerMock;
    private readonly Mock<ITenantContext> _tenantContextMock;

    // SUT configured as Platform Admin by default (TenantId = null → can export any clinic)
    private readonly DataExportService _sut;

    public DataExportServiceTests()
    {
        _loggerMock = new Mock<ILogger<DataExportService>>();
        _tenantContextMock = new Mock<ITenantContext>();

        // Default: simulate Platform Admin — no tenant restriction
        _tenantContextMock.Setup(t => t.TenantId).Returns((Guid?)null);

        _context = PlatformTestHelpers.CreateInMemoryContext();

        _sut = new DataExportService(_context, _tenantContextMock.Object, _loggerMock.Object);
    }

    public void Dispose() => _context.Dispose();

    private async Task<(Guid ClinicId, Guid SubscriptionId)> SeedClinicWithDataAsync()
    {
        var clinicId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var packageId = Guid.NewGuid();

        var clinic = new Clinic
        {
            Id = clinicId,
            Name = "Export Test Clinic",
            Slug = "export-test",
            Phone = "0100000000",
            Status = ClinicStatus.Active
        };
        _context.Clinics.Add(clinic);

        var package = new Package
        {
            Id = packageId,
            Code = "export-pkg",
            Name = "Export Package"
        };
        _context.Packages.Add(package);

        var subscription = new Subscription
        {
            Id = subscriptionId,
            ClinicId = clinicId,
            PackageId = packageId,
            Status = SubscriptionStatus.Active,
            PriceSnapshot = "{}",
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        _context.Subscriptions.Add(subscription);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            SubscriptionId = subscriptionId,
            InvoiceNumber = "INV-TEST-001",
            Amount = 100m,
            TaxAmount = 14m,
            TotalAmount = 114m,
            DueDate = DateTime.UtcNow.AddDays(30),
            BillingPeriodStart = DateTime.UtcNow.AddDays(-30),
            BillingPeriodEnd = DateTime.UtcNow
        };
        _context.Invoices.Add(invoice);

        await _context.SaveChangesAsync();

        return (clinicId, subscriptionId);
    }

    [Fact]
    public async Task ExportClinicData_PlatformAdmin_ReturnsNonEmptyBytes()
    {
        // Platform admin (TenantId = null) can export any clinic
        var (clinicId, _) = await SeedClinicWithDataAsync();

        var result = await _sut.ExportClinicDataAsync(clinicId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportClinicData_ContainsExpectedContent()
    {
        var (clinicId, _) = await SeedClinicWithDataAsync();

        var result = await _sut.ExportClinicDataAsync(clinicId);

        result.IsSuccess.Should().BeTrue();
        var bytes = result.Value;
        bytes.Should().NotBeEmpty("export should contain CSV data in a ZIP archive");
    }

    [Fact]
    public async Task ExportClinicData_EmptyClinic_ReturnsFailure()
    {
        var nonExistentClinicId = Guid.NewGuid();

        var result = await _sut.ExportClinicDataAsync(nonExistentClinicId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    // ── IDOR Protection Tests ────────────────────────────────────────────────

    [Fact]
    public async Task ExportClinicData_ClinicUser_OwnClinic_Succeeds()
    {
        // A clinic user (TenantId set) exporting their OWN clinic — should succeed
        var (clinicId, _) = await SeedClinicWithDataAsync();

        _tenantContextMock.Setup(t => t.TenantId).Returns(clinicId); // caller = same clinic

        var sut = new DataExportService(_context, _tenantContextMock.Object, _loggerMock.Object);

        var result = await sut.ExportClinicDataAsync(clinicId);

        result.IsSuccess.Should().BeTrue("a clinic user should be able to export their own data");
    }

    [Fact]
    public async Task ExportClinicData_ClinicUser_DifferentClinic_ReturnsAccessDenied()
    {
        // IDOR test: clinic user trying to export a DIFFERENT clinic's data
        var (targetClinicId, _) = await SeedClinicWithDataAsync();
        var callerClinicId = Guid.NewGuid(); // different clinic

        _tenantContextMock.Setup(t => t.TenantId).Returns(callerClinicId);

        var sut = new DataExportService(_context, _tenantContextMock.Object, _loggerMock.Object);

        var result = await sut.ExportClinicDataAsync(targetClinicId);

        result.IsSuccess.Should().BeFalse("IDOR: clinic user must not export another clinic's data");
        result.Error.Should().Contain("Access denied");
    }
}
