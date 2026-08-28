using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotesAndFileBackend.Core.Entities;
using NotesAndFileBackend.Infrastructure.Data;

namespace NotesAndFileBackend.Api.Services;

public class AdminSeederService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AdminSeederService> _logger;

    public AdminSeederService(IServiceProvider serviceProvider, ILogger<AdminSeederService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 16)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Ensure database is created/migrated first before seeding
        try
        {
            await context.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while migrating the database.");
            return;
        }

        var adminEmail = "admin@notesandfile.local";

        if (!await context.Users.AnyAsync(u => u.Email == adminEmail, cancellationToken))
        {
            var password = GenerateRandomPassword();
            var adminUser = new User
            {
                Email = adminEmail,
                DisplayName = "Admin",
                PasswordHash = HashPassword(password),
                Status = "ACTIVE"
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogWarning("=================================================");
            _logger.LogWarning("DEFAULT ADMIN USER CREATED");
            _logger.LogWarning($"Email: {adminEmail}");
            _logger.LogWarning($"Password: {password}");
            _logger.LogWarning("Please copy this password. It will not be shown again.");
            _logger.LogWarning("=================================================");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
