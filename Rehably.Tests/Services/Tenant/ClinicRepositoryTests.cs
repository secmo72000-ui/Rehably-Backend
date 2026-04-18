using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rehably.Application.Contexts;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;
using Rehably.Infrastructure.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Rehably.Tests.Services.Tenant;

/// <summary>
/// Service behavior tests for ClinicRepository
/// T068: Service Behavior Tests - Repository Pattern
/// </summary>
public class ClinicRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ClinicRepository _repository;

    public ClinicRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(t => t.TenantId).Returns((Guid?)null);
        _context = new ApplicationDbContext(options, null, mockTenantContext.Object);
        _repository = new ClinicRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private Clinic CreateClinic(string name, string slug, ClinicStatus status = ClinicStatus.Active)
    {
        return new Clinic
        {
            Name = name,
            Slug = slug,
            Email = $"{slug}@test.com",
            Phone = "+1234567890",
            Status = status
        };
    }

    [Fact]
    public async Task GetBySubdomainAsync_WithExistingSlug_ReturnsClinic()
    {
        // Arrange
        var clinic = CreateClinic("Test Clinic", "test-clinic");
        _context.Clinics.Add(clinic);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetBySubdomainAsync("test-clinic");

        // Assert
        result.Should().NotBeNull();
        result!.Slug.Should().Be("test-clinic");
    }

    [Fact]
    public async Task GetBySubdomainAsync_WithNonExistingSlug_ReturnsNull()
    {
        // Arrange & Act
        var result = await _repository.GetBySubdomainAsync("non-existing");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWithSubscriptionAsync_WithSubscription_IncludesSubscription()
    {
        // Arrange
        var clinic = CreateClinic("Test Clinic", "test-clinic");
        _context.Clinics.Add(clinic);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetWithSubscriptionAsync(clinic.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(clinic.Id);
    }

    [Fact]
    public async Task GetActiveClinicsAsync_WithActiveAndInactive_ReturnsOnlyActive()
    {
        // Arrange
        var activeClinic = CreateClinic("Active Clinic", "active-clinic", ClinicStatus.Active);
        var pendingClinic = CreateClinic("Pending Clinic", "pending-clinic", ClinicStatus.PendingEmailVerification);
        _context.Clinics.AddRange(activeClinic, pendingClinic);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveClinicsAsync();

        // Assert
        result.Should().ContainSingle();
        result.First().Slug.Should().Be("active-clinic");
    }

    [Fact]
    public async Task GetByStatusAsync_WithMatchingStatus_ReturnsFiltered()
    {
        // Arrange
        var activeClinic = CreateClinic("Active Clinic", "active-clinic", ClinicStatus.Active);
        var pendingClinic = CreateClinic("Pending Clinic", "pending-clinic", ClinicStatus.PendingEmailVerification);
        _context.Clinics.AddRange(activeClinic, pendingClinic);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStatusAsync(ClinicStatus.Active);

        // Assert
        result.Should().ContainSingle();
        result.First().Status.Should().Be(ClinicStatus.Active);
    }

    [Fact]
    public async Task IsSubdomainAvailableAsync_WithNewSubdomain_ReturnsTrue()
    {
        // Arrange & Act
        var result = await _repository.IsSubdomainAvailableAsync("new-clinic");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsSubdomainAvailableAsync_WithExistingSubdomain_ReturnsFalse()
    {
        // Arrange
        var clinic = CreateClinic("Test Clinic", "existing-clinic");
        _context.Clinics.Add(clinic);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.IsSubdomainAvailableAsync("existing-clinic");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsSubdomainAvailableAsync_WithExcludeId_ReturnsTrue()
    {
        // Arrange
        var clinic = CreateClinic("Test Clinic", "existing-clinic");
        _context.Clinics.Add(clinic);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.IsSubdomainAvailableAsync("existing-clinic", clinic.Id);

        // Assert
        result.Should().BeTrue();
    }
}
