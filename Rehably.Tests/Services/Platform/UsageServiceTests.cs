using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Rehably.Application.DTOs.Platform;
using Rehably.Application.DTOs.Usage;
using Rehably.Application.DTOs.Subscription;
using Rehably.Application.Services.Platform;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Services.Platform;
using Rehably.Tests.Helpers;
using FluentAssertions;

namespace Rehably.Tests.Services.Platform;

public class UsageServiceTests : IDisposable
{
    private readonly Mock<ISubscriptionRepository> _subscriptionRepoMock;
    private readonly Mock<ISubscriptionFeatureUsageRepository> _usageRepoMock;
    private readonly Mock<IMemoryCache> _cacheMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UsageService _sut;

    // Test IDs
    private readonly Guid _feature1Id = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private readonly Guid _feature2Id = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private readonly Guid _feature3Id = Guid.Parse("10000000-0000-0000-0000-000000000003");
    private readonly Guid _categoryId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private readonly Guid _package1Id = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private readonly Guid _clinic1Id = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private readonly Guid _clinic2Id = Guid.Parse("40000000-0000-0000-0000-000000000002");
    private readonly Guid _clinic3Id = Guid.Parse("40000000-0000-0000-0000-000000000003");
    private readonly Guid _subscription1Id = Guid.Parse("50000000-0000-0000-0000-000000000001");
    private readonly Guid _subscription2Id = Guid.Parse("50000000-0000-0000-0000-000000000002");
    private readonly Guid _subscription3Id = Guid.Parse("50000000-0000-0000-0000-000000000003");
    private readonly Guid _usage1Id = Guid.Parse("60000000-0000-0000-0000-000000000001");
    private readonly Guid _usage2Id = Guid.Parse("60000000-0000-0000-0000-000000000002");
    private readonly Guid _packageFeature1Id = Guid.Parse("70000000-0000-0000-0000-000000000001");
    private readonly Guid _packageFeature2Id = Guid.Parse("70000000-0000-0000-0000-000000000002");
    private readonly Guid _packageFeature3Id = Guid.Parse("70000000-0000-0000-0000-000000000003");

    private readonly List<Feature> _features;
    private readonly Package _package;
    private readonly Subscription _subscription1;
    private readonly Subscription _subscription2;
    private readonly List<SubscriptionFeatureUsage> _usageRecords;

    public UsageServiceTests()
    {
        _subscriptionRepoMock = new Mock<ISubscriptionRepository>();
        _usageRepoMock = new Mock<ISubscriptionFeatureUsageRepository>();
        _cacheMock = new Mock<IMemoryCache>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        // Setup features
        _features = new List<Feature>
        {
            PlatformTestHelpers.CreateTestFeature(_feature1Id, "users", "User Seats", PricingType.PerUser, categoryId: _categoryId),
            PlatformTestHelpers.CreateTestFeature(_feature2Id, "storage", "Storage", PricingType.PerStorageGB, categoryId: _categoryId),
            PlatformTestHelpers.CreateTestFeature(_feature3Id, "sms-notifications", "SMS Notifications", PricingType.PerUnit, categoryId: _categoryId)
        };

        // Setup package with features
        _package = PlatformTestHelpers.CreateTestPackage(_package1Id, "basic", "Basic Package", PackageStatus.Active, monthlyPrice: 100m, yearlyPrice: 1000m, trialDays: 14);
        _package.Features = new List<PackageFeature>
        {
            new PackageFeature { Id = _packageFeature1Id, PackageId = _package1Id, FeatureId = _feature1Id, IsIncluded = true, Quantity = 5, Feature = _features[0], CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new PackageFeature { Id = _packageFeature2Id, PackageId = _package1Id, FeatureId = _feature2Id, IsIncluded = true, Quantity = 50, Feature = _features[1], CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new PackageFeature { Id = _packageFeature3Id, PackageId = _package1Id, FeatureId = _feature3Id, IsIncluded = true, Quantity = null, Feature = _features[2], CreatedAt = DateTime.UtcNow.AddDays(-5) }
        };

        // Setup subscriptions
        _subscription1 = PlatformTestHelpers.CreateTestSubscription(_subscription1Id, _clinic1Id, _package1Id, SubscriptionStatus.Active);
        _subscription1.Package = _package;
        _subscription1.FeatureUsage = new List<SubscriptionFeatureUsage>();

        _subscription2 = PlatformTestHelpers.CreateTestSubscription(_subscription2Id, _clinic2Id, _package1Id, SubscriptionStatus.Trial);
        _subscription2.Package = _package;
        _subscription2.FeatureUsage = new List<SubscriptionFeatureUsage>();

        // Setup usage records
        _usageRecords = new List<SubscriptionFeatureUsage>
        {
            new SubscriptionFeatureUsage { Id = _usage1Id, SubscriptionId = _subscription1Id, FeatureId = _feature1Id, Used = 3, Limit = 5, Feature = _features[0], LastResetAt = DateTime.UtcNow.AddDays(-15), CreatedAt = DateTime.UtcNow.AddDays(-30) },
            new SubscriptionFeatureUsage { Id = _usage2Id, SubscriptionId = _subscription1Id, FeatureId = _feature2Id, Used = 25, Limit = 50, Feature = _features[1], LastResetAt = DateTime.UtcNow.AddDays(-15), CreatedAt = DateTime.UtcNow.AddDays(-30) }
        };
        _subscription1.FeatureUsage = _usageRecords.ToList();

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        var clockMock = new Mock<Rehably.Application.Interfaces.IClock>();
        clockMock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);
        _sut = new UsageService(_subscriptionRepoMock.Object, _usageRepoMock.Object, _cacheMock.Object, _unitOfWorkMock.Object, clockMock.Object, Mock.Of<ILogger<UsageService>>());
    }

    [Fact]
    public async Task CanUseFeatureAsync_ValidSubscription_UsageBelowLimit_ReturnsTrue()
    {
        // Arrange
        _subscriptionRepoMock.Setup(r => r.GetActiveSubscriptionWithDetailsByClinicIdAsync(_clinic1Id))
            .ReturnsAsync(_subscription1);

        // Act
        var result = await _sut.CanUseFeatureAsync(_clinic1Id, "users");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task CanUseFeatureAsync_NoSubscription_ReturnsFalse()
    {
        // Arrange
        var invalidClinicId = Guid.NewGuid();
        _subscriptionRepoMock.Setup(r => r.GetActiveSubscriptionWithDetailsByClinicIdAsync(invalidClinicId))
            .ReturnsAsync((Subscription?)null);

        // Act
        var result = await _sut.CanUseFeatureAsync(invalidClinicId, "users");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("No active subscription found");
    }

    [Fact]
    public async Task CanUseFeatureAsync_FeatureNotIncluded_ReturnsFalse()
    {
        // Arrange
        var packageWithoutUsers = PlatformTestHelpers.CreateTestPackage(_package1Id, "basic", "Basic Package", PackageStatus.Active);
        packageWithoutUsers.Features = new List<PackageFeature>
        {
            new PackageFeature { Id = _packageFeature2Id, PackageId = _package1Id, FeatureId = _feature2Id, IsIncluded = true, Feature = _features[1], CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new PackageFeature { Id = _packageFeature3Id, PackageId = _package1Id, FeatureId = _feature3Id, IsIncluded = true, Feature = _features[2], CreatedAt = DateTime.UtcNow.AddDays(-5) }
        };
        var subscriptionWithoutUsers = PlatformTestHelpers.CreateTestSubscription(_subscription1Id, _clinic1Id, _package1Id, SubscriptionStatus.Active);
        subscriptionWithoutUsers.Package = packageWithoutUsers;
        subscriptionWithoutUsers.FeatureUsage = new List<SubscriptionFeatureUsage>();

        _subscriptionRepoMock.Setup(r => r.GetActiveSubscriptionWithDetailsByClinicIdAsync(_clinic1Id))
            .ReturnsAsync(subscriptionWithoutUsers);

        // Act
        var result = await _sut.CanUseFeatureAsync(_clinic1Id, "users");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Feature not included in subscription package");
    }

    [Fact]
    public async Task CanUseFeatureAsync_UsageAtLimit_ReturnsFalse()
    {
        // Arrange
        var subscriptionAtLimit = PlatformTestHelpers.CreateTestSubscription(_subscription1Id, _clinic1Id, _package1Id, SubscriptionStatus.Active);
        subscriptionAtLimit.Package = _package;
        subscriptionAtLimit.FeatureUsage = new List<SubscriptionFeatureUsage>
        {
            new SubscriptionFeatureUsage { Id = _usage1Id, SubscriptionId = _subscription1Id, FeatureId = _feature1Id, Used = 5, Limit = 5, Feature = _features[0], LastResetAt = DateTime.UtcNow.AddDays(-15), CreatedAt = DateTime.UtcNow.AddDays(-30) }
        };

        _subscriptionRepoMock.Setup(r => r.GetActiveSubscriptionWithDetailsByClinicIdAsync(_clinic1Id))
            .ReturnsAsync(subscriptionAtLimit);

        // Act
        var result = await _sut.CanUseFeatureAsync(_clinic1Id, "users");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task CanUseFeatureAsync_UnlimitedFeature_ReturnsTrue()
    {
        // Arrange
        _subscriptionRepoMock.Setup(r => r.GetActiveSubscriptionWithDetailsByClinicIdAsync(_clinic1Id))
            .ReturnsAsync(_subscription1);

        // Act
        var result = await _sut.CanUseFeatureAsync(_clinic1Id, "sms-notifications");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task IncrementUsageAsync_ValidUsage_IncrementsCounter()
    {
        // Arrange
        _subscriptionRepoMock.Setup(r => r.GetActiveSubscriptionWithDetailsByClinicIdAsync(_clinic1Id))
            .ReturnsAsync(_subscription1);

        // Act
        var result = await _sut.IncrementUsageAsync(_clinic1Id, "users", 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task IncrementUsageAsync_FirstUsage_CreatesUsageRecord()
    {
        // Arrange
        _subscriptionRepoMock.Setup(r => r.GetActiveSubscriptionWithDetailsByClinicIdAsync(_clinic1Id))
            .ReturnsAsync(_subscription1);
        _usageRepoMock.Setup(r => r.AddAsync(It.IsAny<SubscriptionFeatureUsage>()))
            .ReturnsAsync((SubscriptionFeatureUsage u) => u);

        // Act
        var result = await _sut.IncrementUsageAsync(_clinic1Id, "sms-notifications", 5);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _usageRepoMock.Verify(r => r.AddAsync(It.IsAny<SubscriptionFeatureUsage>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task IncrementUsageAsync_ExceedsLimit_ReturnsError()
    {
        // Arrange
        var subscriptionAtLimit = PlatformTestHelpers.CreateTestSubscription(_subscription1Id, _clinic1Id, _package1Id, SubscriptionStatus.Active);
        subscriptionAtLimit.Package = _package;
        subscriptionAtLimit.FeatureUsage = new List<SubscriptionFeatureUsage>
        {
            new SubscriptionFeatureUsage { Id = _usage1Id, SubscriptionId = _subscription1Id, FeatureId = _feature1Id, Used = 5, Limit = 5, Feature = _features[0], LastResetAt = DateTime.UtcNow.AddDays(-15), CreatedAt = DateTime.UtcNow.AddDays(-30) }
        };

        _subscriptionRepoMock.Setup(r => r.GetActiveSubscriptionWithDetailsByClinicIdAsync(_clinic1Id))
            .ReturnsAsync(subscriptionAtLimit);

        // Act
        var result = await _sut.IncrementUsageAsync(_clinic1Id, "users", 1);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Feature usage limit exceeded");
    }

    [Fact]
    public async Task IncrementUsageAsync_NoSubscription_ReturnsError()
    {
        // Arrange
        var invalidClinicId = Guid.NewGuid();
        _subscriptionRepoMock.Setup(r => r.GetActiveSubscriptionWithDetailsByClinicIdAsync(invalidClinicId))
            .ReturnsAsync((Subscription?)null);

        // Act
        var result = await _sut.IncrementUsageAsync(invalidClinicId, "users", 1);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("No active subscription found");
    }

    [Fact]
    public async Task GetUsageAsync_ReturnsUsageWithLimit()
    {
        // Arrange
        _subscriptionRepoMock.Setup(r => r.GetActiveSubscriptionWithDetailsByClinicIdAsync(_clinic1Id))
            .ReturnsAsync(_subscription1);

        // Act
        var result = await _sut.GetUsageAsync(_clinic1Id, "users");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FeatureCode.Should().Be("users");
        result.Value.Limit.Should().Be(5);
        result.Value.Used.Should().Be(3);
    }

    [Fact]
    public async Task GetUsageAsync_UsageNotFound_ReturnsError()
    {
        // Arrange
        _subscriptionRepoMock.Setup(r => r.GetActiveSubscriptionWithDetailsByClinicIdAsync(_clinic1Id))
            .ReturnsAsync(_subscription1);

        // Act
        var result = await _sut.GetUsageAsync(_clinic1Id, "sms-notifications");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Usage not found for this feature");
    }

    [Fact]
    public async Task GetAllUsageAsync_NoCache_ReturnsAllUsage()
    {
        // Arrange
        object? cachedValue = null;
        _cacheMock.Setup(c => c.TryGetValue(It.IsAny<object>(), out cachedValue))
            .Returns(false);

        var entryMock = CacheTestHelpers.SetupCacheEntry(_cacheMock, $"usage:all:{_clinic1Id}", new List<SubscriptionFeatureUsageDto>());

        _subscriptionRepoMock.Setup(r => r.GetActiveSubscriptionWithDetailsByClinicIdAsync(_clinic1Id))
            .ReturnsAsync(_subscription1);

        // Act
        var result = await _sut.GetAllUsageAsync(_clinic1Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].FeatureCode.Should().Be("users");
        result.Value[1].FeatureCode.Should().Be("storage");
    }

    [Fact]
    public async Task GetAllUsageAsync_NoSubscription_ReturnsError()
    {
        // Arrange
        var invalidClinicId = Guid.NewGuid();
        object? cachedValue = null;
        _cacheMock.Setup(c => c.TryGetValue(It.IsAny<object>(), out cachedValue))
            .Returns(false);

        _subscriptionRepoMock.Setup(r => r.GetActiveSubscriptionWithDetailsByClinicIdAsync(invalidClinicId))
            .ReturnsAsync((Subscription?)null);

        // Act
        var result = await _sut.GetAllUsageAsync(invalidClinicId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("No active subscription found");
    }

    [Fact]
    public async Task ResetFeatureUsageAsync_ValidUsage_ResetsCounter()
    {
        // Arrange
        _usageRepoMock.Setup(r => r.GetBySubscriptionAndFeatureAsync(_subscription1Id, _feature1Id))
            .ReturnsAsync(_usageRecords[0]);

        // Act
        var result = await _sut.ResetFeatureUsageAsync(_subscription1Id, _feature1Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ResetFeatureUsageAsync_UsageNotFound_ReturnsError()
    {
        // Arrange
        var invalidFeatureId = Guid.NewGuid();
        _usageRepoMock.Setup(r => r.GetBySubscriptionAndFeatureAsync(_subscription1Id, invalidFeatureId))
            .ReturnsAsync((SubscriptionFeatureUsage?)null);

        // Act
        var result = await _sut.ResetFeatureUsageAsync(_subscription1Id, invalidFeatureId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Usage record not found");
    }

    [Fact]
    public async Task ResetAllUsageAsync_ResetsAllCounters()
    {
        // Arrange
        _usageRepoMock.Setup(r => r.GetBySubscriptionIdAsync(_subscription1Id))
            .ReturnsAsync(_usageRecords);

        // Act
        var result = await _sut.ResetAllUsageAsync(_subscription1Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetUsageStatsAsync_NoCache_ReturnsStatistics()
    {
        // Arrange
        object? cachedValue = null;
        _cacheMock.Setup(c => c.TryGetValue(It.IsAny<object>(), out cachedValue))
            .Returns(false);

        var entryMock = CacheTestHelpers.SetupCacheEntry(_cacheMock, $"usage:stats:{_clinic1Id}", new Dictionary<string, UsageStatsDto>());

        _subscriptionRepoMock.Setup(r => r.GetActiveSubscriptionByClinicIdAsync(_clinic1Id))
            .ReturnsAsync(_subscription1);

        // Act
        var result = await _sut.GetUsageStatsAsync(_clinic1Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainKey("users");
        result.Value.Should().ContainKey("storage");
        result.Value["users"].Limit.Should().Be(5);
        result.Value["users"].Used.Should().Be(3);
    }

    public void Dispose()
    {
        // No resources to dispose with mocks
    }
}
