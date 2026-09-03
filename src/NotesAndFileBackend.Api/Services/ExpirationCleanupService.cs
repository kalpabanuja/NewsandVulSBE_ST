using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotesAndFileBackend.Infrastructure.Data;

namespace NotesAndFileBackend.Api.Services;

public class ExpirationCleanupService : BackgroundService
{
    private readonly ILogger<ExpirationCleanupService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ExpirationCleanupService(ILogger<ExpirationCleanupService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Expiration Cleanup Service running.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredResourcesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing Expiration Cleanup.");
            }

            // Run every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task CleanupExpiredResourcesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;

        // Cleanup expired files
        var expiredFiles = await context.Files
            .Where(f => f.Status == "ACTIVE" && f.RetentionExpiresAt <= now)
            .ToListAsync(stoppingToken);

        foreach (var file in expiredFiles)
        {
            file.Status = "DELETED";
            file.DeletedAt = now;
            _logger.LogInformation($"File {file.Id} marked as DELETED due to expiration.");
        }

        // Cleanup expired public file shares
        var expiredFileShares = await context.PublicFileShares
            .Where(s => s.ExpiresAt <= now && s.RevokedAt == null)
            .ToListAsync(stoppingToken);

        foreach (var share in expiredFileShares)
        {
            share.RevokedAt = now;
            _logger.LogInformation($"Public File Share {share.Id} revoked due to expiration.");
        }

        // Cleanup expired public Note shares
        var expiredDocShares = await context.PublicNoteShares
            .Where(s => s.ExpiresAt <= now && s.RevokedAt == null)
            .ToListAsync(stoppingToken);

        foreach (var share in expiredDocShares)
        {
            share.RevokedAt = now;
            _logger.LogInformation($"Public Note Share {share.Id} revoked due to expiration.");
        }

        // Cleanup old Audit Events (older than 90 days)
        var ninetyDaysAgo = now.AddDays(-90);
        var oldAudits = await context.AuditEvents
            .Where(a => a.CreatedAt <= ninetyDaysAgo)
            .ToListAsync(stoppingToken);

        if (oldAudits.Any())
        {
            context.AuditEvents.RemoveRange(oldAudits);
            _logger.LogInformation($"Removed {oldAudits.Count} old audit events.");
        }

        // Cleanup soft-deleted notes older than 24 hours
        var twentyFourHoursAgo = now.AddHours(-24);
        var notesToHardDelete = await context.Notes
            .Where(n => n.IsDeleted && n.DeletedAt != null && n.DeletedAt <= twentyFourHoursAgo)
            .ToListAsync(stoppingToken);

        if (notesToHardDelete.Any())
        {
            context.Notes.RemoveRange(notesToHardDelete);
            _logger.LogInformation($"Hard deleted {notesToHardDelete.Count} notes that were in the trash for over 24 hours.");
        }

        if (expiredFiles.Any() || expiredFileShares.Any() || expiredDocShares.Any() || oldAudits.Any() || notesToHardDelete.Any())
        {
            await context.SaveChangesAsync(stoppingToken);
        }
    }
}


