using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Library;
using Rehably.Application.Services.Storage;
using Rehably.Domain.Entities.Library;

namespace Rehably.Infrastructure.Services.Library;

public class ExerciseService : IExerciseService
{
    private readonly IExerciseRepository _exerciseRepository;
    private readonly IBodyRegionCategoryRepository _bodyRegionRepository;
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<ExerciseService> _logger;

    public ExerciseService(
        IExerciseRepository exerciseRepository,
        IBodyRegionCategoryRepository bodyRegionRepository,
        IFileUploadService fileUploadService,
        ILogger<ExerciseService> logger)
    {
        _exerciseRepository = exerciseRepository;
        _bodyRegionRepository = bodyRegionRepository;
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    public async Task<Result<LibraryItemListResponse<ExerciseDto>>> GetExercisesAsync(Guid? bodyRegionId, string? search, int page, int pageSize)
    {
        var query = _exerciseRepository.Query()
            .Include(e => e.BodyRegionCategory)
            .Where(e => !e.IsDeleted);

        if (bodyRegionId.HasValue)
            query = query.Where(e => e.BodyRegionCategoryId == bodyRegionId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => e.Name.ToLower().Contains(search.ToLower()));

        var totalCount = await query.CountAsync();

        var exercises = (await query
            .OrderBy(e => e.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync())
            .Adapt<List<ExerciseDto>>();

        var response = LibraryItemListResponse<ExerciseDto>.Create(exercises, page, pageSize, totalCount);
        return Result<LibraryItemListResponse<ExerciseDto>>.Success(response);
    }

    public async Task<Result<ExerciseDto>> GetExerciseByIdAsync(Guid id)
    {
        var exercise = await _exerciseRepository.GetWithDetailsAsync(id);
        if (exercise == null)
            return Result<ExerciseDto>.Failure("Exercise not found");

        return Result<ExerciseDto>.Success(exercise.Adapt<ExerciseDto>());
    }

    public async Task<Result<ExerciseDto>> CreateExerciseAsync(CreateExerciseRequest request, Guid? clinicId)
    {
        var bodyRegionExists = await _bodyRegionRepository.AnyAsync(b => b.Id == request.BodyRegionCategoryId && b.IsActive);
        if (!bodyRegionExists)
            return Result<ExerciseDto>.Failure("Body region category not found");

        var exercise = new Exercise
        {
            ClinicId = clinicId,
            Name = request.Name,
            NameArabic = request.NameArabic,
            Description = request.Description,
            BodyRegionCategoryId = request.BodyRegionCategoryId,
            RelatedConditionCode = request.RelatedConditionCode,
            Tags = request.Tags,
            Repeats = request.Repeats,
            Steps = request.Steps,
            HoldSeconds = request.HoldSeconds,
            LinkedExerciseIds = request.LinkedExerciseIds,
            AccessTier = request.AccessTier,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        if (request.VideoStream != null)
        {
            var folder = clinicId.HasValue
                ? $"rehably/clinics/{clinicId}/exercises/videos"
                : "rehably/library/exercises/videos";
            var videoResult = await _fileUploadService.UploadFileAsync(request.VideoStream, request.VideoFileName!, folder, clinicId);
            if (videoResult.IsSuccess)
                exercise.VideoUrl = videoResult.Value;
        }

        if (request.ThumbnailStream != null)
        {
            var folder = clinicId.HasValue
                ? $"rehably/clinics/{clinicId}/exercises/thumbnails"
                : "rehably/library/exercises/thumbnails";
            var thumbResult = await _fileUploadService.UploadFileAsync(request.ThumbnailStream, request.ThumbnailFileName!, folder, clinicId);
            if (thumbResult.IsSuccess)
                exercise.ThumbnailUrl = thumbResult.Value;
        }

        await _exerciseRepository.AddAsync(exercise);
        await _exerciseRepository.SaveChangesAsync();

        _logger.LogInformation("Created exercise {ExerciseId} ({ExerciseName}) for clinic {ClinicId}",
            exercise.Id, exercise.Name, clinicId ?? Guid.Empty);

        var saved = await _exerciseRepository.Query()
            .AsNoTracking()
            .Include(e => e.BodyRegionCategory)
            .FirstOrDefaultAsync(e => e.Id == exercise.Id);
        return Result<ExerciseDto>.Success(saved!.Adapt<ExerciseDto>());
    }

    public async Task<Result<ExerciseDto>> UpdateExerciseAsync(Guid id, UpdateExerciseRequest request, Guid clinicId)
    {
        var exercise = await _exerciseRepository.GetWithDetailsAsync(id);
        if (exercise == null)
            return Result<ExerciseDto>.Failure("Exercise not found");

        if (clinicId != Guid.Empty && exercise.ClinicId != clinicId)
            return Result<ExerciseDto>.Failure("You can only update exercises owned by your clinic");

        if (request.BodyRegionCategoryId != exercise.BodyRegionCategoryId)
        {
            var bodyRegionExists = await _bodyRegionRepository.AnyAsync(b => b.Id == request.BodyRegionCategoryId && b.IsActive);
            if (!bodyRegionExists)
                return Result<ExerciseDto>.Failure("Body region category not found");
        }

        exercise.Name = request.Name;
        exercise.NameArabic = request.NameArabic;
        exercise.Description = request.Description;
        exercise.BodyRegionCategoryId = request.BodyRegionCategoryId;
        exercise.RelatedConditionCode = request.RelatedConditionCode;
        exercise.Tags = request.Tags;
        exercise.Repeats = request.Repeats;
        exercise.Steps = request.Steps;
        exercise.HoldSeconds = request.HoldSeconds;
        exercise.LinkedExerciseIds = request.LinkedExerciseIds;
        exercise.AccessTier = request.AccessTier;
        exercise.UpdatedAt = DateTime.UtcNow;

        if (request.VideoStream != null)
        {
            if (!string.IsNullOrEmpty(exercise.VideoUrl))
                await _fileUploadService.DeleteFileByUrlAsync(exercise.VideoUrl);

            var folder = exercise.ClinicId.HasValue
                ? $"rehably/clinics/{exercise.ClinicId}/exercises/videos"
                : "rehably/library/exercises/videos";
            var videoResult = await _fileUploadService.UploadFileAsync(request.VideoStream, request.VideoFileName!, folder, exercise.ClinicId);
            if (videoResult.IsSuccess)
                exercise.VideoUrl = videoResult.Value;
        }

        if (request.ThumbnailStream != null)
        {
            if (!string.IsNullOrEmpty(exercise.ThumbnailUrl))
                await _fileUploadService.DeleteFileByUrlAsync(exercise.ThumbnailUrl);

            var folder = exercise.ClinicId.HasValue
                ? $"rehably/clinics/{exercise.ClinicId}/exercises/thumbnails"
                : "rehably/library/exercises/thumbnails";
            var thumbResult = await _fileUploadService.UploadFileAsync(request.ThumbnailStream, request.ThumbnailFileName!, folder, exercise.ClinicId);
            if (thumbResult.IsSuccess)
                exercise.ThumbnailUrl = thumbResult.Value;
        }

        await _exerciseRepository.UpdateAsync(exercise);
        await _exerciseRepository.SaveChangesAsync();

        _logger.LogInformation("Updated exercise {ExerciseId} ({ExerciseName})", exercise.Id, exercise.Name);

        var saved = await _exerciseRepository.Query()
            .AsNoTracking()
            .Include(e => e.BodyRegionCategory)
            .FirstOrDefaultAsync(e => e.Id == exercise.Id);
        return Result<ExerciseDto>.Success(saved!.Adapt<ExerciseDto>());
    }

    public async Task<Result> DeleteExerciseAsync(Guid id, Guid clinicId)
    {
        var exercise = await _exerciseRepository.GetByIdAsync(id);
        if (exercise == null)
            return Result.Failure("Exercise not found");

        if (clinicId != Guid.Empty && exercise.ClinicId != clinicId)
            return Result.Failure("You can only delete exercises owned by your clinic");

        exercise.IsDeleted = true;
        exercise.UpdatedAt = DateTime.UtcNow;

        await _exerciseRepository.UpdateAsync(exercise);
        await _exerciseRepository.SaveChangesAsync();

        _logger.LogInformation("Soft deleted exercise {ExerciseId} ({ExerciseName})", exercise.Id, exercise.Name);

        return Result.Success();
    }

}
