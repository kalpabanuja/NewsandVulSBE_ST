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

        // Stream the file directly through the API
        var stream = await _storageService.DownloadFileAsync(share.File.StoredFilename);
        
        return File(stream, share.File.MimeType, share.File.OriginalFilename);
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
