using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Invoice;
using Rehably.Application.DTOs.Subscription;
using Rehably.Application.DTOs.Package;
using Rehably.Application.Interfaces;
using Rehably.Application.Services.Platform;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;
using Rehably.Infrastructure.Repositories;
using Rehably.Infrastructure.Services.Platform;
using Rehably.Tests.Helpers;
using FluentAssertions;

namespace Rehably.Tests.Services.Platform;

public class PlatformSubscriptionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPricingService> _pricingServiceMock;
    private readonly PlatformSubscriptionService _sut;

    // Test IDs - using consistent Guids for tests
    private readonly Guid _feature1Id = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private readonly Guid _feature2Id = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private readonly Guid _categoryId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private readonly Guid _package1Id = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private readonly Guid _package2Id = Guid.Parse("30000000-0000-0000-0000-000000000002");
    private readonly Guid _clinic1Id = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private readonly Guid _clinic2Id = Guid.Parse("40000000-0000-0000-0000-000000000002");
    private readonly Guid _subscription1Id = Guid.Parse("50000000-0000-0000-0000-000000000001");
    private readonly Guid _subscription2Id = Guid.Parse("50000000-0000-0000-0000-000000000002");
    private readonly Guid _subscription3Id = Guid.Parse("50000000-0000-0000-0000-000000000003");
    private readonly Guid _usage1Id = Guid.Parse("60000000-0000-0000-0000-000000000001");
    private readonly Guid _usage2Id = Guid.Parse("60000000-0000-0000-0000-000000000002");
    private readonly Guid _packageFeature1Id = Guid.Parse("70000000-0000-0000-0000-000000000001");
    private readonly Guid _packageFeature2Id = Guid.Parse("70000000-0000-0000-0000-000000000002");
    private readonly Guid _packageFeature3Id = Guid.Parse("70000000-0000-0000-0000-000000000003");
    private readonly Guid _packageFeature4Id = Guid.Parse("70000000-0000-0000-0000-000000000004");

    public PlatformSubscriptionServiceTests()
    {
        _context = PlatformTestHelpers.CreateInMemoryContext();

        // Seed features
        var features = new List<Feature>
        {
            PlatformTestHelpers.CreateTestFeature(_feature1Id, "users", "User Seats", PricingType.PerUser, categoryId: _categoryId),
            PlatformTestHelpers.CreateTestFeature(_feature2Id, "storage", "Storage", PricingType.PerStorageGB, categoryId: _categoryId)
        };
        _context.Features.AddRange(features);

        // Seed packages
        _context.Packages.AddRange(
            PlatformTestHelpers.CreateTestPackage(_package1Id, "basic", "Basic Package", PackageStatus.Active, monthlyPrice: 100m, yearlyPrice: 1000m, trialDays: 14),
            PlatformTestHelpers.CreateTestPackage(_package2Id, "premium", "Premium Package", PackageStatus.Active, monthlyPrice: 500m, yearlyPrice: 5000m, trialDays: 30)
        );

        // Seed package features with Feature navigation property
        _context.PackageFeatures.AddRange(
            new PackageFeature { Id = _packageFeature1Id, PackageId = _package1Id, FeatureId = _feature1Id, IsIncluded = true, Feature = features[0], CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new PackageFeature { Id = _packageFeature2Id, PackageId = _package1Id, FeatureId = _feature2Id, IsIncluded = true, Feature = features[1], CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new PackageFeature { Id = _packageFeature3Id, PackageId = _package2Id, FeatureId = _feature1Id, IsIncluded = true, Feature = features[0], CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new PackageFeature { Id = _packageFeature4Id, PackageId = _package2Id, FeatureId = _feature2Id, IsIncluded = true, Feature = features[1], CreatedAt = DateTime.UtcNow.AddDays(-5) }
        );

        // Seed clinics
        _context.Clinics.AddRange(
            new Clinic { Id = _clinic1Id, Name = "Test Clinic", IsDeleted = false },
            new Clinic { Id = _clinic2Id, Name = "Another Clinic", IsDeleted = false }
        );

        _context.SaveChanges();

        // Seed subscriptions
        _context.Subscriptions.AddRange(
            PlatformTestHelpers.CreateTestSubscription(_subscription1Id, _clinic1Id, _package1Id, SubscriptionStatus.Active, priceSnapshot: "{\"BasePrice\":100}"),
            PlatformTestHelpers.CreateTestSubscription(_subscription2Id, _clinic2Id, _package2Id, SubscriptionStatus.Trial, priceSnapshot: "{\"BasePrice\":500}"),
            PlatformTestHelpers.CreateTestSubscription(_subscription3Id, _clinic1Id, _package1Id, SubscriptionStatus.Cancelled, priceSnapshot: "{}")
        );

        // Seed usage records with Feature navigation property
        _context.SubscriptionFeatureUsages.AddRange(
            new SubscriptionFeatureUsage { Id = _usage1Id, SubscriptionId = _subscription1Id, FeatureId = _feature1Id, Used = 3, Limit = 5, Feature = features[0], LastResetAt = DateTime.UtcNow.AddDays(-15), CreatedAt = DateTime.UtcNow.AddDays(-30) },
            new SubscriptionFeatureUsage { Id = _usage2Id, SubscriptionId = _subscription1Id, FeatureId = _feature2Id, Used = 25, Limit = 50, Feature = features[1], LastResetAt = DateTime.UtcNow.AddDays(-15), CreatedAt = DateTime.UtcNow.AddDays(-30) }
        );

        _context.SaveChanges();

        _unitOfWorkMock = CreateUnitOfWorkMockForContext(_context);
        _pricingServiceMock = new Mock<IPricingService>();
        _pricingServiceMock.Setup(p => p.CreatePackageSnapshotAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Result<PackageSnapshotDto>.Success(new PackageSnapshotDto
            {
                PackageId = _package1Id,
                PackageName = "Basic Package",
                PackageCode = "basic",
                MonthlyPrice = 100m,
                YearlyPrice = 1000m,
                Features = new List<PackageFeatureSnapshotDto>()
            }));

        _sut = CreateService(_context, _unitOfWorkMock.Object, _pricingServiceMock.Object);
    }

    private PlatformSubscriptionService CreateService(ApplicationDbContext context, IUnitOfWork unitOfWork, IPricingService pricingService)
    {
        var subscriptionRepository = new SubscriptionRepository(context);

        return new PlatformSubscriptionService(subscriptionRepository);
    }

    [Fact]
    public async Task GetSubscriptionByIdAsync_ValidId_ReturnsSubscription()
    {
        var result = await _sut.GetSubscriptionByIdAsync(_subscription1Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(_subscription1Id);
        result.Value.ClinicId.Should().Be(_clinic1Id);
        result.Value.PackageId.Should().Be(_package1Id);
        result.Value.PackageName.Should().Be("Basic Package");
        result.Value.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task GetSubscriptionByIdAsync_InvalidId_ReturnsNotFound()
    {
        var invalidId = Guid.NewGuid();
        var result = await _sut.GetSubscriptionByIdAsync(invalidId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Subscription not found");
    }

    [Fact]
    public async Task GetSubscriptionWithDetailsAsync_ValidId_ReturnsSubscriptionWithUsage()
    {
        await using var context = PlatformTestHelpers.CreateInMemoryContext();
        await PlatformTestHelpers.SeedDefaultDataAsync(context);

        var firstSubscription = await context.Subscriptions.FirstAsync();
        var firstUsage = await context.SubscriptionFeatureUsages.FirstAsync();

        var subscription = await context.Subscriptions.Include(s => s.FeatureUsage).FirstAsync(s => s.Id == firstSubscription.Id);
        subscription.FeatureUsage.Clear();
        subscription.FeatureUsage.Add(firstUsage);
        await context.SaveChangesAsync();

        var unitOfWorkMock = CreateUnitOfWorkMockForContext(context);
        var sut = CreateService(context, unitOfWorkMock.Object, _pricingServiceMock.Object);

        var result = await sut.GetSubscriptionWithDetailsAsync(firstSubscription.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(firstSubscription.Id);
        result.Value.FeatureUsage.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetSubscriptionWithDetailsAsync_ValidId_DeserializesSnapshotJson()
    {
        await using var context = PlatformTestHelpers.CreateInMemoryContext();
        await PlatformTestHelpers.SeedDefaultDataAsync(context);

        var firstSubscription = await context.Subscriptions.FirstAsync();
        firstSubscription.PriceSnapshot = $"{{\"PackageId\":\"{_package1Id}\",\"PackageName\":\"Basic Package\",\"PackageCode\":\"basic\",\"MonthlyPrice\":100,\"Features\":[]}}";
        await context.SaveChangesAsync();

        var unitOfWorkMock = CreateUnitOfWorkMockForContext(context);
        var sut = CreateService(context, unitOfWorkMock.Object, _pricingServiceMock.Object);

        var result = await sut.GetSubscriptionWithDetailsAsync(firstSubscription.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.PriceSnapshot.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSubscriptionWithDetailsAsync_InvalidId_ReturnsNotFound()
    {
        var invalidId = Guid.NewGuid();
        var result = await _sut.GetSubscriptionWithDetailsAsync(invalidId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Subscription not found");
    }

    [Fact]
    public async Task GetSubscriptionsAsync_NoFilter_ReturnsAllSubscriptions()
    {
        var result = await _sut.GetSubscriptionsAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        // Service orders by CreatedAt descending, so check IDs are all present
        var ids = result.Value.Select(s => s.Id).ToList();
        ids.Should().Contain(_subscription1Id);
        ids.Should().Contain(_subscription2Id);
        ids.Should().Contain(_subscription3Id);
    }

    [Fact]
    public async Task GetSubscriptionsAsync_WithTenantId_ReturnsFiltered()
    {
        var result = await _sut.GetSubscriptionsAsync(_clinic1Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.All(s => s.ClinicId == _clinic1Id).Should().BeTrue();
    }

    // Helper method to create a UnitOfWork mock for a specific context
    private Mock<IUnitOfWork> CreateUnitOfWorkMockForContext(ApplicationDbContext context)
    {
        var mock = new Mock<IUnitOfWork>();
        mock.Setup(u => u.SaveChangesAsync())
            .Returns(async () =>
            {
                await context.SaveChangesAsync();
                return 1;
            });
        mock.Setup(u => u.BeginTransactionAsync())
            .Returns(() => Task.CompletedTask);
        mock.Setup(u => u.CommitTransactionAsync())
            .Returns(() => Task.CompletedTask);
        mock.Setup(u => u.RollbackTransactionAsync())
            .Returns(() => Task.CompletedTask);
        mock.Setup(u => u.Repository<SubscriptionFeatureUsage>())
            .Returns(new Repository<SubscriptionFeatureUsage>(context));
        return mock;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

public class SubscriptionLifecycleServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPricingService> _pricingServiceMock;
    private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock;
    private readonly Mock<IInvoiceService> _invoiceServiceMock;
    private readonly Mock<ILogger<SubscriptionLifecycleService>> _loggerMock;
    private readonly Mock<IClock> _clockMock;
    private readonly SubscriptionLifecycleService _sut;
    private readonly PlatformSubscriptionService _queryService;

    private readonly Guid _feature1Id = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private readonly Guid _feature2Id = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private readonly Guid _categoryId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private readonly Guid _package1Id = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private readonly Guid _package2Id = Guid.Parse("30000000-0000-0000-0000-000000000002");
    private readonly Guid _clinic1Id = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private readonly Guid _clinic2Id = Guid.Parse("40000000-0000-0000-0000-000000000002");
    private readonly Guid _subscription1Id = Guid.Parse("50000000-0000-0000-0000-000000000001");
    private readonly Guid _subscription3Id = Guid.Parse("50000000-0000-0000-0000-000000000003");
    private readonly Guid _usage1Id = Guid.Parse("60000000-0000-0000-0000-000000000001");
    private readonly Guid _usage2Id = Guid.Parse("60000000-0000-0000-0000-000000000002");
    private readonly Guid _packageFeature1Id = Guid.Parse("70000000-0000-0000-0000-000000000001");
    private readonly Guid _packageFeature2Id = Guid.Parse("70000000-0000-0000-0000-000000000002");
    private readonly Guid _packageFeature3Id = Guid.Parse("70000000-0000-0000-0000-000000000003");
    private readonly Guid _packageFeature4Id = Guid.Parse("70000000-0000-0000-0000-000000000004");

    public SubscriptionLifecycleServiceTests()
    {
        _context = PlatformTestHelpers.CreateInMemoryContext();

        var features = new List<Feature>
        {
            PlatformTestHelpers.CreateTestFeature(_feature1Id, "users", "User Seats", PricingType.PerUser, categoryId: _categoryId),
            PlatformTestHelpers.CreateTestFeature(_feature2Id, "storage", "Storage", PricingType.PerStorageGB, categoryId: _categoryId)
        };
        _context.Features.AddRange(features);

        _context.Packages.AddRange(
            PlatformTestHelpers.CreateTestPackage(_package1Id, "basic", "Basic Package", PackageStatus.Active, monthlyPrice: 100m, yearlyPrice: 1000m, trialDays: 14),
            PlatformTestHelpers.CreateTestPackage(_package2Id, "premium", "Premium Package", PackageStatus.Active, monthlyPrice: 500m, yearlyPrice: 5000m, trialDays: 30)
        );

        _context.PackageFeatures.AddRange(
            new PackageFeature { Id = _packageFeature1Id, PackageId = _package1Id, FeatureId = _feature1Id, IsIncluded = true, Feature = features[0], CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new PackageFeature { Id = _packageFeature2Id, PackageId = _package1Id, FeatureId = _feature2Id, IsIncluded = true, Feature = features[1], CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new PackageFeature { Id = _packageFeature3Id, PackageId = _package2Id, FeatureId = _feature1Id, IsIncluded = true, Feature = features[0], CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new PackageFeature { Id = _packageFeature4Id, PackageId = _package2Id, FeatureId = _feature2Id, IsIncluded = true, Feature = features[1], CreatedAt = DateTime.UtcNow.AddDays(-5) }
        );

        _context.Clinics.AddRange(
            new Clinic { Id = _clinic1Id, Name = "Test Clinic", IsDeleted = false },
            new Clinic { Id = _clinic2Id, Name = "Another Clinic", IsDeleted = false }
        );

        _context.SaveChanges();

        _context.Subscriptions.AddRange(
            PlatformTestHelpers.CreateTestSubscription(_subscription1Id, _clinic1Id, _package1Id, SubscriptionStatus.Active, priceSnapshot: "{\"BasePrice\":100}"),
            PlatformTestHelpers.CreateTestSubscription(_subscription3Id, _clinic1Id, _package1Id, SubscriptionStatus.Cancelled, priceSnapshot: "{}")
        );

        _context.SubscriptionFeatureUsages.AddRange(
            new SubscriptionFeatureUsage { Id = _usage1Id, SubscriptionId = _subscription1Id, FeatureId = _feature1Id, Used = 3, Limit = 5, Feature = features[0], LastResetAt = DateTime.UtcNow.AddDays(-15), CreatedAt = DateTime.UtcNow.AddDays(-30) },
            new SubscriptionFeatureUsage { Id = _usage2Id, SubscriptionId = _subscription1Id, FeatureId = _feature2Id, Used = 25, Limit = 50, Feature = features[1], LastResetAt = DateTime.UtcNow.AddDays(-15), CreatedAt = DateTime.UtcNow.AddDays(-30) }
        );

        _context.SaveChanges();

        _unitOfWorkMock = CreateUnitOfWorkMockForContext(_context);
        _pricingServiceMock = new Mock<IPricingService>();
        _pricingServiceMock.Setup(p => p.CreatePackageSnapshotAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Result<PackageSnapshotDto>.Success(new PackageSnapshotDto
            {
                PackageId = _package1Id,
                PackageName = "Basic Package",
                PackageCode = "basic",
                MonthlyPrice = 100m,
                YearlyPrice = 1000m,
                Features = new List<PackageFeatureSnapshotDto>()
            }));
        _backgroundJobClientMock = new Mock<IBackgroundJobClient>();
        _invoiceServiceMock = new Mock<IInvoiceService>();
        var invoiceId = Guid.NewGuid();
        _invoiceServiceMock.Setup(i => i.GenerateInvoiceAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Result<InvoiceDto>.Success(new InvoiceDto { Id = invoiceId }));
        _invoiceServiceMock.Setup(i => i.MarkAsPaidAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Result<InvoiceDto>.Success(new InvoiceDto { Id = invoiceId }));
        _loggerMock = new Mock<ILogger<SubscriptionLifecycleService>>();
        _clockMock = new Mock<IClock>();
        _clockMock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

        var subscriptionRepository = new SubscriptionRepository(_context);
        _queryService = new PlatformSubscriptionService(subscriptionRepository);
        _sut = new SubscriptionLifecycleService(
            subscriptionRepository,
            new PackageRepository(_context),
            new ClinicRepository(_context),
            _unitOfWorkMock.Object,
            _pricingServiceMock.Object,
            _queryService,
            _backgroundJobClientMock.Object,
            _invoiceServiceMock.Object,
            _loggerMock.Object,
            _clockMock.Object);
    }

    private Mock<IUnitOfWork> CreateUnitOfWorkMockForContext(ApplicationDbContext context)
    {
        var mock = new Mock<IUnitOfWork>();
        mock.Setup(u => u.SaveChangesAsync()).Returns(() => context.SaveChangesAsync());
        mock.Setup(u => u.BeginTransactionAsync()).Returns(() => Task.CompletedTask);
        mock.Setup(u => u.CommitTransactionAsync()).Returns(() => Task.CompletedTask);
        mock.Setup(u => u.RollbackTransactionAsync()).Returns(() => Task.CompletedTask);
        mock.Setup(u => u.Repository<SubscriptionFeatureUsage>()).Returns(new Repository<SubscriptionFeatureUsage>(context));
        return mock;
    }

    [Fact]
    public async Task CreateSubscriptionAsync_ValidRequest_CreatesSubscription()
    {
        await using var context = PlatformTestHelpers.CreateInMemoryContext();
        await PlatformTestHelpers.SeedDefaultDataAsync(context);

        var firstPackage = await context.Packages.Include(p => p.Features).FirstAsync();
        var firstPackageFeature = await context.PackageFeatures
            .Include(pf => pf.Feature)
            .FirstAsync(pf => pf.PackageId == firstPackage.Id);
        firstPackage.Features.Clear();
        firstPackage.Features.Add(firstPackageFeature);
        await context.SaveChangesAsync();

        var unitOfWorkMock = CreateUnitOfWorkMockForContext(context);
        var subscriptionRepository = new SubscriptionRepository(context);
        var queryService = new PlatformSubscriptionService(subscriptionRepository);
        var sut = new SubscriptionLifecycleService(
            subscriptionRepository,
            new PackageRepository(context),
            new ClinicRepository(context),
            unitOfWorkMock.Object,
            _pricingServiceMock.Object,
            queryService,
            _backgroundJobClientMock.Object,
            _invoiceServiceMock.Object,
            _loggerMock.Object,
            _clockMock.Object);

        var clinic = await context.Clinics.FirstAsync(c => !context.Subscriptions.Any(s => s.ClinicId == c.Id && s.Status == SubscriptionStatus.Active));

        var request = new CreateSubscriptionRequestDto
        {
            ClinicId = clinic.Id,
            PackageId = firstPackage.Id,
            PaymentProvider = "stripe",
            AutoRenew = true
        };

        var result = await sut.CreateSubscriptionAsync(request);

        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        result.Value.ClinicId.Should().Be(clinic.Id);
        result.Value.PackageId.Should().Be(firstPackage.Id);
        result.Value.Status.Should().Be(SubscriptionStatus.Trial);
        result.Value.AutoRenew.Should().BeTrue();

        var subscription = await context.Subscriptions.FindAsync(result.Value.Id);
        subscription.Should().NotBeNull();
        subscription!.ClinicId.Should().Be(clinic.Id);
        subscription.PackageId.Should().Be(firstPackage.Id);

        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_CreatesFeatureUsageRecords()
    {
        await using var context = PlatformTestHelpers.CreateInMemoryContext();
        await PlatformTestHelpers.SeedDefaultDataAsync(context);

        var firstPackage = await context.Packages.Include(p => p.Features).FirstAsync();
        var firstPackageFeature = await context.PackageFeatures
            .Include(pf => pf.Feature)
            .FirstAsync(pf => pf.PackageId == firstPackage.Id);
        firstPackage.Features.Clear();
        firstPackage.Features.Add(firstPackageFeature);
        await context.SaveChangesAsync();

        var unitOfWorkMock = CreateUnitOfWorkMockForContext(context);
        var subscriptionRepository = new SubscriptionRepository(context);
        var queryService = new PlatformSubscriptionService(subscriptionRepository);
        var sut = new SubscriptionLifecycleService(
            subscriptionRepository,
            new PackageRepository(context),
            new ClinicRepository(context),
            unitOfWorkMock.Object,
            _pricingServiceMock.Object,
            queryService,
            _backgroundJobClientMock.Object,
            _invoiceServiceMock.Object,
            _loggerMock.Object,
            _clockMock.Object);

        var clinic = await context.Clinics.FirstAsync(c => !context.Subscriptions.Any(s => s.ClinicId == c.Id && s.Status == SubscriptionStatus.Active));

        var request = new CreateSubscriptionRequestDto
        {
            ClinicId = clinic.Id,
            PackageId = firstPackage.Id
        };

        var result = await sut.CreateSubscriptionAsync(request);

        result.IsSuccess.Should().BeTrue();

        var usageRecords = await context.SubscriptionFeatureUsages
            .Where(u => u.SubscriptionId == result.Value.Id)
            .ToListAsync();
        usageRecords.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_TrialDays_SetsTrialStatus()
    {
        await using var context = PlatformTestHelpers.CreateInMemoryContext();
        await PlatformTestHelpers.SeedDefaultDataAsync(context);

        var firstPackage = await context.Packages.FirstAsync();
        firstPackage.TrialDays = 14;
        firstPackage.Features.Clear();
        await context.SaveChangesAsync();

        var unitOfWorkMock = CreateUnitOfWorkMockForContext(context);
        var subscriptionRepository = new SubscriptionRepository(context);
        var queryService = new PlatformSubscriptionService(subscriptionRepository);
        var sut = new SubscriptionLifecycleService(
            subscriptionRepository,
            new PackageRepository(context),
            new ClinicRepository(context),
            unitOfWorkMock.Object,
            _pricingServiceMock.Object,
            queryService,
            _backgroundJobClientMock.Object,
            _invoiceServiceMock.Object,
            _loggerMock.Object,
            _clockMock.Object);

        var clinic = await context.Clinics.FirstAsync(c => !context.Subscriptions.Any(s => s.ClinicId == c.Id && s.Status == SubscriptionStatus.Active));

        var request = new CreateSubscriptionRequestDto
        {
            ClinicId = clinic.Id,
            PackageId = firstPackage.Id
        };

        var result = await sut.CreateSubscriptionAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(SubscriptionStatus.Trial);
        result.Value.TrialEndsAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateSubscriptionAsync_NoTrialDays_SetsActiveStatus()
    {
        await using var context = PlatformTestHelpers.CreateInMemoryContext();
        await PlatformTestHelpers.SeedDefaultDataAsync(context);

        var firstPackage = await context.Packages.FirstAsync();
        firstPackage.TrialDays = 0;
        firstPackage.Features.Clear();
        await context.SaveChangesAsync();

        var unitOfWorkMock = CreateUnitOfWorkMockForContext(context);
        var subscriptionRepository = new SubscriptionRepository(context);
        var queryService = new PlatformSubscriptionService(subscriptionRepository);
        var sut = new SubscriptionLifecycleService(
            subscriptionRepository,
            new PackageRepository(context),
            new ClinicRepository(context),
            unitOfWorkMock.Object,
            _pricingServiceMock.Object,
            queryService,
            _backgroundJobClientMock.Object,
            _invoiceServiceMock.Object,
            _loggerMock.Object,
            _clockMock.Object);

        var clinic = await context.Clinics.FirstAsync(c => !context.Subscriptions.Any(s => s.ClinicId == c.Id && s.Status == SubscriptionStatus.Active));

        var request = new CreateSubscriptionRequestDto
        {
            ClinicId = clinic.Id,
            PackageId = firstPackage.Id
        };

        var result = await sut.CreateSubscriptionAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(SubscriptionStatus.Active);
        result.Value.TrialEndsAt.Should().BeNull();
    }

    [Fact]
    public async Task CreateSubscriptionAsync_InvalidTenantId_ReturnsError()
    {
        var invalidClinicId = Guid.NewGuid();
        var request = new CreateSubscriptionRequestDto
        {
            ClinicId = invalidClinicId,
            PackageId = _package1Id
        };

        var result = await _sut.CreateSubscriptionAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Tenant not found");
    }

    [Fact]
    public async Task CreateSubscriptionAsync_InvalidPackageId_ReturnsError()
    {
        var invalidPackageId = Guid.NewGuid();
        var request = new CreateSubscriptionRequestDto
        {
            ClinicId = _clinic2Id,
            PackageId = invalidPackageId
        };

        var result = await _sut.CreateSubscriptionAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Package not found or inactive");
    }

    [Fact]
    public async Task CreateSubscriptionAsync_TenantHasActiveSubscription_ReturnsError()
    {
        var request = new CreateSubscriptionRequestDto
        {
            ClinicId = _clinic1Id,
            PackageId = _package1Id
        };

        var result = await _sut.CreateSubscriptionAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Tenant already has an active subscription");
    }

    [Fact]
    public async Task CancelSubscriptionAsync_ValidId_CancelsSubscription()
    {
        var request = new CancelSubscriptionRequestDto
        {
            Reason = "Not needed anymore"
        };

        var result = await _sut.CancelSubscriptionAsync(_subscription1Id, request);

        result.IsSuccess.Should().BeTrue();

        var subscription = await _context.Subscriptions.FirstAsync(s => s.Id == _subscription1Id);
        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
        subscription.CancelledAt.Should().NotBeNull();
        subscription.CancelReason.Should().Be("Not needed anymore");
        subscription.AutoRenew.Should().BeFalse();

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CancelSubscriptionAsync_AlreadyCancelled_ReturnsError()
    {
        var request = new CancelSubscriptionRequestDto
        {
            Reason = "Test"
        };

        var result = await _sut.CancelSubscriptionAsync(_subscription3Id, request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Subscription is already cancelled or expired");
    }

    [Fact]
    public async Task CancelSubscriptionAsync_InvalidId_ReturnsError()
    {
        var invalidId = Guid.NewGuid();
        var request = new CancelSubscriptionRequestDto
        {
            Reason = "Test"
        };

        var result = await _sut.CancelSubscriptionAsync(invalidId, request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Subscription not found");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
