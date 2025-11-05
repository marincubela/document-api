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

        // Seed Admin User
        var adminEmail = "admin@documentapi.com";
        var adminUser = await context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == adminEmail);

        if (adminUser == null)
        {
            // Create admin user
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");

            adminUser = new User
            {
                Id = Guid.NewGuid(),
                Email = adminEmail,
                PasswordHash = passwordHash,
                DisplayName = "System Administrator",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();

            // Assign both user and admin roles
            var userRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "user");
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "admin");

            if (userRole != null)
            {
                context.UserRoles.Add(new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = userRole.Id
                });
            }

            if (adminRole != null)
            {
                context.UserRoles.Add(new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id
                });
            }

            await context.SaveChangesAsync();
        }
    }
}

