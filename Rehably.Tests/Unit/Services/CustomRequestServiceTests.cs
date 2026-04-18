using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinic;
using Rehably.Application.DTOs.Invoice;
using Rehably.Application.DTOs.Package;
using Rehably.Application.DTOs.Registration;
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

public class CustomRequestServiceTests
{
    private readonly Mock<IClinicOnboardingRepository> _onboardingRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IClinicService> _clinicServiceMock = new();
    private readonly Mock<IOtpService> _otpServiceMock = new();
    private readonly Mock<IDocumentService> _documentServiceMock = new();
    private readonly Mock<IPaymentService> _paymentServiceMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<ILogger<ClinicOnboardingService>> _loggerMock = new();
    private readonly Mock<IClinicRepository> _clinicRepoMock = new();
    private readonly Mock<IPackageRepository> _packageRepoMock = new();
    private readonly Mock<IRepository<Subscription>> _subscriptionRepoMock = new();
    private readonly Mock<IInvoiceService> _invoiceServiceMock = new();
    private readonly Mock<IAuthPasswordService> _authPasswordServiceMock = new();
    private readonly Mock<IClock> _clockMock = new();

    private readonly ClinicOnboardingService _service;

    private readonly Guid _clinicId = Guid.NewGuid();
    private readonly Guid _onboardingId = Guid.NewGuid();
    private readonly Guid _featureId1 = Guid.NewGuid();
    private readonly Guid _featureId2 = Guid.NewGuid();

    public CustomRequestServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _subscriptionRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Subscription>()))
            .ReturnsAsync((Subscription s) => s);
        _onboardingRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ClinicOnboarding>()))
            .ReturnsAsync((ClinicOnboarding o) => o);
        _packageRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Package>()))
            .ReturnsAsync((Package p) => p);
        _invoiceServiceMock
            .Setup(s => s.GenerateSubscriptionInvoiceAsync(
                It.IsAny<Subscription>(),
                It.IsAny<Package>(),
                It.IsAny<PaymentType>()))
            .ReturnsAsync(Result<InvoiceDto>.Success(new InvoiceDto
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = "INV-001",
                Amount = 300m,
                TaxAmount = 42m,
                TotalAmount = 342m
            }));
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

    private ClinicOnboarding BuildOnboarding(OnboardingStep step) =>
        new()
        {
            Id = _onboardingId,
            ClinicId = _clinicId,
            CurrentStep = step
        };

    // ── T052 Tests ─────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitCustomRequest_ValidFeatures_TransitionsToPendingCustomPackageReview()
    {
        var onboarding = BuildOnboarding(OnboardingStep.PendingDocumentsAndPackage);
        _onboardingRepoMock.Setup(r => r.GetByIdAsync(_onboardingId)).ReturnsAsync(onboarding);
        _clinicRepoMock.Setup(r => r.GetByIdAsync(_clinicId)).ReturnsAsync(new Clinic
        {
            Id = _clinicId, Name = "Test Clinic", Status = ClinicStatus.PendingDocumentsAndPackage
        });

        var request = new SubmitCustomRequestDto(new List<Guid> { _featureId1, _featureId2 });

        var result = await _service.SubmitCustomRequestAsync(_onboardingId, request);

        result.IsSuccess.Should().BeTrue();
        onboarding.CurrentStep.Should().Be(OnboardingStep.PendingCustomPackageReview);
        onboarding.OnboardingType.Should().Be(OnboardingType.CustomRequest);
        onboarding.SelectedFeatures.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SubmitCustomRequest_EmptyFeatures_ReturnsValidationError()
    {
        var onboarding = BuildOnboarding(OnboardingStep.PendingDocumentsAndPackage);
        _onboardingRepoMock.Setup(r => r.GetByIdAsync(_onboardingId)).ReturnsAsync(onboarding);

        var request = new SubmitCustomRequestDto(new List<Guid>());

        var result = await _service.SubmitCustomRequestAsync(_onboardingId, request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("feature");
    }

    [Fact]
    public async Task ProcessCustomRequest_AdminSetsLimitsAndPrice_ActivatesClinic()
    {
        var onboarding = BuildOnboarding(OnboardingStep.PendingCustomPackageReview);
        _onboardingRepoMock.Setup(r => r.GetByClinicIdAsync(_clinicId, default)).ReturnsAsync(onboarding);
        _clinicRepoMock.Setup(r => r.GetByIdAsync(_clinicId)).ReturnsAsync(new Clinic
        {
            Id = _clinicId,
            Name = "Test Clinic",
            Slug = "test-clinic",
            Status = ClinicStatus.PendingCustomPackageReview
        });

        Package? capturedPackage = null;
        _packageRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Package>()))
            .Callback<Package>(p => capturedPackage = p)
            .ReturnsAsync((Package p) => p);

        Subscription? capturedSubscription = null;
        _subscriptionRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Subscription>()))
            .Callback<Subscription>(s => capturedSubscription = s)
            .ReturnsAsync((Subscription s) => s);

        var request = new ProcessCustomRequestDto(
            Features: new List<PackageFeatureRequestDto>
            {
                new() { FeatureId = _featureId1, Limit = 100 },
                new() { FeatureId = _featureId2, Limit = 50 }
            },
            MonthlyPrice: 300m,
            YearlyPrice: 3000m,
            BillingCycle: BillingCycle.Monthly,
            PaymentType: PaymentType.Cash);

        var result = await _service.ProcessCustomRequestAsync(_clinicId, request, "admin-456");

        result.IsSuccess.Should().BeTrue();

        capturedPackage.Should().NotBeNull();
        capturedPackage!.IsCustom.Should().BeTrue();
        capturedPackage.MonthlyPrice.Should().Be(300m);
        capturedPackage.YearlyPrice.Should().Be(3000m);
        capturedPackage.Features.Should().HaveCount(2);

        capturedSubscription.Should().NotBeNull();
        capturedSubscription!.Status.Should().Be(SubscriptionStatus.Active);
        capturedSubscription.BillingCycle.Should().Be(BillingCycle.Monthly);

        onboarding.CurrentStep.Should().Be(OnboardingStep.Completed);
        onboarding.ApprovedBy.Should().Be("admin-456");

        _invoiceServiceMock.Verify(
            s => s.GenerateSubscriptionInvoiceAsync(
                It.IsAny<Subscription>(),
                It.IsAny<Package>(),
                PaymentType.Cash),
            Times.Once);
    }
}
