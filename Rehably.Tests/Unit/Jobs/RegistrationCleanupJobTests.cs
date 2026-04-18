using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Rehably.Application.Interfaces;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;
using Rehably.Infrastructure.Jobs;

namespace Rehably.Tests.Unit.Jobs;

public class RegistrationCleanupJobTests : IDisposable
{
    private readonly Mock<IClinicRepository> _clinicRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IClock> _clockMock;
    private readonly Mock<ILogger<RegistrationCleanupJob>> _loggerMock;
    private readonly ApplicationDbContext _context;
    private readonly RegistrationCleanupJob _sut;

    private readonly DateTime _now = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    public RegistrationCleanupJobTests()
    {
        _clinicRepoMock = new Mock<IClinicRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _clockMock = new Mock<IClock>();
        _loggerMock = new Mock<ILogger<RegistrationCleanupJob>>();

        _clockMock.Setup(c => c.UtcNow).Returns(_now);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
            .Returns(() => _context.SaveChangesAsync());

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _sut = new RegistrationCleanupJob(
            _context,
            _clinicRepoMock.Object,
            _unitOfWorkMock.Object,
            _clockMock.Object,
            _loggerMock.Object);
    }

    public void Dispose() => _context.Dispose();

    private async Task<Guid> SeedIncompleteClinicAsync(ClinicStatus status, DateTime createdAt)
    {
        var clinicId = Guid.NewGuid();
        var clinic = new Clinic
        {
            Id = clinicId,
            Name = "Incomplete Clinic",
            Slug = $"incomplete-{clinicId}",
            Phone = "0100000000",
            Status = status,
            CreatedAt = createdAt,
            IsDeleted = false
        };
        _context.Clinics.Add(clinic);

        var onboarding = new ClinicOnboarding
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            CurrentStep = (OnboardingStep)status,
            CreatedAt = createdAt
        };
        _context.ClinicOnboardings.Add(onboarding);

        await _context.SaveChangesAsync();
        return clinicId;
    }

    [Fact]
    public async Task Execute_IncompleteRegistration30DaysOld_DeletesClinicAndOnboarding()
    {
        var oldCreatedAt = _now.AddDays(-31);
        var clinicId = await SeedIncompleteClinicAsync(ClinicStatus.PendingEmailVerification, oldCreatedAt);

        await _sut.ExecuteAsync();

        var clinic = await _context.Clinics.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == clinicId);
        clinic.Should().BeNull("stale registration clinic should be deleted");

        var onboarding = await _context.ClinicOnboardings.IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.ClinicId == clinicId);
        onboarding.Should().BeNull("stale registration onboarding should be deleted");
    }

    [Fact]
    public async Task Execute_RecentRegistration_NoAction()
    {
        var recentCreatedAt = _now.AddDays(-5);
        var clinicId = await SeedIncompleteClinicAsync(ClinicStatus.PendingEmailVerification, recentCreatedAt);

        await _sut.ExecuteAsync();

        var clinic = await _context.Clinics.FindAsync(clinicId);
        clinic.Should().NotBeNull("recent registration should not be deleted");
    }

    [Fact]
    public async Task Execute_CompletedRegistration_NoAction()
    {
        var oldCreatedAt = _now.AddDays(-60);
        var clinicId = Guid.NewGuid();
        var clinic = new Clinic
        {
            Id = clinicId,
            Name = "Active Clinic",
            Slug = $"active-{clinicId}",
            Phone = "0100000000",
            Status = ClinicStatus.Active,
            CreatedAt = oldCreatedAt,
            IsDeleted = false
        };
        _context.Clinics.Add(clinic);
        await _context.SaveChangesAsync();

        await _sut.ExecuteAsync();

        var found = await _context.Clinics.FindAsync(clinicId);
        found.Should().NotBeNull("active clinic should not be deleted even if old");
    }
}
