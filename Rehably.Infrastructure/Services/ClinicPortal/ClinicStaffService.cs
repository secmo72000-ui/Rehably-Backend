using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.ClinicPortal;
using Rehably.Application.Services.ClinicPortal;
using Rehably.Domain.Entities.Identity;
using Rehably.Domain.Enums;

namespace Rehably.Infrastructure.Services.ClinicPortal;

public class ClinicStaffService : IClinicStaffService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ClinicStaffService> _logger;

    public ClinicStaffService(UserManager<ApplicationUser> userManager, ILogger<ClinicStaffService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Result<PagedResult<StaffMemberDto>>> GetStaffAsync(Guid clinicId, StaffQueryParams query, CancellationToken ct = default)
    {
        try
        {
            var q = _userManager.Users
                .Where(u => u.ClinicId == clinicId && u.RoleType != RoleType.SuperAdmin);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.ToLower();
                q = q.Where(u => (u.FirstName + " " + u.LastName).ToLower().Contains(s)
                              || u.Email!.ToLower().Contains(s));
            }

            if (query.IsActive.HasValue)
                q = q.Where(u => u.IsActive == query.IsActive.Value);

            var total = await q.CountAsync(ct);
            var users = await q.OrderBy(u => u.FirstName)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(ct);

            var dtos = new List<StaffMemberDto>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                dtos.Add(MapToDto(u, roles));
            }

            return Result<PagedResult<StaffMemberDto>>.Success(
                PagedResult<StaffMemberDto>.Create(dtos, total, query.Page, query.PageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting staff for clinic {ClinicId}", clinicId);
            return Result<PagedResult<StaffMemberDto>>.Failure("Failed to retrieve staff");
        }
    }

    public async Task<Result<StaffMemberDto>> GetStaffByIdAsync(Guid clinicId, string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.ClinicId != clinicId)
            return Result<StaffMemberDto>.Failure("Staff member not found");

        var roles = await _userManager.GetRolesAsync(user);
        return Result<StaffMemberDto>.Success(MapToDto(user, roles));
    }

    public async Task<Result<StaffMemberDto>> InviteStaffAsync(Guid clinicId, InviteStaffRequest request, CancellationToken ct = default)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null)
            return Result<StaffMemberDto>.Failure("Email already in use");

        var user = new ApplicationUser
        {
            UserName          = request.Email,
            Email             = request.Email,
            FirstName         = request.FirstName,
            LastName          = request.LastName,
            PhoneNumber       = request.PhoneNumber,
            ClinicId          = clinicId,
            TenantId          = clinicId,
            RoleType          = RoleType.Staff,
            IsActive          = true,
            MustChangePassword = true,
            CreatedAt         = DateTime.UtcNow,
        };

        var tempPassword = $"Rehably@{Guid.NewGuid().ToString("N")[..8]}";
        var result = await _userManager.CreateAsync(user, tempPassword);
        if (!result.Succeeded)
            return Result<StaffMemberDto>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));

        if (!string.IsNullOrWhiteSpace(request.Role))
            await _userManager.AddToRoleAsync(user, request.Role);

        var roles = await _userManager.GetRolesAsync(user);
        return Result<StaffMemberDto>.Success(MapToDto(user, roles));
    }

    public async Task<Result<StaffMemberDto>> UpdateStaffAsync(Guid clinicId, string userId, UpdateStaffRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.ClinicId != clinicId)
            return Result<StaffMemberDto>.Failure("Staff member not found");

        if (request.FirstName != null) user.FirstName = request.FirstName;
        if (request.LastName  != null) user.LastName  = request.LastName;
        if (request.PhoneNumber != null) user.PhoneNumber = request.PhoneNumber;
        if (request.IsActive.HasValue)  user.IsActive  = request.IsActive.Value;

        await _userManager.UpdateAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        return Result<StaffMemberDto>.Success(MapToDto(user, roles));
    }

    public async Task<Result> DeactivateStaffAsync(Guid clinicId, string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.ClinicId != clinicId)
            return Result.Failure("Staff member not found");
        user.IsActive = false;
        await _userManager.UpdateAsync(user);
        return Result.Success();
    }

    public async Task<Result> ReactivateStaffAsync(Guid clinicId, string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.ClinicId != clinicId)
            return Result.Failure("Staff member not found");
        user.IsActive = true;
        await _userManager.UpdateAsync(user);
        return Result.Success();
    }

    private static StaffMemberDto MapToDto(ApplicationUser u, IList<string> roles) => new()
    {
        Id             = u.Id,
        FirstName      = u.FirstName ?? string.Empty,
        LastName       = u.LastName  ?? string.Empty,
        Email          = u.Email     ?? string.Empty,
        PhoneNumber    = u.PhoneNumber,
        ProfileImageUrl= u.ProfileImageUrl,
        IsActive       = u.IsActive,
        Roles          = roles.ToList(),
        LastLoginAt    = u.LastLoginAt,
        CreatedAt      = u.CreatedAt,
    };
}
