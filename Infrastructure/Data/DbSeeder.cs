using Microsoft.EntityFrameworkCore;
using Projekt.Infrastructure.Entities;

namespace Projekt.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Seed Roles
        if (!await context.Roles.AnyAsync())
        {
            var roles = new[]
            {
                new Role { Name = "user" },
                new Role { Name = "admin" }
            };

            context.Roles.AddRange(roles);
            await context.SaveChangesAsync();
        }
    }
}

