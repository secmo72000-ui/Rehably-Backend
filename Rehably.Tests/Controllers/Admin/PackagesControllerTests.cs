using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rehably.API.Controllers.Admin;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Package;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Entities.Platform;

namespace Rehably.Tests.Controllers.Admin;

public class PackagesControllerTests
{
    private readonly Mock<IPackageService> _packageServiceMock;
    private readonly PackagesController _sut;

    public PackagesControllerTests()
    {
        _packageServiceMock = new Mock<IPackageService>();
        _sut = new PackagesController(_packageServiceMock.Object);
    }

    #region GetPackages

    [Fact]
    public async Task GetPackages_WhenPackagesExist_ReturnsOkWithList()
    {
        var packages = new List<PackageDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Basic", Code = "basic", Status = PackageStatus.Active },
            new() { Id = Guid.NewGuid(), Name = "Pro", Code = "pro", Status = PackageStatus.Draft }
        };
        _packageServiceMock
            .Setup(x => x.GetPackagesAsync())
            .ReturnsAsync(Result<List<PackageDto>>.Success(packages));

        var result = await _sut.GetPackages();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPackages_WhenEmpty_ReturnsOkWithEmptyList()
    {
        _packageServiceMock
            .Setup(x => x.GetPackagesAsync())
            .ReturnsAsync(Result<List<PackageDto>>.Success(new List<PackageDto>()));

        var result = await _sut.GetPackages();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPackages_WhenServiceFails_ReturnsError()
    {
        _packageServiceMock
            .Setup(x => x.GetPackagesAsync())
            .ReturnsAsync(Result<List<PackageDto>>.Failure("Failed to retrieve packages"));

        var result = await _sut.GetPackages();

        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region GetPackage

    [Fact]
    public async Task GetPackage_WhenFound_ReturnsOk()
    {
        var packageId = Guid.NewGuid();
        var package = new PackageDto { Id = packageId, Name = "Basic", Code = "basic" };
        _packageServiceMock
            .Setup(x => x.GetPackageByIdAsync(packageId))
            .ReturnsAsync(Result<PackageDto>.Success(package));

        var result = await _sut.GetPackage(packageId);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPackage_WhenNotFound_Returns404()
    {
        var packageId = Guid.NewGuid();
        _packageServiceMock
            .Setup(x => x.GetPackageByIdAsync(packageId))
            .ReturnsAsync(Result<PackageDto>.Failure("Package not found"));

        var result = await _sut.GetPackage(packageId);

        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(404);
    }

    #endregion

    #region CreatePackage

    [Fact]
    public async Task CreatePackage_ValidRequest_Returns201()
    {
        var request = new CreatePackageRequestDto { Name = "Starter", Code = "starter" };
        var created = new PackageDto { Id = Guid.NewGuid(), Name = "Starter", Code = "starter" };
        _packageServiceMock
            .Setup(x => x.CreatePackageAsync(request))
            .ReturnsAsync(Result<PackageDto>.Success(created));

        var result = await _sut.CreatePackage(request);

        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task CreatePackage_DuplicateName_Returns409()
    {
        var request = new CreatePackageRequestDto { Name = "Basic", Code = "basic" };
        _packageServiceMock
            .Setup(x => x.CreatePackageAsync(request))
            .ReturnsAsync(Result<PackageDto>.Failure("Package with this name already exists"));

        var result = await _sut.CreatePackage(request);

        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task CreatePackage_InvalidRequest_Returns400()
    {
        var request = new CreatePackageRequestDto { Name = "", Code = "" };
        _packageServiceMock
            .Setup(x => x.CreatePackageAsync(request))
            .ReturnsAsync(Result<PackageDto>.Failure("Validation failed: Name is required"));

        var result = await _sut.CreatePackage(request);

        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region UpdatePackage

    [Fact]
    public async Task UpdatePackage_ValidRequest_ReturnsOk()
    {
        var packageId = Guid.NewGuid();
        var request = new UpdatePackageRequestDto { Name = "Updated Basic" };
        var updated = new PackageDto { Id = packageId, Name = "Updated Basic", Code = "basic" };
        _packageServiceMock
            .Setup(x => x.UpdatePackageAsync(packageId, request))
            .ReturnsAsync(Result<PackageDto>.Success(updated));

        var result = await _sut.UpdatePackage(packageId, request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdatePackage_WhenNotFound_Returns404()
    {
        var packageId = Guid.NewGuid();
        var request = new UpdatePackageRequestDto { Name = "Updated" };
        _packageServiceMock
            .Setup(x => x.UpdatePackageAsync(packageId, request))
            .ReturnsAsync(Result<PackageDto>.Failure("Package not found"));

        var result = await _sut.UpdatePackage(packageId, request);

        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(404);
    }

    #endregion

    #region ActivatePackage

    [Fact]
    public async Task ActivatePackage_DraftPackage_ReturnsOk()
    {
        var packageId = Guid.NewGuid();
        _packageServiceMock
            .Setup(x => x.ActivatePackageAsync(packageId))
            .ReturnsAsync(Result.Success());

        var result = await _sut.ActivatePackage(packageId);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ActivatePackage_AlreadyActive_Returns400()
    {
        var packageId = Guid.NewGuid();
        _packageServiceMock
            .Setup(x => x.ActivatePackageAsync(packageId))
            .ReturnsAsync(Result.Failure("Cannot activate: package is already active"));

        var result = await _sut.ActivatePackage(packageId);

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ActivatePackage_WhenNotFound_Returns404()
    {
        var packageId = Guid.NewGuid();
        _packageServiceMock
            .Setup(x => x.ActivatePackageAsync(packageId))
            .ReturnsAsync(Result.Failure("Package not found"));

        var result = await _sut.ActivatePackage(packageId);

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(404);
    }

    #endregion

    #region ArchivePackage

    [Fact]
    public async Task ArchivePackage_ActivePackage_ReturnsOk()
    {
        var packageId = Guid.NewGuid();
        _packageServiceMock
            .Setup(x => x.ArchivePackageAsync(packageId))
            .ReturnsAsync(Result.Success());

        var result = await _sut.ArchivePackage(packageId);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ArchivePackage_WhenNotFound_Returns404()
    {
        var packageId = Guid.NewGuid();
        _packageServiceMock
            .Setup(x => x.ArchivePackageAsync(packageId))
            .ReturnsAsync(Result.Failure("Package not found"));

        var result = await _sut.ArchivePackage(packageId);

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(404);
    }

    #endregion

}
