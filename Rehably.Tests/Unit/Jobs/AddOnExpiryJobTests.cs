using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Rehably.Application.Interfaces;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;
using Rehably.Infrastructure.Jobs;
using Rehably.Tests.Helpers;

namespace Rehably.Tests.Unit.Jobs;

public class AddOnExpiryJobTests : IDisposable
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IClock> _clockMock;
    private readonly Mock<ILogger<AddOnExpiryJob>> _loggerMock;
    private readonly ApplicationDbContext _context;
    private readonly AddOnExpiryJob _sut;

    private readonly DateTime _now = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    public AddOnExpiryJobTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _clockMock = new Mock<IClock>();
        _loggerMock = new Mock<ILogger<AddOnExpiryJob>>();

        _clockMock.Setup(c => c.UtcNow).Returns(_now);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _context = PlatformTestHelpers.CreateInMemoryContext();

        var addOnServiceMock = new Mock<IAddOnService>();
        _sut = new AddOnExpiryJob(
            _context,
            addOnServiceMock.Object,
            _unitOfWorkMock.Object,
            _clockMock.Object,
            _loggerMock.Object);
    }

    public void Dispose() => _context.Dispose();

    private async Task SeedAddOnAsync(Guid id, AddOnStatus status, DateTime endDate)
    {
        var feature = new Feature
        {
            Id = Guid.NewGuid(),
            Name = "Test Feature",
            Code = $"test-{Guid.NewGuid()}",
            CategoryId = Guid.NewGuid()
        };
        var clinicId = Guid.NewGuid();
        var clinic = new Rehably.Domain.Entities.Tenant.Clinic
        {
            Id = clinicId,
            Name = "Test Clinic",
            Email = "test@test.com",
            Phone = "0100000000"
        };
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            PackageId = Guid.NewGuid(),
            PriceSnapshot = "{}",
            StartDate = _now.AddMonths(-1),
            EndDate = _now.AddMonths(11)
        };
        _context.Clinics.Add(clinic);
        _context.Features.Add(feature);
        _context.Subscriptions.Add(subscription);

        _context.SubscriptionAddOns.Add(new SubscriptionAddOn
        {
            Id = id,
            SubscriptionId = subscription.Id,
            FeatureId = feature.Id,
            Status = status,
            EndDate = endDate,
            CalculatedPrice = 0m,
            StartDate = _now.AddMonths(-1)
        });
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task Execute_ExpiredAddOns_SetsStatusExpired()
    {
        var addOnId = Guid.NewGuid();
        // EndDate is yesterday — should be expired
        await SeedAddOnAsync(addOnId, AddOnStatus.Active, _now.AddDays(-1));

        await _sut.ExecuteAsync();

        var addOn = await _context.SubscriptionAddOns.FindAsync(addOnId);
        addOn!.Status.Should().Be(AddOnStatus.Expired);
        addOn.UpdatedAt.Should().Be(_now);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Execute_ActiveAddOns_NotExpired_NoChange()
    {
        var addOnId = Guid.NewGuid();
        // EndDate is tomorrow — should NOT be expired
        await SeedAddOnAsync(addOnId, AddOnStatus.Active, _now.AddDays(1));

        await _sut.ExecuteAsync();

        var addOn = await _context.SubscriptionAddOns.FindAsync(addOnId);
        addOn!.Status.Should().Be(AddOnStatus.Active);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Execute_AlreadyExpiredAddOns_Skips()
    {
        var addOnId = Guid.NewGuid();
        // Add-on already has Expired status — job only targets Active
        await SeedAddOnAsync(addOnId, AddOnStatus.Expired, _now.AddDays(-5));

        await _sut.ExecuteAsync();

        var addOn = await _context.SubscriptionAddOns.FindAsync(addOnId);
        addOn!.Status.Should().Be(AddOnStatus.Expired); // Unchanged
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
