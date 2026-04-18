using Microsoft.EntityFrameworkCore;
using Rehably.Application.DTOs.Audit;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Identity;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Repositories;

/// <summary>
/// Implementation of IUserRepository
/// </summary>
public class UserRepository : Repository<ApplicationUser>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
    }

    public async Task<ApplicationUser?> GetByPhoneNumberAsync(string phoneNumber)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
    }

    public async Task<IEnumerable<ApplicationUser>> GetByClinicIdAsync(Guid clinicId)
    {
        return await _dbSet
            .Where(u => u.ClinicId == clinicId)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();
    }

    public async Task<IEnumerable<ApplicationUser>> GetActiveByClinicIdAsync(Guid clinicId)
    {
        return await _dbSet
            .Where(u => u.ClinicId == clinicId && u.IsActive)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();
    }

    public async Task<IEnumerable<ApplicationUser>> GetByRoleIdAsync(string roleId)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
            .Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId))
            .ToListAsync();
    }

    public async Task<ApplicationUser?> GetWithRolesAsync(string userId)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<bool> IsEmailUniqueAsync(string email, string? excludeUserId = null)
    {
        var query = _dbSet.Where(u => u.Email == email.ToLowerInvariant());

        if (!string.IsNullOrEmpty(excludeUserId))
        {
            query = query.Where(u => u.Id != excludeUserId);
        }

        return !await query.AnyAsync();
    }

    public async Task<IEnumerable<ApplicationUser>> GetUsersMustChangePasswordAsync(Guid clinicId)
    {
        return await _dbSet
            .Where(u => u.ClinicId == clinicId && u.MustChangePassword)
            .ToListAsync();
    }

    public async Task<ApplicationUser?> GetByIdAsync(string userId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<Dictionary<string, string?>> GetUserEmailsByIdsAsync(IEnumerable<string> userIds)
    {
        var idsList = userIds.ToList();
        return await _dbSet
            .Where(u => idsList.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email);
    }

    public async Task<Dictionary<string, UserAuditInfoDto>> GetUserAuditInfoByIdsAsync(IEnumerable<string> userIds)
    {
        var idsList = userIds.ToList();
        var users = await _dbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => idsList.Contains(u.Id))
            .ToListAsync();

        return users.ToDictionary(
            u => u.Id,
            u => new UserAuditInfoDto
            {
                Email = u.Email,
                RoleName = u.UserRoles.Select(ur => ur.Role?.Name).FirstOrDefault()
                    ?? u.RoleType.ToString()
            });
    }

    public async Task<ApplicationUser?> GetByResetTokenSelectorAsync(string selector)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.ResetTokenSelector == selector && u.ResetTokenExpiry > DateTime.UtcNow);
    }
}
