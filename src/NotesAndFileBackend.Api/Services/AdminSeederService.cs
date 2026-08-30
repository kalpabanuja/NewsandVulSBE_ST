using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotesAndFileBackend.Domain.Entities;
using NotesAndFileBackend.Infrastructure.Data;

namespace NotesAndFileBackend.Api.Services;

public static class AdminSeeder
{
    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 16).Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public static async Task SeedAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Ensure database is created/migrated first before seeding
        try
        {
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database.");
            return;
        }

        var adminEmail = "admin@notesandfile.local";

        if (!await context.Users.AnyAsync(u => u.Email == adminEmail))
        {
            var password = GenerateRandomPassword();
            var adminUser = new User
            {
                Email = adminEmail,
                NormalizedEmail = adminEmail.ToUpperInvariant(),
                DisplayName = "Admin",
                PasswordHash = HashPassword(password),
                Status = "ACTIVE"
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();

            logger.LogWarning("=================================================");
            logger.LogWarning("DEFAULT ADMIN USER CREATED");
            logger.LogWarning($"Email: {adminEmail}");
            logger.LogWarning($"Password: {password}");
            logger.LogWarning("Please copy this password. It will not be shown again.");
            logger.LogWarning("=================================================");
        }
    }

    /// <summary>
    /// Force-resets the admin password and logs the new one. Call this when the original password is lost.
    /// </summary>
    public static async Task ResetPasswordAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var adminEmail = "admin@notesandfile.local";
        var admin = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (admin is null)
        {
            logger.LogError("Admin user not found. Run SeedAsync first.");
            return;
        }

        var newPassword = GenerateRandomPassword();
        admin.PasswordHash = HashPassword(newPassword);
        await context.SaveChangesAsync();

        logger.LogWarning("=================================================");
        logger.LogWarning("ADMIN PASSWORD HAS BEEN RESET");
        logger.LogWarning($"Email: {adminEmail}");
        logger.LogWarning($"New Password: {newPassword}");
        logger.LogWarning("Please copy this password. It will not be shown again.");
        logger.LogWarning("=================================================");
    }
}
