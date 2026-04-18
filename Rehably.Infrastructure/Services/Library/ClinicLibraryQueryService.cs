using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Services.Library;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Library;
using Rehably.Domain.Enums;

namespace Rehably.Infrastructure.Services.Library;

public class ClinicLibraryQueryService : IClinicLibraryQueryService
{
    private readonly ITreatmentRepository _treatmentRepository;
    private readonly IExerciseRepository _exerciseRepository;
    private readonly IModalityRepository _modalityRepository;
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IClinicLibraryOverrideRepository _overrideRepository;
    private readonly ILogger<ClinicLibraryQueryService> _logger;

    public ClinicLibraryQueryService(
        ITreatmentRepository treatmentRepository,
        IExerciseRepository exerciseRepository,
        IModalityRepository modalityRepository,
        IAssessmentRepository assessmentRepository,
        IDeviceRepository deviceRepository,
        IClinicLibraryOverrideRepository overrideRepository,
        ILogger<ClinicLibraryQueryService> logger)
    {
        _treatmentRepository = treatmentRepository;
        _exerciseRepository = exerciseRepository;
        _modalityRepository = modalityRepository;
        _assessmentRepository = assessmentRepository;
        _deviceRepository = deviceRepository;
        _overrideRepository = overrideRepository;
        _logger = logger;
    }

    public async Task<Result<LibraryItemListResponse<TreatmentDto>>> GetClinicTreatmentsAsync(Guid clinicId, Guid? bodyRegionId, string? search, int page, int pageSize)
    {
        try
        {
            var hiddenIds = await GetHiddenGlobalItemIdsAsync(clinicId, LibraryType.Treatment);
            var overrides = await GetOverridesForTypeAsync(clinicId, LibraryType.Treatment);

            var query = _treatmentRepository.Query()
                .Include(t => t.BodyRegionCategory)
                .Where(t => !t.IsDeleted)
                .Where(t => (t.ClinicId == null && !hiddenIds.Contains(t.Id)) || t.ClinicId == clinicId);

            if (bodyRegionId.HasValue)
                query = query.Where(t => t.BodyRegionCategoryId == bodyRegionId.Value);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(t => EF.Functions.ILike(t.Name, $"%{search}%"));

            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .OrderBy(t => t.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = pagedItems.Select(t => MapToTreatmentDto(t, overrides)).ToList();

            return Result<LibraryItemListResponse<TreatmentDto>>.Success(
                LibraryItemListResponse<TreatmentDto>.Create(dtos, page, pageSize, totalCount));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting clinic treatments for ClinicId: {ClinicId}", clinicId);
            return Result<LibraryItemListResponse<TreatmentDto>>.Failure("Failed to get clinic treatments");
        }
    }

    public async Task<Result<LibraryItemListResponse<ExerciseDto>>> GetClinicExercisesAsync(Guid clinicId, Guid? bodyRegionId, string? search, int page, int pageSize)
    {
        try
        {
            var hiddenIds = await GetHiddenGlobalItemIdsAsync(clinicId, LibraryType.Exercise);
            var overrides = await GetOverridesForTypeAsync(clinicId, LibraryType.Exercise);

            var query = _exerciseRepository.Query()
                .Include(e => e.BodyRegionCategory)
                .Where(e => !e.IsDeleted)
                .Where(e => (e.ClinicId == null && !hiddenIds.Contains(e.Id)) || e.ClinicId == clinicId);

            if (bodyRegionId.HasValue)
                query = query.Where(e => e.BodyRegionCategoryId == bodyRegionId.Value);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(e => e.Name.ToLower().Contains(search.ToLower()));

            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .OrderBy(e => e.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = pagedItems.Select(e => MapToExerciseDto(e, overrides)).ToList();

            return Result<LibraryItemListResponse<ExerciseDto>>.Success(
                LibraryItemListResponse<ExerciseDto>.Create(dtos, page, pageSize, totalCount));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting clinic exercises for ClinicId: {ClinicId}", clinicId);
            return Result<LibraryItemListResponse<ExerciseDto>>.Failure("Failed to get clinic exercises");
        }
    }

    public async Task<Result<LibraryItemListResponse<ModalityDto>>> GetClinicModalitiesAsync(Guid clinicId, Guid? bodyRegionId, string? search, int page, int pageSize)
    {
        try
        {
            var hiddenIds = await GetHiddenGlobalItemIdsAsync(clinicId, LibraryType.Modality);
            var overrides = await GetOverridesForTypeAsync(clinicId, LibraryType.Modality);

            var query = _modalityRepository.Query()
                .Where(m => !m.IsDeleted)
                .Where(m => (m.ClinicId == null && !hiddenIds.Contains(m.Id)) || m.ClinicId == clinicId);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(m => m.Name.ToLower().Contains(search.ToLower()));

            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .OrderBy(m => m.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = pagedItems.Select(m => MapToModalityDto(m, overrides)).ToList();

            return Result<LibraryItemListResponse<ModalityDto>>.Success(
                LibraryItemListResponse<ModalityDto>.Create(dtos, page, pageSize, totalCount));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting clinic modalities for ClinicId: {ClinicId}", clinicId);
            return Result<LibraryItemListResponse<ModalityDto>>.Failure("Failed to get clinic modalities");
        }
    }

    public async Task<Result<LibraryItemListResponse<AssessmentDto>>> GetClinicAssessmentsAsync(Guid clinicId, Guid? bodyRegionId, string? search, int page, int pageSize)
    {
        try
        {
            var hiddenIds = await GetHiddenGlobalItemIdsAsync(clinicId, LibraryType.Assessment);
            var overrides = await GetOverridesForTypeAsync(clinicId, LibraryType.Assessment);

            var query = _assessmentRepository.Query()
                .Where(a => !a.IsDeleted)
                .Where(a => (a.ClinicId == null && !hiddenIds.Contains(a.Id)) || a.ClinicId == clinicId);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a => a.Name.ToLower().Contains(search.ToLower()));

            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .OrderBy(a => a.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = pagedItems.Select(a => MapToAssessmentDto(a, overrides)).ToList();

            return Result<LibraryItemListResponse<AssessmentDto>>.Success(
                LibraryItemListResponse<AssessmentDto>.Create(dtos, page, pageSize, totalCount));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting clinic assessments for ClinicId: {ClinicId}", clinicId);
            return Result<LibraryItemListResponse<AssessmentDto>>.Failure("Failed to get clinic assessments");
        }
    }

    public async Task<Result<LibraryItemListResponse<DeviceDto>>> GetClinicDevicesAsync(Guid clinicId, Guid? bodyRegionId, string? search, int page, int pageSize)
    {
        try
        {
            var hiddenIds = await GetHiddenGlobalItemIdsAsync(clinicId, LibraryType.Device);
            var overrides = await GetOverridesForTypeAsync(clinicId, LibraryType.Device);

            var query = _deviceRepository.Query()
                .Where(d => !d.IsDeleted)
                .Where(d => (d.ClinicId == null && !hiddenIds.Contains(d.Id)) || d.ClinicId == clinicId);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(d => d.Name.ToLower().Contains(search.ToLower()));

            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .OrderBy(d => d.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = pagedItems.Select(d => MapToDeviceDto(d, overrides)).ToList();

            return Result<LibraryItemListResponse<DeviceDto>>.Success(
                LibraryItemListResponse<DeviceDto>.Create(dtos, page, pageSize, totalCount));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting clinic devices for ClinicId: {ClinicId}", clinicId);
            return Result<LibraryItemListResponse<DeviceDto>>.Failure("Failed to get clinic devices");
        }
    }

    private async Task<HashSet<Guid>> GetHiddenGlobalItemIdsAsync(Guid clinicId, LibraryType libraryType)
    {
        var hiddenIds = await _overrideRepository.GetHiddenItemIdsAsync(clinicId, libraryType);
        return hiddenIds.ToHashSet();
    }

    private async Task<Dictionary<Guid, ClinicLibraryOverride>> GetOverridesForTypeAsync(Guid clinicId, LibraryType libraryType)
    {
        var overrides = await _overrideRepository.GetNonHiddenOverridesAsync(clinicId, libraryType);
        return overrides.ToDictionary(o => o.GlobalItemId);
    }

    private TreatmentDto MapToTreatmentDto(Treatment treatment, Dictionary<Guid, ClinicLibraryOverride> overrides)
    {
        var dto = new TreatmentDto
        {
            Id = treatment.Id,
            ClinicId = treatment.ClinicId,
            Code = treatment.Code,
            Name = treatment.Name,
            NameArabic = treatment.NameArabic,
            BodyRegionCategoryId = treatment.BodyRegionCategoryId,
            BodyRegionCategoryName = treatment.BodyRegionCategory?.Name,
            AffectedArea = treatment.AffectedArea,
            MinDurationWeeks = treatment.MinDurationWeeks,
            MaxDurationWeeks = treatment.MaxDurationWeeks,
            ExpectedSessions = treatment.ExpectedSessions,
            Description = treatment.Description,
            RedFlags = treatment.RedFlags,
            Contraindications = treatment.Contraindications,
            ClinicalNotes = treatment.ClinicalNotes,
            SourceReference = treatment.SourceReference,
            SourceDetails = treatment.SourceDetails,
            AccessTier = treatment.AccessTier,
            IsDeleted = treatment.IsDeleted,
            CreatedAt = treatment.CreatedAt,
            UpdatedAt = treatment.UpdatedAt
        };

        if (treatment.ClinicId == null && overrides.TryGetValue(treatment.Id, out var customOverride))
        {
            ApplyOverrideData(dto, customOverride.OverrideDataJson);
        }

        return dto;
    }

    private ExerciseDto MapToExerciseDto(Exercise exercise, Dictionary<Guid, ClinicLibraryOverride> overrides)
    {
        var dto = new ExerciseDto
        {
            Id = exercise.Id,
            ClinicId = exercise.ClinicId,
            Name = exercise.Name,
            NameArabic = exercise.NameArabic,
            Description = exercise.Description,
            BodyRegionCategoryId = exercise.BodyRegionCategoryId,
            BodyRegionCategoryName = exercise.BodyRegionCategory?.Name,
            RelatedConditionCode = exercise.RelatedConditionCode,
            Tags = exercise.Tags,
            Repeats = exercise.Repeats,
            Steps = exercise.Steps,
            HoldSeconds = exercise.HoldSeconds,
            VideoUrl = exercise.VideoUrl,
            ThumbnailUrl = exercise.ThumbnailUrl,
            LinkedExerciseIds = exercise.LinkedExerciseIds,
            AccessTier = exercise.AccessTier,
            IsDeleted = exercise.IsDeleted,
            CreatedAt = exercise.CreatedAt,
            UpdatedAt = exercise.UpdatedAt
        };

        if (exercise.ClinicId == null && overrides.TryGetValue(exercise.Id, out var customOverride))
        {
            ApplyOverrideData(dto, customOverride.OverrideDataJson);
        }

        return dto;
    }

    private ModalityDto MapToModalityDto(Modality modality, Dictionary<Guid, ClinicLibraryOverride> overrides)
    {
        var dto = new ModalityDto
        {
            Id = modality.Id,
            ClinicId = modality.ClinicId,
            Code = modality.Code,
            Name = modality.Name,
            NameArabic = modality.NameArabic,
            ModalityType = modality.ModalityType,
            TherapeuticCategory = modality.TherapeuticCategory,
            MainGoal = modality.MainGoal,
            ParametersNotes = modality.ParametersNotes,
            ClinicalNotes = modality.ClinicalNotes,
            MinDurationWeeks = modality.MinDurationWeeks,
            MaxDurationWeeks = modality.MaxDurationWeeks,
            MinSessions = modality.MinSessions,
            MaxSessions = modality.MaxSessions,
            RelatedConditionCodes = modality.RelatedConditionCodes,
            AccessTier = modality.AccessTier,
            IsDeleted = modality.IsDeleted,
            CreatedAt = modality.CreatedAt,
            UpdatedAt = modality.UpdatedAt
        };

        if (modality.ClinicId == null && overrides.TryGetValue(modality.Id, out var customOverride))
        {
            ApplyOverrideData(dto, customOverride.OverrideDataJson);
        }

        return dto;
    }

    private AssessmentDto MapToAssessmentDto(Assessment assessment, Dictionary<Guid, ClinicLibraryOverride> overrides)
    {
        var dto = new AssessmentDto
        {
            Id = assessment.Id,
            ClinicId = assessment.ClinicId,
            Code = assessment.Code,
            Name = assessment.Name,
            NameArabic = assessment.NameArabic,
            TimePoint = assessment.TimePoint,
            Description = assessment.Description,
            Instructions = assessment.Instructions,
            ScoringGuide = assessment.ScoringGuide,
            RelatedConditionCodes = assessment.RelatedConditionCodes,
            AccessTier = assessment.AccessTier,
            IsDeleted = assessment.IsDeleted,
            CreatedAt = assessment.CreatedAt,
            UpdatedAt = assessment.UpdatedAt
        };

        if (assessment.ClinicId == null && overrides.TryGetValue(assessment.Id, out var customOverride))
        {
            ApplyOverrideData(dto, customOverride.OverrideDataJson);
        }

        return dto;
    }

    private DeviceDto MapToDeviceDto(Device device, Dictionary<Guid, ClinicLibraryOverride> overrides)
    {
        var dto = new DeviceDto
        {
            Id = device.Id,
            ClinicId = device.ClinicId,
            Name = device.Name,
            NameArabic = device.NameArabic,
            Description = device.Description,
            ImageUrl = device.ImageUrl,
            RelatedConditionCodes = device.RelatedConditionCodes,
            Manufacturer = device.Manufacturer,
            Model = device.Model,
            AccessTier = device.AccessTier,
            IsDeleted = device.IsDeleted,
            CreatedAt = device.CreatedAt,
            UpdatedAt = device.UpdatedAt
        };

        if (device.ClinicId == null && overrides.TryGetValue(device.Id, out var customOverride))
        {
            ApplyOverrideData(dto, customOverride.OverrideDataJson);
        }

        return dto;
    }

    private void ApplyOverrideData(object dto, string? overrideDataJson)
    {
        if (string.IsNullOrWhiteSpace(overrideDataJson))
            return;

        try
        {
            var overrideData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(overrideDataJson);
            if (overrideData == null)
                return;

            var dtoType = dto.GetType();

            if (overrideData.TryGetValue("customName", out var customName) && customName.ValueKind == JsonValueKind.String)
            {
                var nameProperty = dtoType.GetProperty("Name");
                nameProperty?.SetValue(dto, customName.GetString());
            }

            if (overrideData.TryGetValue("customNameArabic", out var customNameArabic) && customNameArabic.ValueKind == JsonValueKind.String)
            {
                var nameArabicProperty = dtoType.GetProperty("NameArabic");
                nameArabicProperty?.SetValue(dto, customNameArabic.GetString());
            }

            if (overrideData.TryGetValue("customDescription", out var customDescription) && customDescription.ValueKind == JsonValueKind.String)
            {
                var descriptionProperty = dtoType.GetProperty("Description");
                descriptionProperty?.SetValue(dto, customDescription.GetString());
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse override data JSON for item {ItemId}", dto.GetType().Name);
        }
    }
}
