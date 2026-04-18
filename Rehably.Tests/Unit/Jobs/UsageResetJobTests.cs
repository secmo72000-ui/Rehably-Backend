using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Rehably.Application.Interfaces;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;
using Rehably.Infrastructure.Jobs;
using Rehably.Tests.Helpers;

namespace Rehably.Tests.Unit.Jobs;

public class UsageResetJobTests : IDisposable
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IClock> _clockMock;
    private readonly Mock<ILogger<UsageResetJob>> _loggerMock;
    private readonly ApplicationDbContext _context;
    private readonly UsageResetJob _sut;

    private readonly DateTime _now = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    public UsageResetJobTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _clockMock = new Mock<IClock>();
        _loggerMock = new Mock<ILogger<UsageResetJob>>();

        _clockMock.Setup(c => c.UtcNow).Returns(_now);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _context = PlatformTestHelpers.CreateInMemoryContext();

        _sut = new UsageResetJob(
            _context,
            _unitOfWorkMock.Object,
            _clockMock.Object,
            _loggerMock.Object);
    }

    public void Dispose() => _context.Dispose();

    private async Task<(Guid SubscriptionId, List<Guid> UsageIds)> SeedSubscriptionWithUsageAsync(int usedCount = 5)
    {
        var subscriptionId = Guid.NewGuid();
        var packageId = Guid.NewGuid();

        var subscription = new Subscription
        {
            Id = subscriptionId,
            ClinicId = Guid.NewGuid(),
            PackageId = packageId,
            Status = SubscriptionStatus.Active,
            PriceSnapshot = "{}",
            StartDate = _now.AddDays(-30),
            EndDate = _now.AddDays(30)
        };
        _context.Subscriptions.Add(subscription);

        var usageIds = new List<Guid>();
        for (int i = 0; i < 2; i++)
        {
            var feature = new Feature
            {
                Id = Guid.NewGuid(),
                Code = $"feature-{Guid.NewGuid()}",
                Name = $"Feature {i + 1}",
                CategoryId = Guid.NewGuid()
            };
            _context.Features.Add(feature);

            var usageId = Guid.NewGuid();
            var usage = new SubscriptionFeatureUsage
            {
                Id = usageId,
                SubscriptionId = subscriptionId,
                FeatureId = feature.Id,
                Used = usedCount,
                Limit = 100,
                LastResetAt = _now.AddDays(-30),
                CreatedAt = _now.AddDays(-30)
            };
            _context.SubscriptionFeatureUsages.Add(usage);
            usageIds.Add(usageId);
        }

        await _context.SaveChangesAsync();
        return (subscriptionId, usageIds);
    }

    [Fact]
    public async Task ResetUsage_ActiveSubscription_ResetsAllCountersToZero()
    {
        var (subscriptionId, usageIds) = await SeedSubscriptionWithUsageAsync(usedCount: 50);

        await _sut.ResetUsageCounters(subscriptionId);

        foreach (var usageId in usageIds)
        {
            var usage = await _context.SubscriptionFeatureUsages.FindAsync(usageId);
            usage!.Used.Should().Be(0);
        }
    }

    [Fact]
    public async Task ResetUsage_UpdatesLastResetAt()
    {
        var (subscriptionId, usageIds) = await SeedSubscriptionWithUsageAsync();

        await _sut.ResetUsageCounters(subscriptionId);

        foreach (var usageId in usageIds)
        {
            var usage = await _context.SubscriptionFeatureUsages.FindAsync(usageId);
            usage!.LastResetAt.Should().Be(_now);
        }

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
