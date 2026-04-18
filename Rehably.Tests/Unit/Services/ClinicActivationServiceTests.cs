using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinic;
using Rehably.Application.DTOs.Payment;
using Rehably.Application.DTOs.Subscription;
using Rehably.Application.Services.Auth;
using Rehably.Application.Services.Clinic;
using Rehably.Application.Services.Payment;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Services.Clinic;
using Xunit;

namespace Rehably.Tests.Unit.Services;

/// <summary>
/// TDD tests for ClinicActivationService.
/// T022: Cash payment path records payment and returns ClinicCreatedDto.
/// T023: Free payment path skips payment service entirely.
/// </summary>
public class ClinicActivationServiceTests
{
    private readonly Mock<IClinicService> _clinicServiceMock = new();
    private readonly Mock<ISubscriptionLifecycleService> _lifecycleServiceMock = new();
    private readonly Mock<IPaymentService> _paymentServiceMock = new();
    private readonly Mock<IAuthService> _authServiceMock = new();
    private readonly Mock<IAuthPasswordService> _authPasswordServiceMock = new();
    private readonly Mock<ILogger<ClinicActivationService>> _loggerMock = new();

    private readonly Guid _clinicId = Guid.NewGuid();
    private readonly Guid _subscriptionId = Guid.NewGuid();
    private readonly Guid _packageId = Guid.NewGuid();

    private ClinicActivationService CreateSut() => new(
        _clinicServiceMock.Object,
        _lifecycleServiceMock.Object,
        _paymentServiceMock.Object,
        _authServiceMock.Object,
        _authPasswordServiceMock.Object,
        _loggerMock.Object);

    private void SetupRegisterClinicSuccess()
    {
        _clinicServiceMock
            .Setup(s => s.CreateClinicAsync(It.IsAny<CreateClinicRequest>()))
            .ReturnsAsync(Result<ClinicResponse>.Success(new ClinicResponse
            {
                Id = _clinicId,
                Name = "Test Clinic",
                Slug = "test-clinic",
                Phone = "01000000000",
                Email = "owner@test.com",
                Status = ClinicStatus.PendingEmailVerification,
                SubscriptionStatus = SubscriptionStatus.Active,
                SubscriptionStartDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            }));
    }

    private void SetupCreateSubscriptionSuccess()
    {
        _lifecycleServiceMock
            .Setup(s => s.CreateSubscriptionAsync(It.IsAny<CreateSubscriptionRequestDto>()))
            .ReturnsAsync(Result<SubscriptionDetailDto>.Success(new SubscriptionDetailDto
            {
                Id = _subscriptionId,
                ClinicId = _clinicId,
                PackageId = _packageId,
                PackageName = "Starter",
                Status = SubscriptionStatus.Active,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1)
            }));
    }

    private void SetupCashPaymentSuccess()
    {
        var cashProviderMock = new Mock<IPaymentProvider>();
        cashProviderMock.Setup(p => p.Currency).Returns("USD");
        _paymentServiceMock
            .Setup(s => s.GetProvider("cash"))
            .Returns(cashProviderMock.Object);

        _paymentServiceMock
            .Setup(s => s.RecordCashPaymentAsync(
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>()))
            .ReturnsAsync(Result<CashPaymentResult>.Success(new CashPaymentResult("txn-123")));
    }

    private void SetupActivateClinicSuccess()
    {
        _clinicServiceMock
            .Setup(s => s.ActivateClinicAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Result.Success());
    }

    private void SetupSendWelcomeEmailSuccess()
    {
        _authPasswordServiceMock
            .Setup(s => s.GeneratePasswordResetTokenAsync(It.IsAny<string>()))
            .ReturnsAsync("reset-token");

        _authServiceMock
            .Setup(s => s.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
    }

    private CreateClinicRequest BuildRequest(PaymentType paymentType) => new()
    {
        ClinicName = "Test Clinic",
        Phone = "01000000000",
        OwnerEmail = "owner@test.com",
        OwnerFirstName = "John",
        OwnerLastName = "Doe",
        PackageId = _packageId,
        PaymentType = paymentType
    };

    [Fact]
    public async Task ClinicActivationService_CreateClinic_WithCashPayment_ReturnsClinicCreatedDto()
    {
        SetupRegisterClinicSuccess();
        SetupCreateSubscriptionSuccess();
        SetupCashPaymentSuccess();
        SetupActivateClinicSuccess();
        SetupSendWelcomeEmailSuccess();

        var sut = CreateSut();
        var request = BuildRequest(PaymentType.Cash);

        var result = await sut.ActivateNewClinicAsync(request);

        result.IsSuccess.Should().BeTrue("cash payment path should succeed");
        result.Value.Should().NotBeNull();
        result.Value!.PaymentType.Should().Be("Cash");
        result.Value.PaymentTransactionId.Should().Be("txn-123");
        result.Value.SubscriptionId.Should().Be(_subscriptionId);
        result.Value.Name.Should().Be("Test Clinic");

        _paymentServiceMock.Verify(
            s => s.RecordCashPaymentAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>()),
            Times.Once,
            "payment must be recorded exactly once for cash payment");
    }

    [Fact]
    public async Task ClinicActivationService_CreateClinic_WithFreePayment_NoPaymentRecorded()
    {
        SetupRegisterClinicSuccess();
        SetupCreateSubscriptionSuccess();
        SetupActivateClinicSuccess();
        SetupSendWelcomeEmailSuccess();

        var sut = CreateSut();
        var request = BuildRequest(PaymentType.Free);

        var result = await sut.ActivateNewClinicAsync(request);

        result.IsSuccess.Should().BeTrue("free payment path should succeed");
        result.Value.Should().NotBeNull();
        result.Value!.PaymentType.Should().Be("Free");
        result.Value.PaymentTransactionId.Should().BeNull("free clinics have no payment transaction");

        _paymentServiceMock.Verify(
            s => s.RecordCashPaymentAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>()),
            Times.Never,
            "payment service must never be called for free payment type");
    }

    [Fact]
    public async Task ClinicActivationService_CreateClinic_WhenRegisterFails_ReturnsFailure()
    {
        _clinicServiceMock
            .Setup(s => s.CreateClinicAsync(It.IsAny<CreateClinicRequest>()))
            .ReturnsAsync(Result<ClinicResponse>.Failure("Email already registered"));

        var sut = CreateSut();
        var request = BuildRequest(PaymentType.Cash);

        var result = await sut.ActivateNewClinicAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Email already registered");

        _lifecycleServiceMock.Verify(
            s => s.CreateSubscriptionAsync(It.IsAny<CreateSubscriptionRequestDto>()),
            Times.Never,
            "subsequent saga steps must not execute when a step fails");
    }

    [Fact]
    public async Task ClinicActivationService_CreateClinic_WhenSubscriptionFails_ReturnsFailure()
    {
        SetupRegisterClinicSuccess();

        _lifecycleServiceMock
            .Setup(s => s.CreateSubscriptionAsync(It.IsAny<CreateSubscriptionRequestDto>()))
            .ReturnsAsync(Result<SubscriptionDetailDto>.Failure("Package not found"));

        var sut = CreateSut();
        var request = BuildRequest(PaymentType.Cash);

        var result = await sut.ActivateNewClinicAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Package not found");

        _paymentServiceMock.Verify(
            s => s.RecordCashPaymentAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>()),
            Times.Never);
    }
}
