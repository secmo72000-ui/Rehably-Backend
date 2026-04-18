using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Rehably.Application.Interfaces;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;
using Rehably.Infrastructure.Jobs;
using Rehably.Tests.Helpers;

namespace Rehably.Tests.Unit.Jobs;

public class DataDeletionJobTests : IDisposable
{
    private readonly Mock<IClinicRepository> _clinicRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IClock> _clockMock;
    private readonly Mock<ILogger<DataDeletionJob>> _loggerMock;
    private readonly ApplicationDbContext _context;
    private readonly DataDeletionJob _sut;

    private readonly DateTime _now = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    public DataDeletionJobTests()
    {
        _clinicRepoMock = new Mock<IClinicRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _clockMock = new Mock<IClock>();
        _loggerMock = new Mock<ILogger<DataDeletionJob>>();

        _clockMock.Setup(c => c.UtcNow).Returns(_now);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

        _context = PlatformTestHelpers.CreateInMemoryContext();

        _sut = new DataDeletionJob(
            _context,
            _clinicRepoMock.Object,
            _unitOfWorkMock.Object,
            _clockMock.Object,
            _loggerMock.Object);
    }

    public void Dispose() => _context.Dispose();

    private Clinic CreateSuspendedClinic(bool pastDeletionDate = true)
    {
        return new Clinic
        {
            Id = Guid.NewGuid(),
            Name = "Suspended Clinic",
            Slug = $"suspended-{Guid.NewGuid()}",
            Phone = "0100000000",
            Status = ClinicStatus.Suspended,
            SuspendedAt = _now.AddDays(-31),
            DataDeletionDate = pastDeletionDate ? _now.AddDays(-1) : _now.AddDays(7),
            DeletionStage = DeletionStage.NotStarted
        };
    }

    [Fact]
    public async Task Execute_EligibleClinic_DeletesAllData()
    {
        var clinic = CreateSuspendedClinic();
        _clinicRepoMock.Setup(r => r.GetByIdAsync(clinic.Id)).ReturnsAsync(clinic);
        _context.Clinics.Add(clinic);
        await _context.SaveChangesAsync();

        await _sut.DeleteClinicData(clinic.Id);

        clinic.DeletionStage.Should().Be(DeletionStage.Completed);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task Execute_ClinicNotSuspended_Skips()
    {
        var clinicId = Guid.NewGuid();
        var clinic = new Clinic
        {
            Id = clinicId,
            Name = "Active Clinic",
            Slug = "active-clinic",
            Phone = "0100000000",
            Status = ClinicStatus.Active
        };
        _clinicRepoMock.Setup(r => r.GetByIdAsync(clinicId)).ReturnsAsync(clinic);

        await _sut.DeleteClinicData(clinicId);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Execute_DeletionDateNotReached_Skips()
    {
        var clinic = CreateSuspendedClinic(pastDeletionDate: false);
        _clinicRepoMock.Setup(r => r.GetByIdAsync(clinic.Id)).ReturnsAsync(clinic);

        await _sut.DeleteClinicData(clinic.Id);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Execute_ReleasesSlug()
    {
        var clinic = CreateSuspendedClinic();
        var originalSlug = clinic.Slug;
        _clinicRepoMock.Setup(r => r.GetByIdAsync(clinic.Id)).ReturnsAsync(clinic);
        _context.Clinics.Add(clinic);
        await _context.SaveChangesAsync();

        await _sut.DeleteClinicData(clinic.Id);

        clinic.OriginalSlug.Should().Be(originalSlug);
        clinic.Slug.Should().StartWith("deleted_");
        clinic.Slug.Should().Contain(clinic.Id.ToString());
    }
}
