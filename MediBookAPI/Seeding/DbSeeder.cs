using Data.Enums;
using Data.Models;
using Microsoft.EntityFrameworkCore;

namespace MediBookAPI.Seeding;

public static class DbSeeder
{
    // Idempotent: safe to run on every startup. Only seeds when SuperAdmin:Password is
    // configured, so production environments without that setting stay untouched by default.
    public static async Task SeedSuperAdminAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<BookingDBContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        var email = configuration["SuperAdmin:Email"];
        var password = configuration["SuperAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("SuperAdmin:Email or SuperAdmin:Password is not configured; skipping super admin seed.");
            return;
        }

        var exists = await context.tbUsers.AnyAsync(u => u.Email == email);
        if (exists)
        {
            logger.LogInformation("Super admin account {Email} already exists; skipping seed.", email);
            return;
        }

        context.tbUsers.Add(new tbUsers
        {
            Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.Admin,
            Created_At = DateTimeOffset.UtcNow
        });

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded super admin account {Email}.", email);
    }
}
