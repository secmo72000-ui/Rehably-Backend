using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinic;
using Rehably.Application.DTOs.Invoice;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Auth;
using Rehably.Application.Services.Clinic;
using Rehably.Application.Services.Communication;
using Rehably.Application.Services.Payment;
using Rehably.Application.Services.Platform;
using Rehably.Application.Interfaces;
using Rehably.Application.Services.Storage;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Services.Clinic;

namespace Rehably.Tests.Unit.Services;

public class ClinicOnboardingServiceTests
{
    private readonly Mock<IClinicOnboardingRepository> _onboardingRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IClinicService> _clinicServiceMock;
    private readonly Mock<IOtpService> _otpServiceMock;
    private readonly Mock<IDocumentService> _documentServiceMock;
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<ClinicOnboardingService>> _loggerMock;
    private readonly Mock<IClinicRepository> _clinicRepoMock;
    private readonly Mock<IPackageRepository> _packageRepoMock;
    private readonly Mock<IRepository<Subscription>> _subscriptionRepoMock;
    private readonly Mock<IInvoiceService> _invoiceServiceMock;
    private readonly Mock<IAuthPasswordService> _authPasswordServiceMock;
    private readonly Mock<IClock> _clockMock;
    private readonly ClinicOnboardingService _service;

    private readonly Guid _defaultClinicId = Guid.NewGuid();
    private readonly Guid _defaultPackageId = Guid.NewGuid();

    public ClinicOnboardingServiceTests()
    {
        _onboardingRepoMock = new Mock<IClinicOnboardingRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _clinicServiceMock = new Mock<IClinicService>();
        _otpServiceMock = new Mock<IOtpService>();
        _documentServiceMock = new Mock<IDocumentService>();
        _paymentServiceMock = new Mock<IPaymentService>();
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<ClinicOnboardingService>>();
        _clinicRepoMock = new Mock<IClinicRepository>();
        _packageRepoMock = new Mock<IPackageRepository>();
        _subscriptionRepoMock = new Mock<IRepository<Subscription>>();
        _invoiceServiceMock = new Mock<IInvoiceService>();
        _authPasswordServiceMock = new Mock<IAuthPasswordService>();
        _clockMock = new Mock<IClock>();

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _subscriptionRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Subscription>()))
            .ReturnsAsync((Subscription s) => s);
        _authPasswordServiceMock
            .Setup(a => a.GeneratePasswordResetTokenAsync(It.IsAny<string>()))
            .ReturnsAsync("reset-token-abc");
        _clockMock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

        _service = new ClinicOnboardingService(
            _onboardingRepoMock.Object,
            _unitOfWorkMock.Object,
            _clinicServiceMock.Object,
            _otpServiceMock.Object,
            _documentServiceMock.Object,
            _paymentServiceMock.Object,
            _emailServiceMock.Object,
            _loggerMock.Object,
            _clinicRepoMock.Object,
            _packageRepoMock.Object,
            _subscriptionRepoMock.Object,
            _invoiceServiceMock.Object,
            _authPasswordServiceMock.Object,
            _clockMock.Object);
    }

    private void SetupSlugAvailable()
    {
        _clinicRepoMock
            .Setup(r => r.IsSubdomainAvailableAsync(It.IsAny<string>(), It.IsAny<Guid?>()))
            .ReturnsAsync(true);
    }

    private void SetupSlugTaken()
    {
        _clinicRepoMock
            .Setup(r => r.IsSubdomainAvailableAsync(It.IsAny<string>(), It.IsAny<Guid?>()))
            .ReturnsAsync(false);
    }

    private void SetupRegisterClinicSuccess(Guid? clinicId = null)
    {
        var id = clinicId ?? _defaultClinicId;
        _clinicServiceMock
            .Setup(s => s.RegisterClinicAsync(It.IsAny<RegisterClinicRequest>()))
            .ReturnsAsync(Result<RegisterClinicResponse>.Success(new RegisterClinicResponse
            {
                Clinic = new ClinicResponse { Id = id, Name = "Test Clinic", Slug = "test-clinic" }
            }));
    }

    private void SetupClinicGet(Guid? clinicId = null)
    {
        var id = clinicId ?? _defaultClinicId;
        _clinicRepoMock
            .Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(new Clinic
            {
                Id = id,
                Name = "Test Clinic",
                Slug = "test-clinic",
                Phone = "01000000000",
                Status = ClinicStatus.PendingEmailVerification
            });
    }

    private void SetupPackageGet(Guid? packageId = null, int trialDays = 0)
    {
        var id = packageId ?? _defaultPackageId;
        _packageRepoMock
            .Setup(r => r.GetWithFeaturesAsync(id))
            .ReturnsAsync(new Package
            {
                Id = id,
                Name = "Standard Plan",
                Code = "standard",
                MonthlyPrice = 500m,
                YearlyPrice = 5000m,
                TrialDays = trialDays,
                Status = PackageStatus.Active
            });
    }

    private void SetupInvoiceSuccess()
    {
        _invoiceServiceMock
            .Setup(s => s.GenerateSubscriptionInvoiceAsync(
                It.IsAny<Subscription>(),
                It.IsAny<Package>(),
                It.IsAny<PaymentType>()))
            .ReturnsAsync(Result<InvoiceDto>.Success(new InvoiceDto
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = "INV-001",
                Amount = 500m,
                TaxAmount = 70m,
                TotalAmount = 570m
            }));
    }

    private AdminCreateClinicRequestDto BuildRequest(
        Guid? packageId = null,
        int trialDays = 0,
        BillingCycle billingCycle = BillingCycle.Monthly,
        bool useCustomPackage = false)
    {
        if (useCustomPackage)
        {
            return new AdminCreateClinicRequestDto
            {
                ClinicName = "Test Clinic",
                Slug = "test-clinic",
                Phone = "01000000000",
                OwnerFirstName = "John",
                OwnerLastName = "Doe",
                OwnerEmail = "owner@test.com",
                OwnerPhone = "01000000001",
                PackageId = null,
                CustomFeatures = new List<Application.DTOs.Package.PackageFeatureRequestDto>
                {
                    new() { FeatureId = Guid.NewGuid(), Limit = 50 }
                },
                CustomMonthlyPrice = 300m,
                CustomYearlyPrice = 3000m,
                BillingCycle = billingCycle,
                StartDate = new DateTime(2026, 3, 1),
                TrialDays = trialDays,
                AutoRenew = true,
                PaymentType = PaymentType.Cash
            };
        }

        return new AdminCreateClinicRequestDto
        {
            ClinicName = "Test Clinic",
            Slug = "test-clinic",
            Phone = "01000000000",
            OwnerFirstName = "John",
            OwnerLastName = "Doe",
            OwnerEmail = "owner@test.com",
            OwnerPhone = "01000000001",
            PackageId = packageId ?? _defaultPackageId,
            BillingCycle = billingCycle,
            StartDate = new DateTime(2026, 3, 1),
            TrialDays = trialDays,
            AutoRenew = true,
            PaymentType = PaymentType.Cash
        };
    }

    [Fact]
    public async Task AdminCreateClinic_ValidRequest_CreatesClinicWithActiveStatus()
    {
        SetupSlugAvailable();
        SetupRegisterClinicSuccess();
        SetupClinicGet();
        SetupPackageGet();
        SetupInvoiceSuccess();

        var request = BuildRequest();

        var result = await _service.AdminCreateClinicAsync(request, "admin-123");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Test Clinic");
        result.Value.Email.Should().Be("owner@test.com");
    }

    [Fact]
    public async Task AdminCreateClinic_DuplicateSlug_ReturnsConflict()
    {
        SetupSlugTaken();

        var request = BuildRequest();

        var result = await _service.AdminCreateClinicAsync(request, "admin-123");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Slug already exists");
    }

    [Fact]
    public async Task AdminCreateClinic_GlobalPackage_CreatesSubscriptionLinkedToPackage()
    {
        SetupSlugAvailable();
        SetupRegisterClinicSuccess();
        SetupClinicGet();
        SetupPackageGet();
        SetupInvoiceSuccess();

        Subscription? capturedSubscription = null;
        _subscriptionRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Subscription>()))
            .Callback<Subscription>(s => capturedSubscription = s)
            .ReturnsAsync((Subscription s) => s);

        var request = BuildRequest(packageId: _defaultPackageId);

        var result = await _service.AdminCreateClinicAsync(request, "admin-123");

        result.IsSuccess.Should().BeTrue();
        capturedSubscription.Should().NotBeNull();
        capturedSubscription!.PackageId.Should().Be(_defaultPackageId);
    }

    [Fact]
    public async Task AdminCreateClinic_CustomPackage_CreatesCustomPackageAndSubscription()
    {
        SetupSlugAvailable();
        SetupRegisterClinicSuccess();
        SetupClinicGet();
        SetupInvoiceSuccess();

        Package? capturedPackage = null;
        _packageRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Package>()))
            .Callback<Package>(p => capturedPackage = p)
            .ReturnsAsync((Package p) => p);

        var request = BuildRequest(useCustomPackage: true);

        var result = await _service.AdminCreateClinicAsync(request, "admin-123");

        result.IsSuccess.Should().BeTrue();
        capturedPackage.Should().NotBeNull();
        capturedPackage!.IsCustom.Should().BeTrue();
        capturedPackage.MonthlyPrice.Should().Be(300m);
        capturedPackage.YearlyPrice.Should().Be(3000m);
    }

    [Fact]
    public async Task AdminCreateClinic_WithTrialDays_SetsSubscriptionStatusTrial()
    {
        SetupSlugAvailable();
        SetupRegisterClinicSuccess();
        SetupClinicGet();
        SetupPackageGet(trialDays: 14);
        SetupInvoiceSuccess();

        Subscription? capturedSubscription = null;
        _subscriptionRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Subscription>()))
            .Callback<Subscription>(s => capturedSubscription = s)
            .ReturnsAsync((Subscription s) => s);

        var request = BuildRequest(trialDays: 14);

        var result = await _service.AdminCreateClinicAsync(request, "admin-123");

        result.IsSuccess.Should().BeTrue();
        capturedSubscription.Should().NotBeNull();
        capturedSubscription!.Status.Should().Be(SubscriptionStatus.Trial);
        capturedSubscription.TrialEndsAt.Should().NotBeNull();
    }

    [Fact]
    public async Task AdminCreateClinic_ZeroTrialDays_SetsSubscriptionStatusActive()
    {
        SetupSlugAvailable();
        SetupRegisterClinicSuccess();
        SetupClinicGet();
        SetupPackageGet(trialDays: 0);
        SetupInvoiceSuccess();

        Subscription? capturedSubscription = null;
        _subscriptionRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Subscription>()))
            .Callback<Subscription>(s => capturedSubscription = s)
            .ReturnsAsync((Subscription s) => s);

        var request = BuildRequest(trialDays: 0);

        var result = await _service.AdminCreateClinicAsync(request, "admin-123");

        result.IsSuccess.Should().BeTrue();
        capturedSubscription.Should().NotBeNull();
        capturedSubscription!.Status.Should().Be(SubscriptionStatus.Active);
        capturedSubscription.TrialEndsAt.Should().BeNull();
    }

    [Fact]
    public async Task AdminCreateClinic_GeneratesInvoice()
    {
        SetupSlugAvailable();
        SetupRegisterClinicSuccess();
        SetupClinicGet();
        SetupPackageGet();

        var invoiceId = Guid.NewGuid();
        _invoiceServiceMock
            .Setup(s => s.GenerateSubscriptionInvoiceAsync(
                It.IsAny<Subscription>(),
                It.IsAny<Package>(),
                It.IsAny<PaymentType>()))
            .ReturnsAsync(Result<InvoiceDto>.Success(new InvoiceDto
            {
                Id = invoiceId,
                InvoiceNumber = "INV-001",
                Amount = 500m,
                TaxAmount = 70m,
                TotalAmount = 570m
            }));

        var request = BuildRequest();

        var result = await _service.AdminCreateClinicAsync(request, "admin-123");

        result.IsSuccess.Should().BeTrue();

        _invoiceServiceMock.Verify(
            s => s.GenerateSubscriptionInvoiceAsync(
                It.IsAny<Subscription>(),
                It.IsAny<Package>(),
                It.IsAny<PaymentType>()),
            Times.Once);
    }
}
