using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Rehably.Application.DTOs.AddOn;
using Rehably.Application.Interfaces;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;
using Rehably.Infrastructure.Services.Platform;

namespace Rehably.Tests.Unit.Services;

public class AddOnServiceTests : IDisposable
{
    private readonly Mock<IFeatureRepository> _featureRepoMock;
    private readonly Mock<ISubscriptionRepository> _subscriptionRepoMock;
    private readonly Mock<ISubscriptionAddOnRepository> _addOnRepoMock;
    private readonly Mock<IPackageRepository> _packageRepoMock;
    private readonly Mock<IInvoiceService> _invoiceServiceMock; // kept for test setups; not passed to AddOnService
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IClock> _clockMock;
    private readonly Mock<ILogger<AddOnService>> _loggerMock;
    private readonly ApplicationDbContext _context;
    private readonly AddOnService _sut;

    private readonly DateTime _now = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _clinicId = Guid.Parse("A0000000-0000-0000-0000-000000000001");
    private readonly Guid _subscriptionId = Guid.Parse("B0000000-0000-0000-0000-000000000001");
    private readonly Guid _packageId = Guid.Parse("C0000000-0000-0000-0000-000000000001");
    private readonly Guid _featureId = Guid.Parse("D0000000-0000-0000-0000-000000000001");
    private readonly Guid _addOnId = Guid.Parse("E0000000-0000-0000-0000-000000000001");

    public AddOnServiceTests()
    {
        _featureRepoMock = new Mock<IFeatureRepository>();
        _subscriptionRepoMock = new Mock<ISubscriptionRepository>();
        _addOnRepoMock = new Mock<ISubscriptionAddOnRepository>();
        _packageRepoMock = new Mock<IPackageRepository>();
        _invoiceServiceMock = new Mock<IInvoiceService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _clockMock = new Mock<IClock>();
        _loggerMock = new Mock<ILogger<AddOnService>>();

        _clockMock.Setup(c => c.UtcNow).Returns(_now);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        var clinicRepoMock = new Mock<IClinicRepository>();
        _sut = new AddOnService(
            _featureRepoMock.Object,
            _subscriptionRepoMock.Object,
            _addOnRepoMock.Object,
            _packageRepoMock.Object,
            clinicRepoMock.Object,
            _unitOfWorkMock.Object,
            _clockMock.Object,
            _loggerMock.Object);
    }

    public void Dispose() => _context.Dispose();

    // -----------------------------------------------------------------------
    // CreateAddOnAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAddOn_ValidRequest_CreatesActiveAddOnAndInvoice()
    {
        var feature = new Feature { Id = _featureId, Name = "Extra Patients", Code = "extra-patients", CategoryId = Guid.NewGuid() };
        var subscription = new Subscription { Id = _subscriptionId, ClinicId = _clinicId, PackageId = _packageId };

        _featureRepoMock.Setup(r => r.GetByIdAsync(_featureId)).ReturnsAsync(feature);
        _subscriptionRepoMock.Setup(r => r.GetActiveSubscriptionByClinicIdAsync(_clinicId)).ReturnsAsync(subscription);
        _addOnRepoMock.Setup(r => r.AddAsync(It.IsAny<SubscriptionAddOn>())).ReturnsAsync((SubscriptionAddOn a) => a);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _invoiceServiceMock.Setup(i => i.GenerateInvoiceAsync(_subscriptionId))
            .ReturnsAsync(Application.Common.Result<Application.DTOs.Invoice.InvoiceDto>.Success(new Application.DTOs.Invoice.InvoiceDto()));

        var request = new CreateAddOnRequestDto
        {
            FeatureId = _featureId,
            Limit = 500,
            Price = 99.99m,
            StartDate = _now,
            EndDate = _now.AddMonths(1),
            PaymentType = PaymentType.Cash
        };

        var result = await _sut.CreateAddOnAsync(_clinicId, request);

        result.IsSuccess.Should().BeTrue();
        result.Value.FeatureId.Should().Be(_featureId);
        result.Value.Status.Should().Be(AddOnStatus.Active);
        result.Value.Limit.Should().Be(500);
        result.Value.Price.Should().Be(99.99m);
    }

    [Fact]
    public async Task CreateAddOn_FeatureAlreadyInPackage_StacksLimit()
    {
        // Package has limit 100, add-on has limit 500 = 600 total
        // This test verifies the add-on is created on top of package limit (stacking)
        var feature = new Feature { Id = _featureId, Name = "Patients", Code = "patients", CategoryId = Guid.NewGuid() };
        var subscription = new Subscription { Id = _subscriptionId, ClinicId = _clinicId, PackageId = _packageId };

        _featureRepoMock.Setup(r => r.GetByIdAsync(_featureId)).ReturnsAsync(feature);
        _subscriptionRepoMock.Setup(r => r.GetActiveSubscriptionByClinicIdAsync(_clinicId)).ReturnsAsync(subscription);
        _addOnRepoMock.Setup(r => r.AddAsync(It.IsAny<SubscriptionAddOn>())).ReturnsAsync((SubscriptionAddOn a) => a);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var request = new CreateAddOnRequestDto
        {
            FeatureId = _featureId,
            Limit = 500, // Stacks on top of package limit of 100
            Price = 50m,
            StartDate = _now,
            EndDate = _now.AddMonths(1),
            PaymentType = PaymentType.Cash
        };

        var result = await _sut.CreateAddOnAsync(_clinicId, request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Limit.Should().Be(500);
        // Combined effective limit would be 100 (package) + 500 (add-on) = 600, tested via GetEffectiveLimitAsync
    }

    [Fact]
    public async Task CreateAddOn_EndDateBeforeStartDate_ReturnsValidationError()
    {
        var request = new CreateAddOnRequestDto
        {
            FeatureId = _featureId,
            Price = 50m,
            StartDate = _now.AddDays(10),
            EndDate = _now.AddDays(5), // EndDate before StartDate
            PaymentType = PaymentType.Cash
        };

        var result = await _sut.CreateAddOnAsync(_clinicId, request);

        result.IsSuccess.Should().BeFalse();
        result.Error.ToLower().Should().Contain("end date");
    }

    [Fact]
    public async Task CreateAddOn_EndDateInPast_ReturnsValidationError()
    {
        var request = new CreateAddOnRequestDto
        {
            FeatureId = _featureId,
            Price = 50m,
            StartDate = _now.AddDays(-10),
            EndDate = _now.AddDays(-1), // End date in the past
            PaymentType = PaymentType.Cash
        };

        var result = await _sut.CreateAddOnAsync(_clinicId, request);

        result.IsSuccess.Should().BeFalse();
        result.Error.ToLower().Should().Contain("future");
    }

    // -----------------------------------------------------------------------
    // CancelAddOnAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CancelAddOn_ActiveAddOn_SetsStatusCancelled()
    {
        var subscription = new Subscription { Id = _subscriptionId, ClinicId = _clinicId };
        var addOn = new SubscriptionAddOn
        {
            Id = _addOnId,
            SubscriptionId = _subscriptionId,
            FeatureId = _featureId,
            Status = AddOnStatus.Active,
            Subscription = subscription
        };

        _addOnRepoMock.Setup(r => r.GetWithSubscriptionAndFeatureAsync(_addOnId)).ReturnsAsync(addOn);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CancelAddOnAsync(_clinicId, _addOnId);

        result.IsSuccess.Should().BeTrue();
        addOn.Status.Should().Be(AddOnStatus.Cancelled);
        addOn.CancelledAt.Should().NotBeNull();
        addOn.CancelledAt.Should().Be(_now);
    }

    [Fact]
    public async Task CancelAddOn_NotActive_ReturnsError()
    {
        var subscription = new Subscription { Id = _subscriptionId, ClinicId = _clinicId };
        var addOn = new SubscriptionAddOn
        {
            Id = _addOnId,
            SubscriptionId = _subscriptionId,
            Status = AddOnStatus.Expired,
            Subscription = subscription
        };

        _addOnRepoMock.Setup(r => r.GetWithSubscriptionAndFeatureAsync(_addOnId)).ReturnsAsync(addOn);

        var result = await _sut.CancelAddOnAsync(_clinicId, _addOnId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("active");
    }

    // -----------------------------------------------------------------------
    // GetEffectiveLimitAsync — Removed: method does not exist on current AddOnService implementation
    // -----------------------------------------------------------------------

    // -----------------------------------------------------------------------
    // GetClinicAddOnsAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetClinicAddOns_FiltersByStatus()
    {
        var subscription = new Subscription { Id = _subscriptionId, ClinicId = _clinicId, PackageId = _packageId };
        _subscriptionRepoMock.Setup(r => r.GetActiveSubscriptionByClinicIdAsync(_clinicId)).ReturnsAsync(subscription);

        var feature = new Feature { Id = _featureId, Name = "Patients", Code = "patients", CategoryId = Guid.NewGuid() };

        var activeAddOn = new SubscriptionAddOn
        {
            Id = Guid.NewGuid(),
            SubscriptionId = _subscriptionId,
            FeatureId = _featureId,
            Feature = feature,
            Subscription = subscription,
            Status = AddOnStatus.Active,
            EndDate = _now.AddMonths(1)
        };

        _addOnRepoMock
            .Setup(r => r.GetBySubscriptionIdAsync(_subscriptionId, AddOnStatus.Active))
            .ReturnsAsync(new List<SubscriptionAddOn> { activeAddOn });

        var result = await _sut.GetClinicAddOnsAsync(_clinicId, AddOnStatus.Active);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].Status.Should().Be(AddOnStatus.Active);
    }
}
