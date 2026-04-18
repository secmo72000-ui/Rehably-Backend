using Microsoft.AspNetCore.Identity;
using Rehably.Application.DTOs.Admin;
using Rehably.Application.Services.Admin;
using Rehably.Application.Common;
using Rehably.Application.Services.Auth;
using Rehably.Domain.Entities.Identity;
using System.Security.Cryptography;
using System.Text;

namespace Rehably.Infrastructure.Services.Admin;

public class PlatformAdminManagementService : IPlatformAdminManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IAuthService _authService;
    private readonly IAuthPasswordService _authPasswordService;

    public PlatformAdminManagementService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IAuthService authService,
        IAuthPasswordService authPasswordService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _authService = authService;
        _authPasswordService = authPasswordService;
    }

    public async Task<Result<PlatformAdminResponse>> CreateAdminAsync(CreatePlatformAdminRequest request)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(request.RoleId);
            if (role == null)
            {
                return Result<PlatformAdminResponse>.Failure($"Role with ID '{request.RoleId}' not found");
            }

            if (role.TenantId != null)
            {
                return Result<PlatformAdminResponse>.Failure("Only platform-level roles can be assigned to platform admins");
            }

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return Result<PlatformAdminResponse>.Failure($"User with email '{request.Email}' already exists");
            }

            var tempPassword = GenerateTemporaryPassword();

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = request.Email,
                UserName = request.Email,
                NormalizedEmail = request.Email.ToUpperInvariant(),
                NormalizedUserName = request.Email.ToUpperInvariant(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                MustChangePassword = true,
                IsActive = true,
                TenantId = null,
                ClinicId = null,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, tempPassword);
            if (!createResult.Succeeded)
            {
                return Result<PlatformAdminResponse>.Failure(
                    $"Failed to create user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, role.Name ?? string.Empty);
            if (!addRoleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return Result<PlatformAdminResponse>.Failure(
                    $"Failed to assign role: {string.Join(", ", addRoleResult.Errors.Select(e => e.Description))}");
            }

            var claims = await _roleManager.GetClaimsAsync(role);
            var permissions = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList();
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name ?? string.Empty);

            var roleResponse = new PlatformRoleResponse
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                Description = role.Description,
                Permissions = permissions,
                UserCount = usersInRole.Count(u => u.IsActive),
                CreatedAt = role.CreatedAt
            };

            var response = new PlatformAdminResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                IsActive = user.IsActive,
                Role = roleResponse,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            var token = await _authPasswordService.GeneratePasswordResetTokenAsync(user.Email!);
            await _authService.SendWelcomeEmailAsync(user.Email!, token, "Rehably Platform", $"{user.FirstName} {user.LastName}");

            return Result<PlatformAdminResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<PlatformAdminResponse>.Failure($"Failed to create admin user: {ex.Message}");
        }
    }

    public async Task<Result<PlatformAdminResponse>> UpdateAdminAsync(string userId, UpdatePlatformAdminRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<PlatformAdminResponse>.Failure($"Admin user with ID '{userId}' not found");
            }

            if (user.TenantId != null)
            {
                return Result<PlatformAdminResponse>.Failure("This is not a platform admin user");
            }

            if (request.FirstName != null)
            {
                user.FirstName = request.FirstName;
            }

            if (request.LastName != null)
            {
                user.LastName = request.LastName;
            }

            if (request.IsActive.HasValue)
            {
                user.IsActive = request.IsActive.Value;
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return Result<PlatformAdminResponse>.Failure(
                    $"Failed to update user: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault();
            var role = !string.IsNullOrEmpty(roleName)
                ? await _roleManager.FindByNameAsync(roleName)
                : null;

            var roleResponse = new PlatformRoleResponse();
            if (role != null)
            {
                var claims = await _roleManager.GetClaimsAsync(role);
                var permissions = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList();
                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name ?? string.Empty);

                roleResponse = new PlatformRoleResponse
                {
                    Id = role.Id,
                    Name = role.Name ?? string.Empty,
                    Description = role.Description,
                    Permissions = permissions,
                    UserCount = usersInRole.Count(u => u.IsActive),
                    CreatedAt = role.CreatedAt
                };
            }

            var response = new PlatformAdminResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                IsActive = user.IsActive,
                Role = roleResponse,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            return Result<PlatformAdminResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<PlatformAdminResponse>.Failure($"Failed to update admin user: {ex.Message}");
        }
    }

    public async Task<Result> ChangeAdminRoleAsync(string userId, ChangeAdminRoleRequest request, string currentUserId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result.Failure($"Admin user with ID '{userId}' not found");
            }

            if (user.TenantId != null)
            {
                return Result.Failure("This is not a platform admin user");
            }

            var newRole = await _roleManager.FindByIdAsync(request.RoleId);
            if (newRole == null)
            {
                return Result.Failure($"Role with ID '{request.RoleId}' not found");
            }

            if (newRole.TenantId != null)
            {
                return Result.Failure("Only platform-level roles can be assigned to platform admins");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            foreach (var roleName in currentRoles)
            {
                await _userManager.RemoveFromRoleAsync(user, roleName);
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, newRole.Name ?? string.Empty);
            if (!addRoleResult.Succeeded)
            {
                return Result.Failure(
                    $"Failed to assign new role: {string.Join(", ", addRoleResult.Errors.Select(e => e.Description))}");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to change admin role: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAdminAsync(string userId, string currentUserId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result.Failure($"Admin user with ID '{userId}' not found");
            }

            if (user.TenantId != null)
            {
                return Result.Failure("This is not a platform admin user");
            }

            user.IsActive = false;
            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                return Result.Failure(
                    $"Failed to deactivate user: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete admin user: {ex.Message}");
        }
    }

    private static string GenerateTemporaryPassword()
    {
        const int length = 12;
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%^&*";

        using var rng = RandomNumberGenerator.Create();
        var result = new StringBuilder(length);
        var buffer = new byte[length];

        rng.GetBytes(buffer);
        for (int i = 0; i < length; i++)
        {
            result.Append(chars[buffer[i] % chars.Length]);
        }

        return result.ToString();
    }
}
