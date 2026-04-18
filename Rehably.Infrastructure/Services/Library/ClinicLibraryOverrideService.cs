using Mapster;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Services.Library;
using Rehably.Application.Repositories;
using Rehably.Domain.Enums;

namespace Rehably.Infrastructure.Services.Library;

/// <summary>
/// Implementation of clinic library override operations.
/// </summary>
public class ClinicLibraryOverrideService : IClinicLibraryOverrideService
{
    private readonly ITreatmentRepository _treatmentRepository;
    private readonly IExerciseRepository _exerciseRepository;
    private readonly IModalityRepository _modalityRepository;
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IClinicLibraryOverrideRepository _overrideRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClinicLibraryOverrideService> _logger;

    public ClinicLibraryOverrideService(
        ITreatmentRepository treatmentRepository,
        IExerciseRepository exerciseRepository,
        IModalityRepository modalityRepository,
        IAssessmentRepository assessmentRepository,
        IDeviceRepository deviceRepository,
        IClinicLibraryOverrideRepository overrideRepository,
        IUnitOfWork unitOfWork,
        ILogger<ClinicLibraryOverrideService> logger)
    {
        _treatmentRepository = treatmentRepository;
        _exerciseRepository = exerciseRepository;
        _modalityRepository = modalityRepository;
        _assessmentRepository = assessmentRepository;
        _deviceRepository = deviceRepository;
        _overrideRepository = overrideRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ClinicLibraryOverrideDto>> CreateOverrideAsync(Guid clinicId, CreateClinicLibraryOverrideRequest request)
    {
        try
        {
            var globalItemExists = await ValidateGlobalItemExistsAsync(request.LibraryType, request.GlobalItemId);
            if (!globalItemExists)
            {
                return Result<ClinicLibraryOverrideDto>.Failure($"Global {request.LibraryType} item with ID {request.GlobalItemId} not found");
            }

            var existingOverride = await _overrideRepository.GetByClinicAndItemAsync(clinicId, request.GlobalItemId, request.LibraryType);
            if (existingOverride != null)
            {
                return Result<ClinicLibraryOverrideDto>.Failure("An override already exists for this item. Use update instead.");
            }

            var overrideEntity = new Domain.Entities.Library.ClinicLibraryOverride
            {
                ClinicId = clinicId,
                LibraryType = request.LibraryType,
                GlobalItemId = request.GlobalItemId,
                IsHidden = request.IsHidden,
                OverrideDataJson = request.OverrideDataJson,
                CreatedAt = DateTime.UtcNow
            };

            await _overrideRepository.AddAsync(overrideEntity);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Created override for ClinicId: {ClinicId}, Type: {Type}, GlobalItemId: {GlobalItemId}",
                clinicId, request.LibraryType, request.GlobalItemId);

            return Result<ClinicLibraryOverrideDto>.Success(overrideEntity.Adapt<ClinicLibraryOverrideDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating override for ClinicId: {ClinicId}", clinicId);
            return Result<ClinicLibraryOverrideDto>.Failure("Failed to create override");
        }
    }

    public async Task<Result<ClinicLibraryOverrideDto>> UpdateOverrideAsync(Guid clinicId, Guid overrideId, UpdateClinicLibraryOverrideRequest request)
    {
        try
        {
            var overrideEntity = await _overrideRepository.GetByIdAsync(overrideId);

            if (overrideEntity == null || overrideEntity.ClinicId != clinicId || overrideEntity.IsDeleted)
            {
                return Result<ClinicLibraryOverrideDto>.Failure("Override not found");
            }

            overrideEntity.IsHidden = request.IsHidden;
            overrideEntity.OverrideDataJson = request.OverrideDataJson;
            overrideEntity.UpdatedAt = DateTime.UtcNow;

            await _overrideRepository.UpdateAsync(overrideEntity);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Updated override {OverrideId} for ClinicId: {ClinicId}", overrideId, clinicId);

            return Result<ClinicLibraryOverrideDto>.Success(overrideEntity.Adapt<ClinicLibraryOverrideDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating override {OverrideId} for ClinicId: {ClinicId}", overrideId, clinicId);
            return Result<ClinicLibraryOverrideDto>.Failure("Failed to update override");
        }
    }

    public async Task<Result> RemoveOverrideAsync(Guid clinicId, Guid overrideId)
    {
        try
        {
            var overrideEntity = await _overrideRepository.GetByIdAsync(overrideId);

            if (overrideEntity == null || overrideEntity.ClinicId != clinicId || overrideEntity.IsDeleted)
            {
                return Result.Failure("Override not found");
            }

            await _overrideRepository.DeleteAsync(overrideEntity);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Removed override {OverrideId} for ClinicId: {ClinicId}", overrideId, clinicId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing override {OverrideId} for ClinicId: {ClinicId}", overrideId, clinicId);
            return Result.Failure("Failed to remove override");
        }
    }

    public async Task<Result<List<ClinicLibraryOverrideDto>>> GetClinicOverridesAsync(Guid clinicId, LibraryType? type)
    {
        try
        {
            var overrides = await _overrideRepository.GetByClinicAndTypeAsync(clinicId, type);
            var dtos = overrides.Adapt<List<ClinicLibraryOverrideDto>>();

            return Result<List<ClinicLibraryOverrideDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overrides for ClinicId: {ClinicId}", clinicId);
            return Result<List<ClinicLibraryOverrideDto>>.Failure("Failed to get clinic overrides");
        }
    }

    public async Task<Result<ClinicLibraryOverrideDto>> GetOverrideByIdAsync(Guid clinicId, Guid overrideId)
    {
        try
        {
            var overrideEntity = await _overrideRepository.GetByIdAsync(overrideId);

            if (overrideEntity == null || overrideEntity.ClinicId != clinicId || overrideEntity.IsDeleted)
            {
                return Result<ClinicLibraryOverrideDto>.Failure("Override not found");
            }

            return Result<ClinicLibraryOverrideDto>.Success(overrideEntity.Adapt<ClinicLibraryOverrideDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting override {OverrideId} for ClinicId: {ClinicId}", overrideId, clinicId);
            return Result<ClinicLibraryOverrideDto>.Failure("Failed to get override");
        }
    }

    private async Task<bool> ValidateGlobalItemExistsAsync(LibraryType libraryType, Guid globalItemId)
    {
        return libraryType switch
        {
            LibraryType.Treatment => await _treatmentRepository.GlobalItemExistsAsync(globalItemId),
            LibraryType.Exercise => await _exerciseRepository.GlobalItemExistsAsync(globalItemId),
            LibraryType.Modality => await _modalityRepository.GlobalItemExistsAsync(globalItemId),
            LibraryType.Assessment => await _assessmentRepository.GlobalItemExistsAsync(globalItemId),
            LibraryType.Device => await _deviceRepository.GlobalItemExistsAsync(globalItemId),
            _ => false
        };
    }

}
