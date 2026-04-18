using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Library;
using Rehably.Domain.Entities.Library;
using Rehably.Infrastructure.Services.Library;

namespace Rehably.Tests.Services.Library;

public class TreatmentStageServiceTests
{
    private readonly Mock<ITreatmentStageRepository> _repositoryMock;
    private readonly Mock<IBodyRegionCategoryRepository> _bodyRegionRepositoryMock;
    private readonly Mock<ILogger<TreatmentStageService>> _loggerMock;
    private readonly ITreatmentStageService _sut;

    public TreatmentStageServiceTests()
    {
        _repositoryMock = new Mock<ITreatmentStageRepository>();
        _bodyRegionRepositoryMock = new Mock<IBodyRegionCategoryRepository>();
        _loggerMock = new Mock<ILogger<TreatmentStageService>>();
        _sut = new TreatmentStageService(
            _repositoryMock.Object,
            _bodyRegionRepositoryMock.Object,
            _loggerMock.Object);
    }

    // T039: CreateStage_ValidRequest_ReturnsStageDto
    [Fact]
    public async Task CreateStage_ValidRequest_ReturnsStageDto()
    {
        var clinicId = Guid.NewGuid();
        var request = new CreateTreatmentStageRequest
        {
            Code = "S001",
            Name = "Acute Phase",
            MinWeeks = 1,
            MaxWeeks = 4,
            MinSessions = 6,
            MaxSessions = 12
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<TreatmentStage>()))
            .ReturnsAsync((TreatmentStage s) => s);
        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _sut.CreateStageAsync(request, clinicId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Code.Should().Be("S001");
        result.Value.Name.Should().Be("Acute Phase");
        result.Value.TenantId.Should().Be(clinicId);
    }

    // T040: UpdateStage_DifferentClinicOwner_ReturnsFailure
    [Fact]
    public async Task UpdateStage_DifferentClinicOwner_ReturnsFailure()
    {
        var clinicA = Guid.NewGuid();
        var clinicB = Guid.NewGuid();
        var stageId = Guid.NewGuid();

        var stage = new TreatmentStage
        {
            Id = stageId,
            TenantId = clinicA,
            Code = "S001",
            Name = "Acute Phase"
        };

        _repositoryMock
            .Setup(r => r.GetWithDetailsAsync(stageId))
            .ReturnsAsync(stage);

        var request = new UpdateTreatmentStageRequest { Name = "Updated" };

        var result = await _sut.UpdateStageAsync(stageId, request, clinicB);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    // T041: DeleteStage_DifferentClinicOwner_ReturnsFailure
    [Fact]
    public async Task DeleteStage_DifferentClinicOwner_ReturnsFailure()
    {
        var clinicA = Guid.NewGuid();
        var clinicB = Guid.NewGuid();
        var stageId = Guid.NewGuid();

        var stage = new TreatmentStage
        {
            Id = stageId,
            TenantId = clinicA,
            Code = "S001",
            Name = "Acute Phase"
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(stageId))
            .ReturnsAsync(stage);

        var result = await _sut.DeleteStageAsync(stageId, clinicB);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    // T043: GetAllStages_WithSearch_ReturnsMatchingStages
    [Fact]
    public async Task GetAllStages_WithSearch_ReturnsMatchingStages()
    {
        var clinicId = Guid.NewGuid();
        var stages = new List<TreatmentStage>
        {
            new() { Id = Guid.NewGuid(), TenantId = clinicId, Code = "S001", Name = "Acute Phase" },
            new() { Id = Guid.NewGuid(), TenantId = clinicId, Code = "S002", Name = "Recovery Phase" }
        };

        _repositoryMock
            .Setup(r => r.Query())
            .Returns(stages.AsQueryable());

        var result = await _sut.GetClinicStagesAsync(clinicId, null, "Acute", 1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].Name.Should().Be("Acute Phase");
    }

    [Fact]
    public async Task GetAllStages_NoSearch_ReturnsAllClinicStages()
    {
        var clinicId = Guid.NewGuid();
        var stages = new List<TreatmentStage>
        {
            new() { Id = Guid.NewGuid(), TenantId = clinicId, Code = "S001", Name = "Acute Phase" },
            new() { Id = Guid.NewGuid(), TenantId = clinicId, Code = "S002", Name = "Recovery Phase" }
        };

        _repositoryMock
            .Setup(r => r.Query())
            .Returns(stages.AsQueryable());

        var result = await _sut.GetClinicStagesAsync(clinicId, null, null, 1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetStageById_StageExists_ReturnsDto()
    {
        var stageId = Guid.NewGuid();
        var clinicId = Guid.NewGuid();
        var stage = new TreatmentStage
        {
            Id = stageId,
            TenantId = clinicId,
            Code = "S001",
            Name = "Acute Phase"
        };

        _repositoryMock
            .Setup(r => r.GetWithDetailsAsync(stageId))
            .ReturnsAsync(stage);

        var result = await _sut.GetStageByIdAsync(stageId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(stageId);
    }

    [Fact]
    public async Task GetStageById_StageNotFound_ReturnsFailure()
    {
        var stageId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetWithDetailsAsync(stageId))
            .ReturnsAsync((TreatmentStage?)null);

        var result = await _sut.GetStageByIdAsync(stageId);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateStage_ValidOwnership_ReturnsUpdatedDto()
    {
        var clinicId = Guid.NewGuid();
        var stageId = Guid.NewGuid();

        var stage = new TreatmentStage
        {
            Id = stageId,
            TenantId = clinicId,
            Code = "S001",
            Name = "Acute Phase"
        };

        _repositoryMock
            .Setup(r => r.GetWithDetailsAsync(stageId))
            .ReturnsAsync(stage);
        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<TreatmentStage>()))
            .Returns(Task.CompletedTask);
        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = new UpdateTreatmentStageRequest { Name = "Updated Phase" };

        var result = await _sut.UpdateStageAsync(stageId, request, clinicId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated Phase");
    }

    [Fact]
    public async Task DeleteStage_ValidOwnership_ReturnsSuccess()
    {
        var clinicId = Guid.NewGuid();
        var stageId = Guid.NewGuid();

        var stage = new TreatmentStage
        {
            Id = stageId,
            TenantId = clinicId,
            Code = "S001",
            Name = "Acute Phase"
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(stageId))
            .ReturnsAsync(stage);
        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<TreatmentStage>()))
            .Returns(Task.CompletedTask);
        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _sut.DeleteStageAsync(stageId, clinicId);

        result.IsSuccess.Should().BeTrue();
    }
}
