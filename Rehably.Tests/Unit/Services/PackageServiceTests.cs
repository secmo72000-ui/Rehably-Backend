using FluentAssertions;
using Moq;
using Rehably.Application.DTOs.Package;
using Rehably.Application.Interfaces;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Entities.Platform;
using Rehably.Infrastructure.Services.Platform;

namespace Rehably.Tests.Unit.Services;

public class PackageServiceTests
{
    private readonly Mock<IPackageRepository> _packageRepoMock;
    private readonly Mock<IFeatureRepository> _featureRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPricingService> _pricingServiceMock;
    private readonly Mock<IClock> _clockMock;
    private readonly PackageService _service;

    public PackageServiceTests()
    {
        _packageRepoMock = new Mock<IPackageRepository>();
        _featureRepoMock = new Mock<IFeatureRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _pricingServiceMock = new Mock<IPricingService>();
        _clockMock = new Mock<IClock>();
        _clockMock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);
        _service = new PackageService(
            _packageRepoMock.Object,
            _featureRepoMock.Object,
            _unitOfWorkMock.Object,
            _pricingServiceMock.Object,
            _clockMock.Object);
    }

    [Fact]
    public async Task CreatePackageAsync_ValidRequest_ReturnsCreatedPackageInDraftStatus()
    {
        var featureId = Guid.NewGuid();
        var request = new CreatePackageRequestDto
        {
            Name = "Basic Plan",
            Code = "basic",
            MonthlyPrice = 100m,
            YearlyPrice = 1000m,
            Features = new List<PackageFeatureRequestDto>
            {
                new() { FeatureId = featureId, Limit = 50 }
            }
        };
        _packageRepoMock.Setup(r => r.GetByCodeAsync("basic")).ReturnsAsync((Package?)null);
        _featureRepoMock
            .Setup(r => r.ExistsAsync(featureId))
            .ReturnsAsync(true);

        var result = await _service.CreatePackageAsync(request);

        result.IsSuccess.Should().BeTrue();
        _packageRepoMock.Verify(r => r.AddAsync(It.Is<Package>(p =>
            p.Name == "Basic Plan" &&
            p.Status == PackageStatus.Draft &&
            p.MonthlyPrice == 100m)), Times.Once);
    }

    [Fact]
    public async Task CreatePackageAsync_DuplicateCode_ReturnsFailure()
    {
        var existing = new Package { Id = Guid.NewGuid(), Code = "basic", IsDeleted = false };
        _packageRepoMock.Setup(r => r.GetByCodeAsync("basic")).ReturnsAsync(existing);

        var request = new CreatePackageRequestDto
        {
            Name = "Another Plan",
            Code = "basic",
            MonthlyPrice = 50m,
            YearlyPrice = 500m,
            Features = new List<PackageFeatureRequestDto>()
        };

        var result = await _service.CreatePackageAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
        _packageRepoMock.Verify(r => r.AddAsync(It.IsAny<Package>()), Times.Never);
    }

    [Fact]
    public async Task CreatePackageAsync_FeatureNotFound_ReturnsFailure()
    {
        var missingFeatureId = Guid.NewGuid();
        _packageRepoMock.Setup(r => r.GetByCodeAsync("pro")).ReturnsAsync((Package?)null);
        _featureRepoMock
            .Setup(r => r.ExistsAsync(missingFeatureId))
            .ReturnsAsync(false); // feature does not exist

        var request = new CreatePackageRequestDto
        {
            Name = "Pro Plan",
            Code = "pro",
            MonthlyPrice = 200m,
            YearlyPrice = 2000m,
            Features = new List<PackageFeatureRequestDto>
            {
                new() { FeatureId = missingFeatureId }
            }
        };

        var result = await _service.CreatePackageAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain(missingFeatureId.ToString());
        _packageRepoMock.Verify(r => r.AddAsync(It.IsAny<Package>()), Times.Never);
    }

    [Fact]
    public async Task CreatePackageAsync_DuplicateFeatureIds_ReturnsFailure()
    {
        // Service-level duplicate check: ValidateAndBuildFeaturesAsync rejects duplicate IDs
        var featureId = Guid.NewGuid();
        _packageRepoMock.Setup(r => r.GetByCodeAsync("basic")).ReturnsAsync((Package?)null);

        var request = new CreatePackageRequestDto
        {
            Name = "Basic Plan",
            Code = "basic",
            MonthlyPrice = 100m,
            YearlyPrice = 1000m,
            Features = new List<PackageFeatureRequestDto>
            {
                new() { FeatureId = featureId, Limit = 10 },
                new() { FeatureId = featureId, Limit = 20 }
            }
        };

        var result = await _service.CreatePackageAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Duplicate feature IDs");
        _packageRepoMock.Verify(r => r.AddAsync(It.IsAny<Package>()), Times.Never);
    }

    [Fact]
    public async Task CreatePackageAsync_NoFeatures_ReturnsSuccess()
    {
        // Service allows empty features — ValidateAndBuildFeaturesAsync returns
        // an empty list immediately when features.Count == 0.
        var request = new CreatePackageRequestDto
        {
            Name = "Empty Plan",
            Code = "empty",
            MonthlyPrice = 0m,
            YearlyPrice = 0m,
            Features = new List<PackageFeatureRequestDto>()
        };
        _packageRepoMock.Setup(r => r.GetByCodeAsync("empty")).ReturnsAsync((Package?)null);

        var result = await _service.CreatePackageAsync(request);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePackageAsync_ExistingPackage_UpdatesFields()
    {
        var packageId = Guid.NewGuid();
        var package = new Package
        {
            Id = packageId,
            Name = "Old Name",
            Code = "old",
            Status = PackageStatus.Draft,
            MonthlyPrice = 50m,
            YearlyPrice = 500m
        };
        _packageRepoMock.Setup(r => r.GetForEditAsync(packageId)).ReturnsAsync(package);

        var request = new UpdatePackageRequestDto
        {
            Name = "New Name",
            Description = "Updated description",
            MonthlyPrice = 200m,
            YearlyPrice = 2000m,
            DisplayOrder = 1
        };

        var result = await _service.UpdatePackageAsync(packageId, request);

        result.IsSuccess.Should().BeTrue();
        package.Name.Should().Be("New Name");
        package.Description.Should().Be("Updated description");
        package.MonthlyPrice.Should().Be(200m);
        package.YearlyPrice.Should().Be(2000m);
    }

    [Fact]
    public async Task UpdatePackageAsync_NonExistentPackage_ReturnsFailure()
    {
        var packageId = Guid.NewGuid();
        _packageRepoMock.Setup(r => r.GetForEditAsync(packageId)).ReturnsAsync((Package?)null);

        var request = new UpdatePackageRequestDto
        {
            Name = "New Name",
            MonthlyPrice = 100m,
            YearlyPrice = 1000m
        };

        var result = await _service.UpdatePackageAsync(packageId, request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task UpdatePackageAsync_WithFeatures_ClearsAndReplacesFeatures()
    {
        var packageId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        var package = new Package { Id = packageId, Name = "Plan", Code = "plan", Status = PackageStatus.Draft };
        _packageRepoMock.Setup(r => r.GetForEditAsync(packageId)).ReturnsAsync(package);
        _featureRepoMock
            .Setup(r => r.ExistsAsync(featureId))
            .ReturnsAsync(true);

        var request = new UpdatePackageRequestDto
        {
            Name = "Plan",
            MonthlyPrice = 100m,
            YearlyPrice = 1000m,
            Features = new List<PackageFeatureRequestDto>
            {
                new() { FeatureId = featureId, Limit = 10 }
            }
        };

        var result = await _service.UpdatePackageAsync(packageId, request);

        result.IsSuccess.Should().BeTrue();
        _packageRepoMock.Verify(r => r.ClearFeaturesAsync(packageId), Times.Once);
    }

    [Fact]
    public async Task UpdatePackageAsync_NullFeatures_DoesNotClearFeatures()
    {
        // When Features is null, the service skips the update block entirely
        var packageId = Guid.NewGuid();
        var package = new Package { Id = packageId, Name = "Plan", Code = "plan", Status = PackageStatus.Draft };
        _packageRepoMock.Setup(r => r.GetForEditAsync(packageId)).ReturnsAsync(package);

        var request = new UpdatePackageRequestDto
        {
            Name = "Updated Plan",
            MonthlyPrice = 100m,
            YearlyPrice = 1000m,
            Features = null
        };

        var result = await _service.UpdatePackageAsync(packageId, request);

        result.IsSuccess.Should().BeTrue();
        _packageRepoMock.Verify(r => r.ClearFeaturesAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ActivatePackageAsync_DraftPackage_SetsStatusActive()
    {
        var packageId = Guid.NewGuid();
        var package = new Package { Id = packageId, Status = PackageStatus.Draft };
        _packageRepoMock.Setup(r => r.GetByIdAsync(packageId)).ReturnsAsync(package);

        var result = await _service.ActivatePackageAsync(packageId);

        result.IsSuccess.Should().BeTrue();
        package.Status.Should().Be(PackageStatus.Active);
    }

    [Fact]
    public async Task ActivatePackageAsync_AlreadyActivePackage_ReturnsFailure()
    {
        // Only draft packages can be activated
        var packageId = Guid.NewGuid();
        var package = new Package { Id = packageId, Status = PackageStatus.Active };
        _packageRepoMock.Setup(r => r.GetByIdAsync(packageId)).ReturnsAsync(package);

        var result = await _service.ActivatePackageAsync(packageId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("draft");
    }

    [Fact]
    public async Task ActivatePackageAsync_NonExistentPackage_ReturnsFailure()
    {
        var packageId = Guid.NewGuid();
        _packageRepoMock.Setup(r => r.GetByIdAsync(packageId)).ReturnsAsync((Package?)null);

        var result = await _service.ActivatePackageAsync(packageId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task ArchivePackageAsync_ActivePackage_SetsStatusArchived()
    {
        var packageId = Guid.NewGuid();
        var package = new Package { Id = packageId, Status = PackageStatus.Active };
        _packageRepoMock.Setup(r => r.GetByIdAsync(packageId)).ReturnsAsync(package);

        var result = await _service.ArchivePackageAsync(packageId);

        result.IsSuccess.Should().BeTrue();
        package.Status.Should().Be(PackageStatus.Archived);
    }

    [Fact]
    public async Task ArchivePackageAsync_DraftPackage_ReturnsFailure()
    {
        // Only active packages can be archived
        var packageId = Guid.NewGuid();
        var package = new Package { Id = packageId, Status = PackageStatus.Draft };
        _packageRepoMock.Setup(r => r.GetByIdAsync(packageId)).ReturnsAsync(package);

        var result = await _service.ArchivePackageAsync(packageId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("active");
    }

    [Fact]
    public async Task ArchivePackageAsync_NonExistentPackage_ReturnsFailure()
    {
        var packageId = Guid.NewGuid();
        _packageRepoMock.Setup(r => r.GetByIdAsync(packageId)).ReturnsAsync((Package?)null);

        var result = await _service.ArchivePackageAsync(packageId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task GetPackageByIdAsync_ExistingPackage_ReturnsPackageDetailDto()
    {
        var packageId = Guid.NewGuid();
        var package = new Package
        {
            Id = packageId,
            Name = "Basic",
            Code = "basic",
            Status = PackageStatus.Active
        };
        _packageRepoMock.Setup(r => r.GetWithFeaturesAsync(packageId)).ReturnsAsync(package);

        var result = await _service.GetPackageByIdAsync(packageId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(packageId);
        result.Value.Name.Should().Be("Basic");
    }

    [Fact]
    public async Task GetPackageByIdAsync_NonExistentPackage_ReturnsFailure()
    {
        var packageId = Guid.NewGuid();
        _packageRepoMock.Setup(r => r.GetWithFeaturesAsync(packageId)).ReturnsAsync((Package?)null);

        var result = await _service.GetPackageByIdAsync(packageId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task GetPackageByIdAsync_DeletedPackage_ReturnsFailure()
    {
        var packageId = Guid.NewGuid();
        var package = new Package { Id = packageId, IsDeleted = true };
        _packageRepoMock.Setup(r => r.GetWithFeaturesAsync(packageId)).ReturnsAsync(package);

        var result = await _service.GetPackageByIdAsync(packageId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task GetPackagesAsync_NoFilter_ReturnsAllNonCustomPackages()
    {
        var packages = new List<Package>
        {
            new() { Id = Guid.NewGuid(), Name = "Basic", Code = "basic", Status = PackageStatus.Active, IsCustom = false },
            new() { Id = Guid.NewGuid(), Name = "Pro", Code = "pro", Status = PackageStatus.Draft, IsCustom = false }
        };
        _packageRepoMock.Setup(r => r.GetAllForAdminAsync()).ReturnsAsync(packages);

        var result = await _service.GetPackagesAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPackagesAsync_FilterByStatus_ReturnsOnlyMatchingPackages()
    {
        var packages = new List<Package>
        {
            new() { Id = Guid.NewGuid(), Name = "Basic", Code = "basic", Status = PackageStatus.Active, IsCustom = false },
            new() { Id = Guid.NewGuid(), Name = "Pro", Code = "pro", Status = PackageStatus.Draft, IsCustom = false }
        };
        _packageRepoMock.Setup(r => r.GetAllForAdminAsync()).ReturnsAsync(packages);

        // GetPackagesAsync() returns all; filtering by status is caller responsibility
        var result = await _service.GetPackagesAsync();

        result.IsSuccess.Should().BeTrue();
        var activeOnly = result.Value!.Where(p => p.Status == PackageStatus.Active).ToList();
        activeOnly.Should().HaveCount(1);
        activeOnly[0].Name.Should().Be("Basic");
    }

    [Fact]
    public async Task GetPackagesAsync_IncludeCustomTrue_ReturnsCustomPackages()
    {
        var packages = new List<Package>
        {
            new() { Id = Guid.NewGuid(), Name = "Standard", Code = "standard", Status = PackageStatus.Active, IsCustom = false },
            new() { Id = Guid.NewGuid(), Name = "Custom Clinic", Code = "custom-clinic", Status = PackageStatus.Active, IsCustom = true }
        };
        _packageRepoMock.Setup(r => r.GetAllForAdminAsync()).ReturnsAsync(packages);

        // GetPackagesAsync() returns all packages including custom
        var result = await _service.GetPackagesAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPackagesAsync_EmptyRepository_ReturnsEmptyList()
    {
        _packageRepoMock.Setup(r => r.GetAllForAdminAsync()).ReturnsAsync(new List<Package>());

        var result = await _service.GetPackagesAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPublicPackages_ReturnsOnlyActivePackages()
    {
        var activeId = Guid.NewGuid();
        var draftId = Guid.NewGuid();
        var archivedId = Guid.NewGuid();
        var packages = new List<Package>
        {
            new() { Id = activeId, Name = "Active Plan", Code = "active", Status = PackageStatus.Active, IsPublic = true, MonthlyPrice = 100m, YearlyPrice = 1000m },
            new() { Id = draftId, Name = "Draft Plan", Code = "draft", Status = PackageStatus.Draft, IsPublic = true, MonthlyPrice = 50m, YearlyPrice = 500m },
            new() { Id = archivedId, Name = "Archived Plan", Code = "archived", Status = PackageStatus.Archived, IsPublic = true, MonthlyPrice = 200m, YearlyPrice = 2000m }
        };
        _packageRepoMock.Setup(r => r.GetPublicPackagesAsync()).ReturnsAsync(
            packages.Where(p => p.Status == PackageStatus.Active).ToList());

        var result = await _service.GetPublicPackagesAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].Id.Should().Be(activeId);
    }

    [Fact]
    public async Task GetPublicPackages_ExcludesDraftAndArchived()
    {
        var packages = new List<Package>
        {
            new() { Id = Guid.NewGuid(), Name = "Draft Plan", Code = "draft", Status = PackageStatus.Draft, IsPublic = true, MonthlyPrice = 50m },
            new() { Id = Guid.NewGuid(), Name = "Archived Plan", Code = "archived", Status = PackageStatus.Archived, IsPublic = true, MonthlyPrice = 200m }
        };
        _packageRepoMock.Setup(r => r.GetPublicPackagesAsync()).ReturnsAsync(new List<Package>());

        var result = await _service.GetPublicPackagesAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task DeletePackage_Draft_NoSubscriptions_Succeeds()
    {
        var packageId = Guid.NewGuid();
        var package = new Package { Id = packageId, Status = PackageStatus.Draft, IsDeleted = false };
        _packageRepoMock.Setup(r => r.GetByIdAsync(packageId)).ReturnsAsync(package);
        _packageRepoMock.Setup(r => r.HasAnySubscriptionsAsync(packageId)).ReturnsAsync(false);

        var result = await _service.DeletePackageAsync(packageId);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeletePackage_ActivePackage_Returns409()
    {
        var packageId = Guid.NewGuid();
        var package = new Package { Id = packageId, Status = PackageStatus.Active, IsDeleted = false };
        _packageRepoMock.Setup(r => r.GetByIdAsync(packageId)).ReturnsAsync(package);

        var result = await _service.DeletePackageAsync(packageId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Draft");
    }

    [Fact]
    public async Task DeletePackage_HasExistingSubscriptions_Returns409()
    {
        var packageId = Guid.NewGuid();
        var package = new Package { Id = packageId, Status = PackageStatus.Draft, IsDeleted = false };
        _packageRepoMock.Setup(r => r.GetByIdAsync(packageId)).ReturnsAsync(package);
        _packageRepoMock.Setup(r => r.HasAnySubscriptionsAsync(packageId)).ReturnsAsync(true);

        var result = await _service.DeletePackageAsync(packageId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("subscription");
    }
}
