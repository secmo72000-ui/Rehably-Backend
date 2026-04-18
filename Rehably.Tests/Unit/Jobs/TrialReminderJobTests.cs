using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Rehably.Application.Interfaces;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;
using Rehably.Infrastructure.Jobs;
using Rehably.Application.Services.Platform;
using Rehably.Infrastructure.Services.Platform;

namespace Rehably.Tests.Unit.Jobs;

public class TrialReminderJobTests : IDisposable
{
    private readonly Mock<ISubscriptionNotificationService> _notificationServiceMock;
    private readonly Mock<IClock> _clockMock;
    private readonly Mock<ILogger<TrialReminderJob>> _loggerMock;
    private readonly ApplicationDbContext _context;
    private readonly TrialReminderJob _sut;

    private readonly DateTime _now = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    public TrialReminderJobTests()
    {
        _notificationServiceMock = new Mock<ISubscriptionNotificationService>();
        _clockMock = new Mock<IClock>();
        _loggerMock = new Mock<ILogger<TrialReminderJob>>();

        _clockMock.Setup(c => c.UtcNow).Returns(_now);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _sut = new TrialReminderJob(
            _context,
            _notificationServiceMock.Object,
            _clockMock.Object,
            _loggerMock.Object);
    }

    public void Dispose() => _context.Dispose();

    private async Task SeedTrialSubscriptionAsync(Guid subscriptionId, DateTime trialEndsAt, SubscriptionStatus status = SubscriptionStatus.Trial)
    {
        var clinic = new Rehably.Domain.Entities.Tenant.Clinic
        {
            Id = Guid.NewGuid(),
            Name = "Test Clinic",
            Slug = $"test-{Guid.NewGuid()}",
            Phone = "0100000000"
        };
        _context.Clinics.Add(clinic);

        var package = new Package
        {
            Id = Guid.NewGuid(),
            Code = $"pkg-{Guid.NewGuid()}",
            Name = "Trial Package"
        };
        _context.Packages.Add(package);

        var subscription = new Subscription
        {
            Id = subscriptionId,
            ClinicId = clinic.Id,
            PackageId = package.Id,
            Status = status,
            TrialEndsAt = trialEndsAt,
            PriceSnapshot = "{}",
            StartDate = _now.AddDays(-7),
            EndDate = trialEndsAt
        };
        _context.Subscriptions.Add(subscription);

        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task Execute_TrialEnding7Days_SendsReminder()
    {
        var subscriptionId = Guid.NewGuid();
        var trialEndsAt = _now.AddDays(7);
        await SeedTrialSubscriptionAsync(subscriptionId, trialEndsAt);

        await _sut.ExecuteAsync();

        _notificationServiceMock.Verify(
            n => n.SendTrialEndingReminderAsync(
                subscriptionId,
                7,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_TrialEnding1Day_SendsUrgentReminder()
    {
        var subscriptionId = Guid.NewGuid();
        var trialEndsAt = _now.AddDays(1);
        await SeedTrialSubscriptionAsync(subscriptionId, trialEndsAt);

        await _sut.ExecuteAsync();

        _notificationServiceMock.Verify(
            n => n.SendTrialEndingReminderAsync(
                subscriptionId,
                1,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_TrialNotEnding_NoNotification()
    {
        var subscriptionId = Guid.NewGuid();
        var trialEndsAt = _now.AddDays(14);
        await SeedTrialSubscriptionAsync(subscriptionId, trialEndsAt);

        await _sut.ExecuteAsync();

        _notificationServiceMock.Verify(
            n => n.SendTrialEndingReminderAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_NonTrialSubscription_Skips()
    {
        var subscriptionId = Guid.NewGuid();
        var trialEndsAt = _now.AddDays(7);
        await SeedTrialSubscriptionAsync(subscriptionId, trialEndsAt, SubscriptionStatus.Active);

        await _sut.ExecuteAsync();

        _notificationServiceMock.Verify(
            n => n.SendTrialEndingReminderAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
