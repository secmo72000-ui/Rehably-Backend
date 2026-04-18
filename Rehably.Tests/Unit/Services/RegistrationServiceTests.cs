using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Auth;
using Rehably.Application.DTOs.Clinic;
using Rehably.Application.DTOs.Invoice;
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

public class RegistrationServiceTests
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
    private readonly Guid _packageId = Guid.NewGuid();

    public RegistrationServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _subscriptionRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Subscription>()))
            .ReturnsAsync((Subscription s) => s);
        _authPasswordServiceMock
            .Setup(a => a.GeneratePasswordResetTokenAsync(It.IsAny<string>()))
            .ReturnsAsync("reset-token");
        _onboardingRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ClinicOnboarding>()))
            .ReturnsAsync((ClinicOnboarding o) => o);
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

    private void SetupEmailNotInUse()
    {
        _clinicRepoMock
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Clinic, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    private void SetupEmailInUse()
    {
        _clinicRepoMock
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Clinic, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private void SetupRegisterClinicSuccess()
    {
        _clinicServiceMock
            .Setup(s => s.RegisterClinicAsync(It.IsAny<RegisterClinicRequest>()))
            .ReturnsAsync(Result<RegisterClinicResponse>.Success(new RegisterClinicResponse
            {
                Clinic = new ClinicResponse { Id = _clinicId, Name = "Test Clinic", Slug = "test-clinic" }
            }));
    }

    private void SetupOtpGenerateSuccess()
    {
        _otpServiceMock
            .Setup(s => s.GenerateOtpAsync(It.IsAny<string>(), OtpPurpose.EmailVerification))
            .ReturnsAsync(Result<string>.Success("123456"));
    }

    private ClinicOnboarding BuildOnboarding(OnboardingStep step = OnboardingStep.PendingEmailVerification) =>
        new()
        {
            Id = _onboardingId,
            ClinicId = _clinicId,
            CurrentStep = step,
            Clinic = new Clinic
            {
                Id = _clinicId,
                Name = "Test Clinic",
                Slug = "test-clinic",
                Email = "owner@test.com",
                Phone = "01000000000",
                Status = ClinicStatus.PendingEmailVerification
            }
        };

    // ── T051 Tests ─────────────────────────────────────────────────────────

    [Fact]
    public async Task StartRegistration_ValidRequest_CreatesClinicWithPendingEmailVerification()
    {
        SetupEmailNotInUse();
        SetupRegisterClinicSuccess();
        SetupOtpGenerateSuccess();
        _clinicRepoMock.Setup(r => r.GetByIdAsync(_clinicId)).ReturnsAsync(new Clinic
        {
            Id = _clinicId, Name = "Test Clinic", Slug = "test-clinic",
            Email = "owner@test.com", Phone = "01000000000"
        });

        var request = new StartRegistrationRequestDto(
            "Test Clinic", null, "owner@test.com", "01000000000", "John", "Doe");

        var result = await _service.StartRegistrationAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be("owner@test.com");
        result.Value.OtpExpiresIn.Should().BeGreaterThan(0);
        _onboardingRepoMock.Verify(r => r.AddAsync(It.Is<ClinicOnboarding>(
            o => o.CurrentStep == OnboardingStep.PendingEmailVerification)), Times.Once);
    }

    [Fact]
    public async Task StartRegistration_DuplicateEmail_ReturnsConflict()
    {
        SetupEmailInUse();

        var request = new StartRegistrationRequestDto(
            "Test Clinic", null, "existing@test.com", "01000000000", "John", "Doe");

        var result = await _service.StartRegistrationAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already registered");
    }

    [Fact]
    public async Task VerifyEmail_CorrectOtp_TransitionsToPendingDocumentsAndPackage()
    {
        var onboarding = BuildOnboarding(OnboardingStep.PendingEmailVerification);
        _onboardingRepoMock.Setup(r => r.GetWithClinicAsync(_onboardingId, default)).ReturnsAsync(onboarding);
        _otpServiceMock
            .Setup(s => s.VerifyOtpAsync("owner@test.com", "123456", OtpPurpose.EmailVerification))
            .ReturnsAsync(Result<OtpVerifyResult>.Success(new OtpVerifyResult { IsValid = true }));

        var result = await _service.VerifyRegistrationEmailAsync(_onboardingId, "123456");

        result.IsSuccess.Should().BeTrue();
        onboarding.CurrentStep.Should().Be(OnboardingStep.PendingDocumentsAndPackage);
        onboarding.EmailVerifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task VerifyEmail_WrongOtp_ReturnsError()
    {
        var onboarding = BuildOnboarding(OnboardingStep.PendingEmailVerification);
        _onboardingRepoMock.Setup(r => r.GetWithClinicAsync(_onboardingId, default)).ReturnsAsync(onboarding);
        _otpServiceMock
            .Setup(s => s.VerifyOtpAsync("owner@test.com", "wrong", OtpPurpose.EmailVerification))
            .ReturnsAsync(Result<OtpVerifyResult>.Success(new OtpVerifyResult { IsValid = false, AttemptsRemaining = 2 }));

        var result = await _service.VerifyRegistrationEmailAsync(_onboardingId, "wrong");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid OTP");
    }

    [Fact]
    public async Task SubmitGlobalPackage_ValidRequest_TransitionsToPendingApproval()
    {
        var onboarding = new ClinicOnboarding
        {
            Id = _onboardingId,
            ClinicId = _clinicId,
            CurrentStep = OnboardingStep.PendingDocumentsAndPackage
        };
        _onboardingRepoMock.Setup(r => r.GetByIdAsync(_onboardingId)).ReturnsAsync(onboarding);
        _packageRepoMock.Setup(r => r.GetByIdAsync(_packageId)).ReturnsAsync(new Package
        {
            Id = _packageId, Name = "Standard", Code = "standard",
            MonthlyPrice = 500, YearlyPrice = 5000, Status = PackageStatus.Active
        });
        _clinicRepoMock.Setup(r => r.GetByIdAsync(_clinicId)).ReturnsAsync(new Clinic
        {
            Id = _clinicId, Name = "Test Clinic", Status = ClinicStatus.PendingDocumentsAndPackage
        });

        var request = new SubmitGlobalPackageRequestDto(_packageId, BillingCycle.Monthly);

        var result = await _service.SubmitGlobalPackageAsync(_onboardingId, request);

        result.IsSuccess.Should().BeTrue();
        onboarding.CurrentStep.Should().Be(OnboardingStep.PendingApproval);
        onboarding.OnboardingType.Should().Be(OnboardingType.GlobalPackage);
        onboarding.SelectedPackageId.Should().Be(_packageId);
    }

    [Fact]
    public async Task SubmitGlobalPackage_InvalidPackageId_ReturnsNotFound()
    {
        var onboarding = new ClinicOnboarding
        {
            Id = _onboardingId,
            ClinicId = _clinicId,
            CurrentStep = OnboardingStep.PendingDocumentsAndPackage
        };
        _onboardingRepoMock.Setup(r => r.GetByIdAsync(_onboardingId)).ReturnsAsync(onboarding);
        _packageRepoMock.Setup(r => r.GetByIdAsync(_packageId)).ReturnsAsync((Package?)null);

        var request = new SubmitGlobalPackageRequestDto(_packageId, BillingCycle.Monthly);

        var result = await _service.SubmitGlobalPackageAsync(_onboardingId, request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Package not found");
    }

    [Fact]
    public async Task ApproveRegistration_PendingApproval_TransitionsToPendingPayment()
    {
        var onboarding = new ClinicOnboarding
        {
            Id = _onboardingId,
            ClinicId = _clinicId,
            CurrentStep = OnboardingStep.PendingApproval
        };
        _onboardingRepoMock.Setup(r => r.GetByClinicIdAsync(_clinicId, default)).ReturnsAsync(onboarding);
        _clinicRepoMock.Setup(r => r.GetByIdAsync(_clinicId)).ReturnsAsync(new Clinic
        {
            Id = _clinicId, Name = "Test Clinic", Status = ClinicStatus.PendingApproval
        });

        var result = await _service.ApproveRegistrationAsync(_clinicId, null, "admin-123");

        result.IsSuccess.Should().BeTrue();
        onboarding.CurrentStep.Should().Be(OnboardingStep.PendingPayment);
        onboarding.ApprovedBy.Should().Be("admin-123");
        onboarding.ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RejectRegistration_SetsRejectionReasonAndNotifies()
    {
        var clinic = new Clinic
        {
            Id = _clinicId, Name = "Test Clinic",
            Email = "owner@test.com", Phone = "01000000000",
            Status = ClinicStatus.PendingApproval
        };
        var onboarding = new ClinicOnboarding
        {
            Id = _onboardingId,
            ClinicId = _clinicId,
            CurrentStep = OnboardingStep.PendingApproval,
            Clinic = clinic
        };
        _onboardingRepoMock.Setup(r => r.GetByClinicIdAsync(_clinicId, default)).ReturnsAsync(onboarding);
        _onboardingRepoMock.Setup(r => r.GetWithClinicAsync(_onboardingId, default)).ReturnsAsync(onboarding);

        var result = await _service.RejectRegistrationAsync(_clinicId, "Incomplete documents", "admin-123");

        result.IsSuccess.Should().BeTrue();
        onboarding.RejectionReason.Should().Be("Incomplete documents");
        onboarding.CurrentStep.Should().Be(OnboardingStep.PendingDocumentsAndPackage);
        _emailServiceMock.Verify(
            s => s.SendWithDefaultProviderAsync(It.Is<Application.DTOs.Communication.EmailMessage>(
                m => m.To == "owner@test.com")),
            Times.Once);
    }
}
