using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Library;
using Rehably.Domain.Entities.Library;

namespace Rehably.Infrastructure.Services.Library;

public class AssessmentService : IAssessmentService
{
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly ILogger<AssessmentService> _logger;

    public AssessmentService(IAssessmentRepository assessmentRepository, ILogger<AssessmentService> logger)
    {
        _assessmentRepository = assessmentRepository;
        _logger = logger;
    }

    public async Task<Result<LibraryItemListResponse<AssessmentDto>>> GetAssessmentsAsync(Guid? bodyRegionId, string? search, int page, int pageSize)
    {
        var query = _assessmentRepository.Query().Where(a => !a.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.Name.ToLower().Contains(search.ToLower()));

        var totalCount = await query.CountAsync();

        var assessments = (await query
            .OrderBy(a => a.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync())
            .Adapt<List<AssessmentDto>>();

        var response = LibraryItemListResponse<AssessmentDto>.Create(assessments, page, pageSize, totalCount);
        return Result<LibraryItemListResponse<AssessmentDto>>.Success(response);
    }

    public async Task<Result<AssessmentDto>> GetAssessmentByIdAsync(Guid id)
    {
        var assessment = await _assessmentRepository.GetWithDetailsAsync(id);
        if (assessment == null)
            return Result<AssessmentDto>.Failure("Assessment not found");

        return Result<AssessmentDto>.Success(assessment.Adapt<AssessmentDto>());
    }

    public async Task<Result<AssessmentDto>> CreateAssessmentAsync(CreateAssessmentRequest request, Guid? clinicId)
    {
        var assessment = new Assessment
        {
            ClinicId = clinicId,
            Code = request.Code,
            Name = request.Name,
            NameArabic = request.NameArabic,
            TimePoint = request.TimePoint,
            Description = request.Description,
            Instructions = request.Instructions,
            ScoringGuide = request.ScoringGuide,
            RelatedConditionCodes = request.RelatedConditionCodes,
            AccessTier = request.AccessTier,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _assessmentRepository.AddAsync(assessment);
        await _assessmentRepository.SaveChangesAsync();

        _logger.LogInformation("Created assessment {AssessmentId} ({AssessmentName}) for clinic {ClinicId}",
            assessment.Id, assessment.Name, clinicId ?? Guid.Empty);

        return Result<AssessmentDto>.Success(assessment.Adapt<AssessmentDto>());
    }

    public async Task<Result<AssessmentDto>> UpdateAssessmentAsync(Guid id, UpdateAssessmentRequest request, Guid clinicId)
    {
        var assessment = await _assessmentRepository.GetWithDetailsAsync(id);
        if (assessment == null)
            return Result<AssessmentDto>.Failure("Assessment not found");

        if (clinicId != Guid.Empty && assessment.ClinicId != clinicId)
            return Result<AssessmentDto>.Failure("You can only update assessments owned by your clinic");

        assessment.Name = request.Name;
        assessment.NameArabic = request.NameArabic;
        assessment.TimePoint = request.TimePoint;
        assessment.Description = request.Description;
        assessment.Instructions = request.Instructions;
        assessment.ScoringGuide = request.ScoringGuide;
        assessment.RelatedConditionCodes = request.RelatedConditionCodes;
        assessment.AccessTier = request.AccessTier;
        assessment.UpdatedAt = DateTime.UtcNow;

        await _assessmentRepository.UpdateAsync(assessment);
        await _assessmentRepository.SaveChangesAsync();

        _logger.LogInformation("Updated assessment {AssessmentId} ({AssessmentName})", assessment.Id, assessment.Name);

        return Result<AssessmentDto>.Success(assessment.Adapt<AssessmentDto>());
    }

    public async Task<Result> DeleteAssessmentAsync(Guid id, Guid clinicId)
    {
        var assessment = await _assessmentRepository.GetByIdAsync(id);
        if (assessment == null)
            return Result.Failure("Assessment not found");

        if (clinicId != Guid.Empty && assessment.ClinicId != clinicId)
            return Result.Failure("You can only delete assessments owned by your clinic");

        assessment.IsDeleted = true;
        assessment.UpdatedAt = DateTime.UtcNow;

        await _assessmentRepository.UpdateAsync(assessment);
        await _assessmentRepository.SaveChangesAsync();

        _logger.LogInformation("Soft deleted assessment {AssessmentId} ({AssessmentName})", assessment.Id, assessment.Name);

        return Result.Success();
    }

}
