using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rehably.API.Controllers.Admin;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Services.Library;
using Rehably.Domain.Enums;

namespace Rehably.Tests.Controllers.Admin;

public class AdminModalitiesControllerTests
{
    private readonly Mock<IModalityService> _modalityServiceMock;
    private readonly AdminModalitiesController _sut;

    public AdminModalitiesControllerTests()
    {
        _modalityServiceMock = new Mock<IModalityService>();
        _sut = new AdminModalitiesController(_modalityServiceMock.Object);
    }

    #region GetModalities

    [Fact]
    public async Task GetModalities_WhenServiceSucceeds_ReturnsOkWithPaginatedList()
    {
        var response = new LibraryItemListResponse<ModalityDto>
        {
            Items = new List<ModalityDto>
            {
                new() { Id = Guid.NewGuid(), Name = "Ultrasound", Code = "MOD001", ModalityType = ModalityType.Mechanical },
                new() { Id = Guid.NewGuid(), Name = "TENS", Code = "MOD002", ModalityType = ModalityType.Electrotherapy }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };

        _modalityServiceMock
            .Setup(x => x.GetModalitiesAsync(null, null, 1, 20))
            .ReturnsAsync(Result<LibraryItemListResponse<ModalityDto>>.Success(response));

        var result = await _sut.GetModalities();

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetModalities_WithBodyRegionFilter_PassesFilterToService()
    {
        var bodyRegionId = Guid.NewGuid();
        var response = new LibraryItemListResponse<ModalityDto>
        {
            Items = new List<ModalityDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };

        _modalityServiceMock
            .Setup(x => x.GetModalitiesAsync(bodyRegionId, null, 1, 20))
            .ReturnsAsync(Result<LibraryItemListResponse<ModalityDto>>.Success(response));

        var result = await _sut.GetModalities(bodyRegionId);

        result.Result.Should().BeOfType<OkObjectResult>();
        _modalityServiceMock.Verify(x => x.GetModalitiesAsync(bodyRegionId, null, 1, 20), Times.Once);
    }

    [Fact]
    public async Task GetModalities_WhenServiceFails_ReturnsBadRequest()
    {
        _modalityServiceMock
            .Setup(x => x.GetModalitiesAsync(null, null, 1, 20))
            .ReturnsAsync(Result<LibraryItemListResponse<ModalityDto>>.Failure("Failed to retrieve modalities"));

        var result = await _sut.GetModalities();

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetModality

    [Fact]
    public async Task GetModality_WhenFound_ReturnsOk()
    {
        var modalityId = Guid.NewGuid();
        var modality = new ModalityDto
        {
            Id = modalityId,
            Name = "Ultrasound",
            Code = "MOD001",
            ModalityType = ModalityType.Mechanical,
            TherapeuticCategory = "Deep Heat",
            MainGoal = "Pain relief",
            AccessTier = LibraryAccessTier.Free
        };

        _modalityServiceMock
            .Setup(x => x.GetModalityByIdAsync(modalityId))
            .ReturnsAsync(Result<ModalityDto>.Success(modality));

        var result = await _sut.GetModality(modalityId);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(modality);
    }

    [Fact]
    public async Task GetModality_WhenNotFound_ReturnsNotFound()
    {
        var modalityId = Guid.NewGuid();

        _modalityServiceMock
            .Setup(x => x.GetModalityByIdAsync(modalityId))
            .ReturnsAsync(Result<ModalityDto>.Failure("Modality not found"));

        var result = await _sut.GetModality(modalityId);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region CreateModality

    [Fact]
    public async Task CreateModality_WhenValid_ReturnsCreatedAtAction()
    {
        var request = new CreateModalityRequest
        {
            Name = "New Modality",
            Code = "MOD003",
            ModalityType = ModalityType.Thermal,
            TherapeuticCategory = "Thermal",
            MainGoal = "Increase circulation"
        };

        var created = new ModalityDto
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Code = request.Code,
            ModalityType = request.ModalityType,
            TherapeuticCategory = request.TherapeuticCategory,
            MainGoal = request.MainGoal,
            ClinicId = null
        };

        _modalityServiceMock
            .Setup(x => x.CreateModalityAsync(request, null))
            .ReturnsAsync(Result<ModalityDto>.Success(created));

        var result = await _sut.CreateModality(request);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult!.ActionName.Should().Be(nameof(_sut.GetModality));
        createdResult.RouteValues!["id"].Should().Be(created.Id);
    }

    [Fact]
    public async Task CreateModality_WhenServiceFails_ReturnsBadRequest()
    {
        var request = new CreateModalityRequest
        {
            Name = "New Modality",
            Code = "MOD003",
            ModalityType = ModalityType.Thermal,
            TherapeuticCategory = "Thermal",
            MainGoal = "Pain relief"
        };

        _modalityServiceMock
            .Setup(x => x.CreateModalityAsync(request, null))
            .ReturnsAsync(Result<ModalityDto>.Failure("Duplicate code"));

        var result = await _sut.CreateModality(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region UpdateModality

    [Fact]
    public async Task UpdateModality_WhenValid_ReturnsOk()
    {
        var modalityId = Guid.NewGuid();
        var request = new UpdateModalityRequest
        {
            Name = "Updated Modality",
            ModalityType = ModalityType.Electrotherapy,
            TherapeuticCategory = "Updated Category",
            MainGoal = "Updated goal"
        };

        var updated = new ModalityDto
        {
            Id = modalityId,
            Name = request.Name,
            ModalityType = request.ModalityType,
            TherapeuticCategory = request.TherapeuticCategory,
            MainGoal = request.MainGoal
        };

        _modalityServiceMock
            .Setup(x => x.UpdateModalityAsync(modalityId, request, Guid.Empty))
            .ReturnsAsync(Result<ModalityDto>.Success(updated));

        var result = await _sut.UpdateModality(modalityId, request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateModality_WhenNotFound_ReturnsNotFound()
    {
        var modalityId = Guid.NewGuid();
        var request = new UpdateModalityRequest
        {
            Name = "Updated",
            TherapeuticCategory = "Cat",
            MainGoal = "Goal"
        };

        _modalityServiceMock
            .Setup(x => x.UpdateModalityAsync(modalityId, request, Guid.Empty))
            .ReturnsAsync(Result<ModalityDto>.Failure("Modality not found"));

        var result = await _sut.UpdateModality(modalityId, request);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateModality_WhenServiceFails_ReturnsBadRequest()
    {
        var modalityId = Guid.NewGuid();
        var request = new UpdateModalityRequest
        {
            Name = "Updated",
            TherapeuticCategory = "Cat",
            MainGoal = "Goal"
        };

        _modalityServiceMock
            .Setup(x => x.UpdateModalityAsync(modalityId, request, Guid.Empty))
            .ReturnsAsync(Result<ModalityDto>.Failure("Validation error"));

        var result = await _sut.UpdateModality(modalityId, request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region DeleteModality

    [Fact]
    public async Task DeleteModality_WhenFound_ReturnsNoContent()
    {
        var modalityId = Guid.NewGuid();

        _modalityServiceMock
            .Setup(x => x.DeleteModalityAsync(modalityId, Guid.Empty))
            .ReturnsAsync(Result.Success());

        var result = await _sut.DeleteModality(modalityId);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteModality_WhenNotFound_ReturnsNotFound()
    {
        var modalityId = Guid.NewGuid();

        _modalityServiceMock
            .Setup(x => x.DeleteModalityAsync(modalityId, Guid.Empty))
            .ReturnsAsync(Result.Failure("Modality not found"));

        var result = await _sut.DeleteModality(modalityId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteModality_WhenServiceFails_ReturnsBadRequest()
    {
        var modalityId = Guid.NewGuid();

        _modalityServiceMock
            .Setup(x => x.DeleteModalityAsync(modalityId, Guid.Empty))
            .ReturnsAsync(Result.Failure("Cannot delete modality in use"));

        var result = await _sut.DeleteModality(modalityId);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion
}
