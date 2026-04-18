using Microsoft.AspNetCore.Identity;
using Rehably.Domain.Entities.Identity;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Seed;

public static class RoleSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, RoleManager<ApplicationRole> roleManager)
    {
        var roles = new List<(string Name, string Description)>
        {
            ("PlatformAdmin", "Full system access"),
            ("ClinicOwner", "Clinic owner with full access to their clinic"),
            ("Doctor", "Doctor with patient access"),
            ("Receptionist", "Receptionist with appointment management"),
            ("User", "Basic user access")
        };

        foreach (var (name, description) in roles)
        {
            if (!await roleManager.RoleExistsAsync(name))
            {
                var result = await roleManager.CreateAsync(new ApplicationRole
                {
                    Name = name,
                    NormalizedName = name.ToUpperInvariant(),
                    Description = description
                });
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to create role {name}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}
