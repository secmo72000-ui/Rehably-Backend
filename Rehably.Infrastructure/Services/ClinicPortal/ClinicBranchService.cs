using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.ClinicPortal;
using Rehably.Application.Services.ClinicPortal;
using Rehably.Domain.Entities.Tenant;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Services.ClinicPortal;

public class ClinicBranchService : IClinicBranchService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ClinicBranchService> _logger;

    public ClinicBranchService(ApplicationDbContext context, ILogger<ClinicBranchService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<List<BranchDto>>> GetBranchesAsync(Guid clinicId, CancellationToken ct = default)
    {
        try
        {
            var branches = await _context.ClinicBranches
                .Where(b => b.ClinicId == clinicId)
                .OrderByDescending(b => b.IsMain)
                .ThenBy(b => b.Name)
                .Select(b => MapToDto(b))
                .ToListAsync(ct);

            return Result<List<BranchDto>>.Success(branches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting branches for clinic {ClinicId}", clinicId);
            return Result<List<BranchDto>>.Failure("Failed to retrieve branches");
        }
    }

    public async Task<Result<BranchDto>> GetBranchByIdAsync(Guid clinicId, Guid branchId, CancellationToken ct = default)
    {
        var branch = await _context.ClinicBranches
            .FirstOrDefaultAsync(b => b.Id == branchId && b.ClinicId == clinicId, ct);

        return branch == null
            ? Result<BranchDto>.Failure("Branch not found")
            : Result<BranchDto>.Success(MapToDto(branch));
    }

    public async Task<Result<BranchDto>> CreateBranchAsync(Guid clinicId, CreateBranchRequest request, CancellationToken ct = default)
    {
        try
        {
            // If new branch is main, unset existing main
            if (request.IsMain)
            {
                var currentMain = await _context.ClinicBranches
                    .Where(b => b.ClinicId == clinicId && b.IsMain)
                    .ToListAsync(ct);
                currentMain.ForEach(b => b.IsMain = false);
            }

            var branch = new ClinicBranch
            {
                ClinicId   = clinicId,
                Name       = request.Name,
                NameArabic = request.NameArabic,
                Phone      = request.Phone,
                Email      = request.Email,
                Address    = request.Address,
                City       = request.City,
                IsMain     = request.IsMain,
                IsActive   = true,
                CreatedAt  = DateTime.UtcNow,
            };

            _context.ClinicBranches.Add(branch);
            await _context.SaveChangesAsync(ct);

            return Result<BranchDto>.Success(MapToDto(branch));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating branch for clinic {ClinicId}", clinicId);
            return Result<BranchDto>.Failure("Failed to create branch");
        }
    }

    public async Task<Result<BranchDto>> UpdateBranchAsync(Guid clinicId, Guid branchId, UpdateBranchRequest request, CancellationToken ct = default)
    {
        var branch = await _context.ClinicBranches
            .FirstOrDefaultAsync(b => b.Id == branchId && b.ClinicId == clinicId, ct);

        if (branch == null) return Result<BranchDto>.Failure("Branch not found");

        if (request.Name       != null) branch.Name       = request.Name;
        if (request.NameArabic != null) branch.NameArabic = request.NameArabic;
        if (request.Phone      != null) branch.Phone      = request.Phone;
        if (request.Email      != null) branch.Email      = request.Email;
        if (request.Address    != null) branch.Address    = request.Address;
        if (request.City       != null) branch.City       = request.City;
        if (request.IsActive.HasValue)  branch.IsActive   = request.IsActive.Value;
        branch.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return Result<BranchDto>.Success(MapToDto(branch));
    }

    public async Task<Result> DeleteBranchAsync(Guid clinicId, Guid branchId, CancellationToken ct = default)
    {
        var branch = await _context.ClinicBranches
            .FirstOrDefaultAsync(b => b.Id == branchId && b.ClinicId == clinicId, ct);

        if (branch == null) return Result.Failure("Branch not found");
        if (branch.IsMain)  return Result.Failure("Cannot delete main branch");

        _context.ClinicBranches.Remove(branch);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static BranchDto MapToDto(ClinicBranch b) => new()
    {
        Id         = b.Id,
        Name       = b.Name,
        NameArabic = b.NameArabic,
        Phone      = b.Phone,
        Email      = b.Email,
        Address    = b.Address,
        City       = b.City,
        IsMain     = b.IsMain,
        IsActive   = b.IsActive,
        CreatedAt  = b.CreatedAt,
    };
}
