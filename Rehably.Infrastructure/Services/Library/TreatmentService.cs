using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Library;
using Rehably.Domain.Entities.Library;

namespace Rehably.Infrastructure.Services.Library;

public class TreatmentService : ITreatmentService
{
    private readonly ITreatmentRepository _treatmentRepository;
    private readonly IBodyRegionCategoryRepository _bodyRegionRepository;
    private readonly ILogger<TreatmentService> _logger;

    public TreatmentService(
        ITreatmentRepository treatmentRepository,
        IBodyRegionCategoryRepository bodyRegionRepository,
        ILogger<TreatmentService> logger)
    {
        _treatmentRepository = treatmentRepository;
        _bodyRegionRepository = bodyRegionRepository;
        _logger = logger;
    }

    public async Task<Result<LibraryItemListResponse<TreatmentDto>>> GetTreatmentsAsync(Guid? bodyRegionId, string? search, int page, int pageSize)
    {
        var query = _treatmentRepository.Query()
            .Include(t => t.BodyRegionCategory)
            .Where(t => !t.IsDeleted);

        if (bodyRegionId.HasValue)
            query = query.Where(t => t.BodyRegionCategoryId == bodyRegionId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Name.ToLower().Contains(search.ToLower()));

        var totalCount = await query.CountAsync();

        var treatments = (await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync())
            .Adapt<List<TreatmentDto>>();

        var response = LibraryItemListResponse<TreatmentDto>.Create(treatments, page, pageSize, totalCount);
        return Result<LibraryItemListResponse<TreatmentDto>>.Success(response);
    }

    public async Task<Result<TreatmentDto>> GetTreatmentByIdAsync(Guid id)
    {
        var treatment = await _treatmentRepository.GetWithDetailsAsync(id);

        if (treatment == null)
            return Result<TreatmentDto>.Failure("Treatment not found");

        return Result<TreatmentDto>.Success(treatment.Adapt<TreatmentDto>());
    }

    public async Task<Result<TreatmentDto>> CreateTreatmentAsync(CreateTreatmentRequest request, Guid? clinicId)
    {
        var bodyRegionExists = await _bodyRegionRepository.AnyAsync(b => b.Id == request.BodyRegionCategoryId && b.IsActive);
        if (!bodyRegionExists)
            return Result<TreatmentDto>.Failure("Body region category not found");

        var treatment = new Treatment
        {
            ClinicId = clinicId,
            Code = request.Code,
            Name = request.Name,
            NameArabic = request.NameArabic,
            BodyRegionCategoryId = request.BodyRegionCategoryId,
            AffectedArea = request.AffectedArea,
            MinDurationWeeks = request.MinDurationWeeks,
            MaxDurationWeeks = request.MaxDurationWeeks,
            ExpectedSessions = request.ExpectedSessions,
            Description = request.Description,
            RedFlags = request.RedFlags,
            Contraindications = request.Contraindications,
            ClinicalNotes = request.ClinicalNotes,
            SourceReference = request.SourceReference,
            SourceDetails = request.SourceDetails,
            AccessTier = request.AccessTier,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _treatmentRepository.AddAsync(treatment);
        await _treatmentRepository.SaveChangesAsync();

        _logger.LogInformation("Created treatment {TreatmentId} ({TreatmentName}) for clinic {ClinicId}",
            treatment.Id, treatment.Name, clinicId ?? Guid.Empty);

        var saved = await _treatmentRepository.Query()
            .AsNoTracking()
            .Include(t => t.BodyRegionCategory)
            .FirstOrDefaultAsync(t => t.Id == treatment.Id);
        return Result<TreatmentDto>.Success(saved!.Adapt<TreatmentDto>());
    }

    public async Task<Result<TreatmentDto>> UpdateTreatmentAsync(Guid id, UpdateTreatmentRequest request, Guid clinicId)
    {
        var treatment = await _treatmentRepository.GetWithDetailsAsync(id);
        if (treatment == null)
            return Result<TreatmentDto>.Failure("Treatment not found");

        if (clinicId != Guid.Empty && treatment.ClinicId != clinicId)
            return Result<TreatmentDto>.Failure("You can only update treatments owned by your clinic");

        if (request.BodyRegionCategoryId != treatment.BodyRegionCategoryId)
        {
            var bodyRegionExists = await _bodyRegionRepository.AnyAsync(b => b.Id == request.BodyRegionCategoryId && b.IsActive);
            if (!bodyRegionExists)
                return Result<TreatmentDto>.Failure("Body region category not found");
        }

        treatment.Name = request.Name;
        treatment.NameArabic = request.NameArabic;
        treatment.BodyRegionCategoryId = request.BodyRegionCategoryId;
        treatment.AffectedArea = request.AffectedArea;
        treatment.MinDurationWeeks = request.MinDurationWeeks;
        treatment.MaxDurationWeeks = request.MaxDurationWeeks;
        treatment.ExpectedSessions = request.ExpectedSessions;
        treatment.Description = request.Description;
        treatment.RedFlags = request.RedFlags;
        treatment.Contraindications = request.Contraindications;
        treatment.ClinicalNotes = request.ClinicalNotes;
        treatment.SourceReference = request.SourceReference;
        treatment.SourceDetails = request.SourceDetails;
        treatment.AccessTier = request.AccessTier;
        treatment.UpdatedAt = DateTime.UtcNow;

        await _treatmentRepository.UpdateAsync(treatment);
        await _treatmentRepository.SaveChangesAsync();

        _logger.LogInformation("Updated treatment {TreatmentId} ({TreatmentName})", treatment.Id, treatment.Name);

        var saved = await _treatmentRepository.Query()
            .AsNoTracking()
            .Include(t => t.BodyRegionCategory)
            .FirstOrDefaultAsync(t => t.Id == treatment.Id);
        return Result<TreatmentDto>.Success(saved!.Adapt<TreatmentDto>());
    }

    public async Task<Result> DeleteTreatmentAsync(Guid id, Guid clinicId)
    {
        var treatment = await _treatmentRepository.GetByIdAsync(id);
        if (treatment == null)
            return Result.Failure("Treatment not found");

        if (clinicId != Guid.Empty && treatment.ClinicId != clinicId)
            return Result.Failure("You can only delete treatments owned by your clinic");

        treatment.IsDeleted = true;
        treatment.DeletedAt = DateTime.UtcNow;

        await _treatmentRepository.UpdateAsync(treatment);
        await _treatmentRepository.SaveChangesAsync();

        _logger.LogInformation("Soft deleted treatment {TreatmentId} ({TreatmentName})", treatment.Id, treatment.Name);

        return Result.Success();
    }

}
