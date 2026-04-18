using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.Logging;
using Moq;
using Rehably.Application.Interfaces;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Services.Platform;

namespace Rehably.Tests.Unit.Services;

public class TrialConversionTests
{
    private readonly Mock<ISubscriptionRepository> _subscriptionRepoMock;
    private readonly Mock<IInvoiceService> _invoiceServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPackageRepository> _packageRepoMock;
    private readonly Mock<IClinicRepository> _clinicRepoMock;
    private readonly Mock<IPricingService> _pricingServiceMock;
    private readonly Mock<IPlatformSubscriptionService> _platformSubServiceMock;
    private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock;
    private readonly Mock<ILogger<SubscriptionLifecycleService>> _loggerMock;
    private readonly Mock<IClock> _clockMock;
    private readonly SubscriptionLifecycleService _sut;

    public TrialConversionTests()
    {
        _subscriptionRepoMock = new Mock<ISubscriptionRepository>();
        _invoiceServiceMock = new Mock<IInvoiceService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _packageRepoMock = new Mock<IPackageRepository>();
        _clinicRepoMock = new Mock<IClinicRepository>();
        _pricingServiceMock = new Mock<IPricingService>();
        _platformSubServiceMock = new Mock<IPlatformSubscriptionService>();
        _backgroundJobClientMock = new Mock<IBackgroundJobClient>();
        _loggerMock = new Mock<ILogger<SubscriptionLifecycleService>>();
        _clockMock = new Mock<IClock>();

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _clockMock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

        _sut = new SubscriptionLifecycleService(
            _subscriptionRepoMock.Object,
            _packageRepoMock.Object,
            _clinicRepoMock.Object,
            _unitOfWorkMock.Object,
            _pricingServiceMock.Object,
            _platformSubServiceMock.Object,
            _backgroundJobClientMock.Object,
            _invoiceServiceMock.Object,
            _loggerMock.Object,
            _clockMock.Object);
    }

    private Subscription CreateTrialSubscription(Guid? id = null)
    {
        var packageId = Guid.NewGuid();
        return new Subscription
        {
            Id = id ?? Guid.NewGuid(),
            ClinicId = Guid.NewGuid(),
            PackageId = packageId,
            Status = SubscriptionStatus.Trial,
            TrialEndsAt = DateTime.UtcNow.AddDays(7),
            PriceSnapshot = "{}",
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddDays(23),
            Package = new Package
            {
                Id = packageId,
                Code = "trial-pkg",
                Name = "Trial Package"
            }
        };
    }

    [Fact]
    public async Task ConvertTrial_ValidTrialStatus_SetsStatusActive()
    {
        var subscription = CreateTrialSubscription();
        _subscriptionRepoMock
            .Setup(r => r.GetWithPackageAsync(subscription.Id))
            .ReturnsAsync(subscription);

        var result = await _sut.ConvertTrialAsync(subscription.Id, PaymentType.Free);

        result.IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.TrialEndsAt.Should().BeNull();
    }

    [Fact]
    public async Task ConvertTrial_NotTrialStatus_ReturnsError()
    {
        var subscription = CreateTrialSubscription();
        subscription.Status = SubscriptionStatus.Active;
        _subscriptionRepoMock
            .Setup(r => r.GetWithPackageAsync(subscription.Id))
            .ReturnsAsync(subscription);

        var result = await _sut.ConvertTrialAsync(subscription.Id, PaymentType.Free);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not in trial status");
    }

    [Fact]
    public async Task ConvertTrial_PaidPaymentType_GeneratesInvoice()
    {
        var subscription = CreateTrialSubscription();
        _subscriptionRepoMock
            .Setup(r => r.GetWithPackageAsync(subscription.Id))
            .ReturnsAsync(subscription);
        _invoiceServiceMock
            .Setup(i => i.GenerateInvoiceAsync(subscription.Id))
            .ReturnsAsync(Application.Common.Result<Application.DTOs.Invoice.InvoiceDto>.Success(null!));

        var result = await _sut.ConvertTrialAsync(subscription.Id, PaymentType.Online);

        result.IsSuccess.Should().BeTrue();
        _invoiceServiceMock.Verify(i => i.GenerateInvoiceAsync(subscription.Id), Times.Once);
    }

    [Fact]
    public async Task ConvertTrial_FreePaymentType_SkipsInvoiceGeneration()
    {
        var subscription = CreateTrialSubscription();
        _subscriptionRepoMock
            .Setup(r => r.GetWithPackageAsync(subscription.Id))
            .ReturnsAsync(subscription);

        var result = await _sut.ConvertTrialAsync(subscription.Id, PaymentType.Free);

        result.IsSuccess.Should().BeTrue();
        _invoiceServiceMock.Verify(i => i.GenerateInvoiceAsync(It.IsAny<Guid>()), Times.Never);
    }
}
