using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotesAndFileBackend.Core.Interfaces;
using NotesAndFileBackend.Infrastructure.Data;

namespace NotesAndFileBackend.Api.Controllers;

[ApiController]
[Route("api/v1/public")]
[AllowAnonymous]
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
    public async Task<IActionResult> GetSharedFile(string token)
    {
        var share = await _context.PublicFileShares
            .Include(s => s.File)
            .FirstOrDefaultAsync(s => s.TokenHash == token && s.RevokedAt == null);

        if (share == null || share.File.Status != "ACTIVE")
            return NotFound(new { error = new { message = "Share link is invalid or expired." } });

        if (share.ExpiresAt.HasValue && share.ExpiresAt.Value < DateTime.UtcNow)
            return NotFound(new { error = new { message = "Share link has expired." } });

        // Increment stats
        share.AccessCount++;
        share.LastAccessedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Give a short-lived download URL (e.g. 15 minutes) for the actual file
        var url = await _storageService.GeneratePresignedDownloadUrlAsync(share.File.StoredFilename, TimeSpan.FromMinutes(15));
        
        return Redirect(url); // Redirect directly to the file!
    }

    [HttpGet("documents/{token}")]
    public async Task<IActionResult> GetSharedDocument(string token)
    {
        var share = await _context.PublicDocumentShares
            .Include(s => s.Document)
            .ThenInclude(d => d.Blocks.OrderBy(b => b.Position))
            .Include(s => s.Document)
            .ThenInclude(d => d.Attachments)
            .FirstOrDefaultAsync(s => s.TokenHash == token && s.RevokedAt == null);

        if (share == null || share.Document.Status != "ACTIVE")
            return NotFound(new { error = new { message = "Share link is invalid or expired." } });

        if (share.ExpiresAt.HasValue && share.ExpiresAt.Value < DateTime.UtcNow)
            return NotFound(new { error = new { message = "Share link has expired." } });

        // Update stats
        share.LastAccessedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(share.Document); // Return the full read-only document
    }
}
