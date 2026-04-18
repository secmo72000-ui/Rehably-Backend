using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rehably.Application.Contexts;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;
using Rehably.Infrastructure.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Rehably.Tests.Services.Platform;

/// <summary>
/// Service behavior tests for SubscriptionRepository
/// T067: Service Behavior Tests - Repository Pattern
/// </summary>
public class SubscriptionRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SubscriptionRepository _repository;

    public SubscriptionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(t => t.TenantId).Returns((Guid?)null);
        _context = new ApplicationDbContext(options, null, mockTenantContext.Object);
        _repository = new SubscriptionRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private Subscription CreateSubscription(Guid clinicId, SubscriptionStatus status = SubscriptionStatus.Active)
    {
        var packageId = Guid.NewGuid();

        // Seed related entities so Include() queries work with InMemory
        _context.Packages.Add(new Package
        {
            Id = packageId,
            Name = "Test Package",
            Code = "test-" + packageId.ToString()[..8],
            MonthlyPrice = 100,
            YearlyPrice = 1000,
            Status = PackageStatus.Active
        });

        if (!_context.Clinics.Any(c => c.Id == clinicId))
        {
            _context.Clinics.Add(new Clinic
            {
                Id = clinicId,
                Name = "Test Clinic",
                Status = ClinicStatus.Active,
                Slug = "test-" + clinicId.ToString()[..8],
                Phone = "123"
            });
        }

        _context.SaveChanges();

        return new Subscription
        {
            ClinicId = clinicId,
            Status = status,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1),
            PackageId = packageId,
            BillingCycle = BillingCycle.Monthly,
            PriceSnapshot = "100"
        };
    }

    [Fact]
    public async Task GetActiveSubscriptionByClinicIdAsync_WithActiveSubscription_ReturnsSubscription()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var subscription = CreateSubscription(clinicId, SubscriptionStatus.Active);
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveSubscriptionByClinicIdAsync(clinicId);

        // Assert
        result.Should().NotBeNull();
        result!.ClinicId.Should().Be(clinicId);
        result.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task GetActiveSubscriptionByClinicIdAsync_WithNoActiveSubscription_ReturnsNull()
    {
        // Arrange
        var clinicId = Guid.NewGuid();

        // Act
        var result = await _repository.GetActiveSubscriptionByClinicIdAsync(clinicId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetExpiringAsync_WithExpiringSubscriptions_ReturnsFiltered()
    {
        // Arrange
        var expiringDate = DateTime.UtcNow.AddDays(7);
        var subscription = CreateSubscription(Guid.NewGuid(), SubscriptionStatus.Active);
        subscription.EndDate = expiringDate;

        var notExpiringSubscription = CreateSubscription(Guid.NewGuid(), SubscriptionStatus.Active);
        notExpiringSubscription.EndDate = DateTime.UtcNow.AddDays(30);

        _context.Subscriptions.AddRange(subscription, notExpiringSubscription);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetExpiringAsync(10);

        // Assert
        result.Should().ContainSingle();
        result.First().Id.Should().Be(subscription.Id);
    }

    [Fact]
    public async Task GetByStatusAsync_WithActiveStatus_ReturnsActiveSubscriptions()
    {
        // Arrange
        var activeSubscription = CreateSubscription(Guid.NewGuid(), SubscriptionStatus.Active);
        var expiredSubscription = CreateSubscription(Guid.NewGuid(), SubscriptionStatus.Expired);

        _context.Subscriptions.AddRange(activeSubscription, expiredSubscription);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStatusAsync(SubscriptionStatus.Active);

        // Assert
        result.Should().ContainSingle();
        result.First().Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task GetByClinicIdAsync_WithClinicId_ReturnsAllSubscriptions()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var subscription1 = CreateSubscription(clinicId, SubscriptionStatus.Active);
        var subscription2 = CreateSubscription(clinicId, SubscriptionStatus.Expired);
        var otherClinicSubscription = CreateSubscription(Guid.NewGuid(), SubscriptionStatus.Active);

        _context.Subscriptions.AddRange(subscription1, subscription2, otherClinicSubscription);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByClinicIdAsync(clinicId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task CountActiveByPackageAsync_WithActiveSubscriptions_ReturnsCount()
    {
        // Arrange
        var packageId = Guid.NewGuid();
        var subscription1 = CreateSubscription(Guid.NewGuid(), SubscriptionStatus.Active);
        subscription1.PackageId = packageId;
        var subscription2 = CreateSubscription(Guid.NewGuid(), SubscriptionStatus.Active);
        subscription2.PackageId = packageId;
        var expiredSubscription = CreateSubscription(Guid.NewGuid(), SubscriptionStatus.Expired);
        expiredSubscription.PackageId = packageId;

        _context.Subscriptions.AddRange(subscription1, subscription2, expiredSubscription);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.CountActiveByPackageAsync(packageId);

        // Assert
        result.Should().Be(2);
    }
}
