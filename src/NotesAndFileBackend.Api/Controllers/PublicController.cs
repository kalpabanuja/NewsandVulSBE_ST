using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using NotesAndFileBackend.Application.Interfaces;
using NotesAndFileBackend.Infrastructure.Data;

namespace NotesAndFileBackend.Api.Controllers;

[ApiController]
[Route("api/v1/public")]
[AllowAnonymous]
[EnableRateLimiting("StrictPolicy")]
public class PublicController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IStorageService _storageService;

    public PublicController(AppDbContext context, IStorageService storageService)
    {
        _context = context;
        _storageService = storageService;
    }

    [HttpGet("files/{token}")]
    public async Task<IActionResult> GetSharedFile(string token, CancellationToken ct)
    {
        var share = await _context.PublicFileShares
            .Include(s => s.File)
            .FirstOrDefaultAsync(s => s.TokenHash == token && s.RevokedAt == null, ct);

        if (share == null || share.File.Status != "ACTIVE")
            return NotFound(new { error = new { message = "Share link is invalid or expired." } });

        if (share.ExpiresAt.HasValue && share.ExpiresAt.Value < DateTime.UtcNow)
            return NotFound(new { error = new { message = "Share link has expired." } });

        // Increment stats
        share.AccessCount++;
        share.LastAccessedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        // Stream the file directly through the API
        var stream = await _storageService.DownloadFileAsync(share.File.StoredFilename);
        
        return File(stream, share.File.MimeType, share.File.OriginalFilename);
    }

    [HttpGet("Notes/{token}")]
    public async Task<IActionResult> GetSharedNote(string token, [FromHeader(Name = "X-Share-Password")] string? password, CancellationToken ct)
    {
        var share = await _context.PublicNoteShares
            .Include(s => s.Note)
            .FirstOrDefaultAsync(s => s.TokenHash == token && s.RevokedAt == null, ct);

        if (share == null || share.Note.IsDeleted)
            return NotFound(new { error = new { message = "Share link is invalid or expired." } });

        if (share.ExpiresAt.HasValue && share.ExpiresAt.Value < DateTime.UtcNow)
            return NotFound(new { error = new { message = "Share link has expired." } });

        if (share.MaxViews.HasValue && share.ViewCount >= share.MaxViews.Value)
            return NotFound(new { error = new { message = "Share link has reached its maximum view limit." } });

        if (!string.IsNullOrWhiteSpace(share.PasswordHash))
        {
            if (string.IsNullOrWhiteSpace(password))
                return Unauthorized(new { error = new { message = "This share is password protected. Provide X-Share-Password header." } });
            
            if (!BCrypt.Net.BCrypt.Verify(password, share.PasswordHash))
                return Unauthorized(new { error = new { message = "Invalid password." } });
        }

        // Update stats
        share.ViewCount++;
        share.LastAccessedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        var contentJsonb = share.Note.ContentJsonb;
        System.Text.Json.JsonElement parsedContent;
        try
        {
            parsedContent = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(contentJsonb);
        }
        catch
        {
            parsedContent = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>("{}");
        }

        var dto = new NotesAndFileBackend.Api.DTOs.SharedNoteDto
        {
            Id = share.Note.Id,
            Title = share.Note.Title,
            Summary = share.Note.Summary,
            ToolName = share.Note.ToolName ?? string.Empty,
            ContentJsonb = parsedContent,
            UpdatedAt = share.Note.UpdatedAt
        };

        if (!share.AllowIndexing)
        {
            Response.Headers.Append("X-Robots-Tag", "noindex, nofollow");
        }

        return Ok(dto);
    }
}



