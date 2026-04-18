using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Mapping;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Library;
using Rehably.Domain.Entities.Library;
using Rehably.Infrastructure.Services.Library;
using Rehably.Tests.Helpers;

namespace Rehably.Tests.Services.Library;

public class TreatmentServiceTests
{
    private readonly Mock<ITreatmentRepository> _repositoryMock;
    private readonly Mock<IBodyRegionCategoryRepository> _bodyRegionRepositoryMock;
    private readonly Mock<ILogger<TreatmentService>> _loggerMock;
    private readonly ITreatmentService _sut;

    static TreatmentServiceTests()
    {
        MapsterConfig.ConfigureMappings();
    }

    public TreatmentServiceTests()
    {
        _repositoryMock = new Mock<ITreatmentRepository>();
        _bodyRegionRepositoryMock = new Mock<IBodyRegionCategoryRepository>();
        _loggerMock = new Mock<ILogger<TreatmentService>>();
        _sut = new TreatmentService(
            _repositoryMock.Object,
            _bodyRegionRepositoryMock.Object,
            _loggerMock.Object);
    }

    // T042: UpdateTreatment_DifferentClinicOwner_ReturnsFailure
    [Fact]
    public async Task UpdateTreatment_DifferentClinicOwner_ReturnsFailure()
    {
        var clinicA = Guid.NewGuid();
        var clinicB = Guid.NewGuid();
        var bodyRegionId = Guid.NewGuid();
        var treatmentId = Guid.NewGuid();

        var treatment = new Treatment
        {
            Id = treatmentId,
            ClinicId = clinicA,
            Code = "T001",
            Name = "Shoulder Therapy",
            BodyRegionCategoryId = bodyRegionId,
            AffectedArea = "Shoulder"
        };

        _repositoryMock
            .Setup(r => r.GetWithDetailsAsync(treatmentId))
            .ReturnsAsync(treatment);

        var request = new UpdateTreatmentRequest
        {
            Name = "Updated",
            BodyRegionCategoryId = bodyRegionId,
            AffectedArea = "Shoulder"
        };

        var result = await _sut.UpdateTreatmentAsync(treatmentId, request, clinicB);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    // DeleteTreatment_DifferentClinicOwner_ReturnsFailure
    [Fact]
    public async Task DeleteTreatment_DifferentClinicOwner_ReturnsFailure()
    {
        var clinicA = Guid.NewGuid();
        var clinicB = Guid.NewGuid();
        var treatmentId = Guid.NewGuid();

        var treatment = new Treatment
        {
            Id = treatmentId,
            ClinicId = clinicA,
            Code = "T001",
            Name = "Shoulder Therapy",
            BodyRegionCategoryId = Guid.NewGuid(),
            AffectedArea = "Shoulder"
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(treatmentId))
            .ReturnsAsync(treatment);

        var result = await _sut.DeleteTreatmentAsync(treatmentId, clinicB);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    // UpdateTreatment_GlobalItem_ReturnsFailure (clinic cannot update global item)
    [Fact]
    public async Task UpdateTreatment_GlobalItem_ReturnsFailure()
    {
        var clinicId = Guid.NewGuid();
        var bodyRegionId = Guid.NewGuid();
        var treatmentId = Guid.NewGuid();

        var globalTreatment = new Treatment
        {
            Id = treatmentId,
            ClinicId = null, // global item
            Code = "T001",
            Name = "Global Treatment",
            BodyRegionCategoryId = bodyRegionId,
            AffectedArea = "Back"
        };

        _repositoryMock
            .Setup(r => r.GetWithDetailsAsync(treatmentId))
            .ReturnsAsync(globalTreatment);

        var request = new UpdateTreatmentRequest
        {
            Name = "Updated",
            BodyRegionCategoryId = bodyRegionId,
            AffectedArea = "Back"
        };

        var result = await _sut.UpdateTreatmentAsync(treatmentId, request, clinicId);

        result.IsSuccess.Should().BeFalse();
    }

    // DeleteTreatment_GlobalItem_ReturnsFailure
    [Fact]
    public async Task DeleteTreatment_GlobalItem_ReturnsFailure()
    {
        var clinicId = Guid.NewGuid();
        var treatmentId = Guid.NewGuid();

        var globalTreatment = new Treatment
        {
            Id = treatmentId,
            ClinicId = null, // global item
            Code = "T001",
            Name = "Global Treatment",
            BodyRegionCategoryId = Guid.NewGuid(),
            AffectedArea = "Back"
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(treatmentId))
            .ReturnsAsync(globalTreatment);

        var result = await _sut.DeleteTreatmentAsync(treatmentId, clinicId);

        result.IsSuccess.Should().BeFalse();
    }

    // T044: GetAllTreatments_WithSearch_ReturnsMatchingTreatments
    [Fact]
    public async Task GetAllTreatments_WithSearch_ReturnsMatchingTreatments()
    {
        var bodyRegionId = Guid.NewGuid();
        var bodyRegion = new BodyRegionCategory { Id = bodyRegionId, Name = "Upper Body" };
        var treatments = new List<Treatment>
        {
            new() { Id = Guid.NewGuid(), ClinicId = null, Code = "T001", Name = "Shoulder Therapy", BodyRegionCategoryId = bodyRegionId, AffectedArea = "Shoulder", BodyRegionCategory = bodyRegion },
            new() { Id = Guid.NewGuid(), ClinicId = null, Code = "T002", Name = "Knee Rehabilitation", BodyRegionCategoryId = bodyRegionId, AffectedArea = "Knee", BodyRegionCategory = bodyRegion }
        };

        _repositoryMock
            .Setup(r => r.Query())
            .Returns(treatments.ToAsyncQueryable());

        var result = await _sut.GetTreatmentsAsync(null, "Shoulder", 1, 20);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].Name.Should().Be("Shoulder Therapy");
    }

    // GetAllTreatments_NoSearch_ReturnsAll
    [Fact]
    public async Task GetAllTreatments_NoSearch_ReturnsAll()
    {
        var bodyRegionId = Guid.NewGuid();
        var bodyRegion = new BodyRegionCategory { Id = bodyRegionId, Name = "Lower Body" };
        var treatments = new List<Treatment>
        {
            new() { Id = Guid.NewGuid(), ClinicId = null, Code = "T001", Name = "Shoulder Therapy", BodyRegionCategoryId = bodyRegionId, AffectedArea = "Shoulder", BodyRegionCategory = bodyRegion },
            new() { Id = Guid.NewGuid(), ClinicId = null, Code = "T002", Name = "Knee Rehabilitation", BodyRegionCategoryId = bodyRegionId, AffectedArea = "Knee", BodyRegionCategory = bodyRegion }
        };

        _repositoryMock
            .Setup(r => r.Query())
            .Returns(treatments.ToAsyncQueryable());

        var result = await _sut.GetTreatmentsAsync(null, null, 1, 20);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
    }

    // UpdateTreatment_ValidOwnership_ReturnsUpdatedDto
    [Fact]
    public async Task UpdateTreatment_ValidOwnership_ReturnsUpdatedDto()
    {
        var clinicId = Guid.NewGuid();
        var bodyRegionId = Guid.NewGuid();
        var treatmentId = Guid.NewGuid();

        var treatment = new Treatment
        {
            Id = treatmentId,
            ClinicId = clinicId,
            Code = "T001",
            Name = "Shoulder Therapy",
            BodyRegionCategoryId = bodyRegionId,
            AffectedArea = "Shoulder",
            BodyRegionCategory = new BodyRegionCategory { Id = bodyRegionId, Name = "Shoulder Region" }
        };

        _repositoryMock
            .Setup(r => r.GetWithDetailsAsync(treatmentId))
            .ReturnsAsync(treatment);
        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Treatment>()))
            .Returns(Task.CompletedTask);
        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = new UpdateTreatmentRequest
        {
            Name = "Updated Shoulder Therapy",
            BodyRegionCategoryId = bodyRegionId,
            AffectedArea = "Shoulder"
        };

        var result = await _sut.UpdateTreatmentAsync(treatmentId, request, clinicId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated Shoulder Therapy");
    }

    // DeleteTreatment_ValidOwnership_ReturnsSuccess
    [Fact]
    public async Task DeleteTreatment_ValidOwnership_ReturnsSuccess()
    {
        var clinicId = Guid.NewGuid();
        var treatmentId = Guid.NewGuid();

        var treatment = new Treatment
        {
            Id = treatmentId,
            ClinicId = clinicId,
            Code = "T001",
            Name = "Shoulder Therapy",
            BodyRegionCategoryId = Guid.NewGuid(),
            AffectedArea = "Shoulder"
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(treatmentId))
            .ReturnsAsync(treatment);
        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Treatment>()))
            .Returns(Task.CompletedTask);
        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _sut.DeleteTreatmentAsync(treatmentId, clinicId);

        result.IsSuccess.Should().BeTrue();
    }
}
