using Rehably.Application.DTOs.Audit;
using Rehably.Domain.Entities.Identity;

namespace Rehably.Application.Repositories;

public interface IUserRepository : IRepository<ApplicationUser>
{
    Task<ApplicationUser?> GetByEmailAsync(string email);
    Task<ApplicationUser?> GetByPhoneNumberAsync(string phoneNumber);
    Task<IEnumerable<ApplicationUser>> GetByClinicIdAsync(Guid clinicId);
    Task<IEnumerable<ApplicationUser>> GetActiveByClinicIdAsync(Guid clinicId);
    Task<IEnumerable<ApplicationUser>> GetByRoleIdAsync(string roleId);
    Task<ApplicationUser?> GetWithRolesAsync(string userId);
    Task<bool> IsEmailUniqueAsync(string email, string? excludeUserId = null);
    Task<IEnumerable<ApplicationUser>> GetUsersMustChangePasswordAsync(Guid clinicId);
    Task<ApplicationUser?> GetByIdAsync(string userId);
    Task<Dictionary<string, string?>> GetUserEmailsByIdsAsync(IEnumerable<string> userIds);
    Task<Dictionary<string, UserAuditInfoDto>> GetUserAuditInfoByIdsAsync(IEnumerable<string> userIds);
    Task<ApplicationUser?> GetByResetTokenSelectorAsync(string selector);
}
