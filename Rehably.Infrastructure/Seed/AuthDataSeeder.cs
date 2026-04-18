using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Rehably.Domain.Entities.Identity;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Seed;

public static class AuthDataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, IWebHostEnvironment environment)
    {
        // Always seed the platform admin if none exists (any environment)
        var adminUser = await context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == "admin@rehably.com");

        if (adminUser == null)
        {
            var admin = new ApplicationUser
            {
                Id = "seed-admin-001",
                Email = "admin@rehably.com",
                UserName = "admin@rehably.com",
                NormalizedEmail = "ADMIN@REHABLY.COM",
                NormalizedUserName = "ADMIN@REHABLY.COM",
                FirstName = "Platform",
                LastName = "Admin",
                MustChangePassword = false,
                IsActive = true,
                TenantId = null,
                ClinicId = null,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(admin, "Admin@Rehably2026!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "PlatformAdmin");
        }

        // Test users — development only
        if (!environment.IsDevelopment())
            return;

        var testUsers = new List<ApplicationUser>
        {
            new()
            {
                Id = "test-user-001",
                Email = "clinic.owner@test.com",
                UserName = "clinic.owner@test.com",
                NormalizedEmail = "CLINIC.OWNER@TEST.COM",
                NormalizedUserName = "CLINIC.OWNER@TEST.COM",
                FirstName = "Dr.",
                LastName = "Test Owner",
                MustChangePassword = true,
                IsActive = true,
                TenantId = null,
                ClinicId = null,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "test-user-002",
                Email = "user@test.com",
                UserName = "user@test.com",
                NormalizedEmail = "USER@TEST.COM",
                NormalizedUserName = "USER@TEST.COM",
                FirstName = "Test",
                LastName = "User",
                MustChangePassword = false,
                IsActive = true,
                TenantId = null,
                ClinicId = null,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "test-user-003",
                Email = "disabled@test.com",
                UserName = "disabled@test.com",
                NormalizedEmail = "DISABLED@TEST.COM",
                NormalizedUserName = "DISABLED@TEST.COM",
                FirstName = "Disabled",
                LastName = "User",
                MustChangePassword = false,
                IsActive = false,
                TenantId = null,
                ClinicId = null,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        foreach (var user in testUsers)
        {
            var result = await userManager.CreateAsync(user, "TempPassword123!");
            if (!result.Succeeded)
            {
                throw new Exception($"Failed to create user {user.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            if (user.Email.Contains("owner"))
            {
                await userManager.AddToRoleAsync(user, "ClinicOwner");
            }
            else
            {
                await userManager.AddToRoleAsync(user, "User");
            }
        }
    }
}
