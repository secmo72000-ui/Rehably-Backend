using Rehably.Application.Common;
using Rehably.Application.DTOs.Admin;

namespace Rehably.Application.Services.Admin;

public interface IPlatformAdminManagementService
{
    Task<Result<PlatformAdminResponse>> CreateAdminAsync(CreatePlatformAdminRequest request);
    Task<Result<PlatformAdminResponse>> UpdateAdminAsync(string userId, UpdatePlatformAdminRequest request);
    Task<Result> ChangeAdminRoleAsync(string userId, ChangeAdminRoleRequest request, string currentUserId);
    Task<Result> DeleteAdminAsync(string userId, string currentUserId);
}
