using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rehably.API.Controllers.Admin;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Services.Library;
using Rehably.Domain.Enums;

namespace Rehably.Tests.Controllers.Admin;

public class AdminDevicesControllerTests
{
    private readonly Mock<IDeviceService> _deviceServiceMock;
    private readonly AdminDevicesController _sut;

    public AdminDevicesControllerTests()
    {
        _deviceServiceMock = new Mock<IDeviceService>();
        _sut = new AdminDevicesController(_deviceServiceMock.Object);
    }

    #region GetDevices

    [Fact]
    public async Task GetDevices_WhenServiceSucceeds_ReturnsOkWithPaginatedList()
    {
        var response = new LibraryItemListResponse<DeviceDto>
        {
            Items = new List<DeviceDto>
            {
                new() { Id = Guid.NewGuid(), Name = "TENS Unit", Manufacturer = "Acme" },
                new() { Id = Guid.NewGuid(), Name = "Ultrasound Machine", Manufacturer = "MedCo" }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };

        _deviceServiceMock
            .Setup(x => x.GetDevicesAsync(null, null, 1, 20))
            .ReturnsAsync(Result<LibraryItemListResponse<DeviceDto>>.Success(response));

        var result = await _sut.GetDevices();

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetDevices_WithBodyRegionFilter_PassesFilterToService()
    {
        var bodyRegionId = Guid.NewGuid();
        var response = new LibraryItemListResponse<DeviceDto>
        {
            Items = new List<DeviceDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };

        _deviceServiceMock
            .Setup(x => x.GetDevicesAsync(bodyRegionId, null, 1, 20))
            .ReturnsAsync(Result<LibraryItemListResponse<DeviceDto>>.Success(response));

        var result = await _sut.GetDevices(bodyRegionId);

        result.Result.Should().BeOfType<OkObjectResult>();
        _deviceServiceMock.Verify(x => x.GetDevicesAsync(bodyRegionId, null, 1, 20), Times.Once);
    }

    [Fact]
    public async Task GetDevices_WhenServiceFails_ReturnsBadRequest()
    {
        _deviceServiceMock
            .Setup(x => x.GetDevicesAsync(null, null, 1, 20))
            .ReturnsAsync(Result<LibraryItemListResponse<DeviceDto>>.Failure("Failed to retrieve devices"));

        var result = await _sut.GetDevices();

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetDevice

    [Fact]
    public async Task GetDevice_WhenFound_ReturnsOk()
    {
        var deviceId = Guid.NewGuid();
        var device = new DeviceDto
        {
            Id = deviceId,
            Name = "TENS Unit",
            Manufacturer = "Acme",
            AccessTier = LibraryAccessTier.Free
        };

        _deviceServiceMock
            .Setup(x => x.GetDeviceByIdAsync(deviceId))
            .ReturnsAsync(Result<DeviceDto>.Success(device));

        var result = await _sut.GetDevice(deviceId);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(device);
    }

    [Fact]
    public async Task GetDevice_WhenNotFound_ReturnsNotFound()
    {
        var deviceId = Guid.NewGuid();

        _deviceServiceMock
            .Setup(x => x.GetDeviceByIdAsync(deviceId))
            .ReturnsAsync(Result<DeviceDto>.Failure("Device not found"));

        var result = await _sut.GetDevice(deviceId);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region CreateDevice

    [Fact]
    public async Task CreateDevice_WhenValid_ReturnsCreatedAtAction()
    {
        var request = new CreateDeviceRequest
        {
            Name = "New Device",
            Manufacturer = "MedCo",
            Model = "X100"
        };

        var created = new DeviceDto
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Manufacturer = request.Manufacturer,
            Model = request.Model,
            ClinicId = null
        };

        _deviceServiceMock
            .Setup(x => x.CreateDeviceAsync(It.Is<CreateDeviceRequest>(r => r.Name == request.Name), null))
            .ReturnsAsync(Result<DeviceDto>.Success(created));

        var result = await _sut.CreateDevice(request, null);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult!.ActionName.Should().Be(nameof(_sut.GetDevice));
        createdResult.RouteValues!["id"].Should().Be(created.Id);
    }

    [Fact]
    public async Task CreateDevice_WithImageFile_SetsStreamProperties()
    {
        var request = new CreateDeviceRequest { Name = "Device With Image" };
        var fileMock = new Mock<IFormFile>();
        var memoryStream = new MemoryStream();
        fileMock.Setup(f => f.OpenReadStream()).Returns(memoryStream);
        fileMock.Setup(f => f.FileName).Returns("device.png");
        fileMock.Setup(f => f.ContentType).Returns("image/png");

        var created = new DeviceDto
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            ImageUrl = "https://cdn.example.com/device.png"
        };

        _deviceServiceMock
            .Setup(x => x.CreateDeviceAsync(
                It.Is<CreateDeviceRequest>(r =>
                    r.ImageStream != null &&
                    r.ImageFileName == "device.png" &&
                    r.ImageContentType == "image/png"),
                null))
            .ReturnsAsync(Result<DeviceDto>.Success(created));

        var result = await _sut.CreateDevice(request, fileMock.Object);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task CreateDevice_WhenServiceFails_ReturnsBadRequest()
    {
        var request = new CreateDeviceRequest { Name = "New Device" };

        _deviceServiceMock
            .Setup(x => x.CreateDeviceAsync(It.IsAny<CreateDeviceRequest>(), null))
            .ReturnsAsync(Result<DeviceDto>.Failure("Validation error"));

        var result = await _sut.CreateDevice(request, null);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region UpdateDevice

    [Fact]
    public async Task UpdateDevice_WhenValid_ReturnsOk()
    {
        var deviceId = Guid.NewGuid();
        var request = new UpdateDeviceRequest
        {
            Name = "Updated Device",
            Manufacturer = "Updated Mfg"
        };

        var updated = new DeviceDto
        {
            Id = deviceId,
            Name = request.Name,
            Manufacturer = request.Manufacturer
        };

        _deviceServiceMock
            .Setup(x => x.UpdateDeviceAsync(deviceId, It.Is<UpdateDeviceRequest>(r => r.Name == request.Name), Guid.Empty))
            .ReturnsAsync(Result<DeviceDto>.Success(updated));

        var result = await _sut.UpdateDevice(deviceId, request, null);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateDevice_WhenNotFound_ReturnsNotFound()
    {
        var deviceId = Guid.NewGuid();
        var request = new UpdateDeviceRequest { Name = "Updated" };

        _deviceServiceMock
            .Setup(x => x.UpdateDeviceAsync(deviceId, It.IsAny<UpdateDeviceRequest>(), Guid.Empty))
            .ReturnsAsync(Result<DeviceDto>.Failure("Device not found"));

        var result = await _sut.UpdateDevice(deviceId, request, null);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateDevice_WhenServiceFails_ReturnsBadRequest()
    {
        var deviceId = Guid.NewGuid();
        var request = new UpdateDeviceRequest { Name = "Updated" };

        _deviceServiceMock
            .Setup(x => x.UpdateDeviceAsync(deviceId, It.IsAny<UpdateDeviceRequest>(), Guid.Empty))
            .ReturnsAsync(Result<DeviceDto>.Failure("Validation error"));

        var result = await _sut.UpdateDevice(deviceId, request, null);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region DeleteDevice

    [Fact]
    public async Task DeleteDevice_WhenFound_ReturnsNoContent()
    {
        var deviceId = Guid.NewGuid();

        _deviceServiceMock
            .Setup(x => x.DeleteDeviceAsync(deviceId, Guid.Empty))
            .ReturnsAsync(Result.Success());

        var result = await _sut.DeleteDevice(deviceId);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteDevice_WhenNotFound_ReturnsNotFound()
    {
        var deviceId = Guid.NewGuid();

        _deviceServiceMock
            .Setup(x => x.DeleteDeviceAsync(deviceId, Guid.Empty))
            .ReturnsAsync(Result.Failure("Device not found"));

        var result = await _sut.DeleteDevice(deviceId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteDevice_WhenServiceFails_ReturnsBadRequest()
    {
        var deviceId = Guid.NewGuid();

        _deviceServiceMock
            .Setup(x => x.DeleteDeviceAsync(deviceId, Guid.Empty))
            .ReturnsAsync(Result.Failure("Cannot delete device in use"));

        var result = await _sut.DeleteDevice(deviceId);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion
}
