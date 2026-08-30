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
    public async Task<IActionResult> GetSharedNote(string token, [FromHeader(Name = "X-Share-Password")] string? headerPassword, [FromQuery(Name = "pwd")] string? queryPassword, CancellationToken ct)
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

        var password = headerPassword ?? queryPassword;
        if (!string.IsNullOrWhiteSpace(share.PasswordHash))
        {
            if (string.IsNullOrWhiteSpace(password) || !BCrypt.Net.BCrypt.Verify(password, share.PasswordHash))
            {
                // If requested by a web browser, show a beautiful HTML password form
                if (Request.Headers["Accept"].ToString().Contains("text/html"))
                {
                    var promptHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Password Protected Note</title>
    <style>
        body {{ font-family: system-ui, -apple-system, sans-serif; max-width: 400px; margin: 100px auto; text-align: center; color: #333; background: #f9fafb; }}
        .card {{ background: white; padding: 2rem; border-radius: 12px; box-shadow: 0 4px 6px rgba(0,0,0,0.05); border: 1px solid #e5e7eb; }}
        input {{ padding: 12px; font-size: 1rem; width: 100%; box-sizing: border-box; margin: 15px 0; border: 1px solid #d1d5db; border-radius: 6px; outline: none; }}
        input:focus {{ border-color: #0284c7; box-shadow: 0 0 0 3px rgba(2, 132, 199, 0.1); }}
        button {{ padding: 12px; font-size: 1rem; background: #0ea5e9; color: white; border: none; border-radius: 6px; width: 100%; cursor: pointer; font-weight: 600; transition: background 0.2s; }}
        button:hover {{ background: #0284c7; }}
    </style>
</head>
<body>
    <div class='card'>
        <svg style='width:48px;height:48px;color:#0ea5e9;margin-bottom:10px;' fill='none' stroke='currentColor' viewBox='0 0 24 24'><path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z'></path></svg>
        <h2 style='margin-top:0;'>Protected Note</h2>
        <p style='color:#666;font-size:0.9rem;'>This note is secured. Please enter the password to unlock it.</p>
        <form method='GET'>
            <input type='password' name='pwd' placeholder='Enter password' required autofocus />
            <button type='submit'>Unlock Note</button>
        </form>
    </div>
</body>
</html>";
                    return Content(promptHtml, "text/html", System.Text.Encoding.UTF8);
                }
                // Otherwise, return standard API JSON Error
                return Unauthorized(new { error = new { message = "Invalid or missing password." } });
            }
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

        // If a web browser opens the link, return a styled HTML page
        if (Request.Headers["Accept"].ToString().Contains("text/html"))
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>{System.Net.WebUtility.HtmlEncode(share.Note.Title)}</title>
    <style>
        body {{ font-family: system-ui, -apple-system, sans-serif; line-height: 1.6; max-width: 800px; margin: 0 auto; padding: 2rem; color: #333; background: #fff; }}
        h1 {{ border-bottom: 2px solid #eaeaea; padding-bottom: 0.5rem; color: #111; }}
        .summary {{ font-size: 1.2rem; color: #666; margin-bottom: 2rem; font-style: italic; }}
        .content {{ background: #f9fafb; padding: 1.5rem; border-radius: 8px; white-space: pre-wrap; word-wrap: break-word; font-family: monospace; font-size: 0.95rem; border: 1px solid #e5e7eb; }}
        .meta {{ margin-top: 3rem; font-size: 0.9rem; color: #888; border-top: 1px solid #eaeaea; padding-top: 1rem; }}
    </style>
</head>
<body>
    <h1>{System.Net.WebUtility.HtmlEncode(share.Note.Title)}</h1>
    <div class='summary'>{System.Net.WebUtility.HtmlEncode(share.Note.Summary)}</div>
    <div class='content'>{System.Net.WebUtility.HtmlEncode(contentJsonb)}</div>
    <div class='meta'>Shared securely via ThreatIntel &bull; Last updated: {share.Note.UpdatedAt:yyyy-MM-dd HH:mm}</div>
</body>
</html>";
            return Content(html, "text/html", System.Text.Encoding.UTF8);
        }

        // Otherwise, return standard JSON (for APIs)
        return Ok(dto);
    }
}



