using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotesAndFileBackend.Api.DTOs;
using NotesAndFileBackend.Core.Entities;
using NotesAndFileBackend.Infrastructure.Data;

namespace NotesAndFileBackend.Api.Controllers;

[ApiController]
[Route("api/v1/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public DocumentsController(AppDbContext context)
    {
        _context = context;
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    private Guid GetCurrentDeviceId()
    {
        var claim = User.FindFirst("deviceId");
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    [HttpPost]
    public async Task<IActionResult> CreateDocument([FromBody] CreateDocumentRequest request)
    {
        var userId = GetCurrentUserId();
        var deviceId = GetCurrentDeviceId();

        var document = new Document
        {
            OwnerUserId = userId,
            OwnerDeviceId = deviceId,
            Title = request.Title,
            Description = request.Description,
            Slug = request.Title.ToLower().Replace(" ", "-") // Basic slugification
        };

        _context.Documents.Add(document);
        
        // Audit Log
        _context.AuditEvents.Add(new AuditEvent { UserId = userId, DeviceId = deviceId, EventType = "document.created", ResourceType = "document", ResourceId = document.Id.ToString() });

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, document);
    }

    [HttpGet]
    public async Task<IActionResult> ListDocuments()
    {
        var userId = GetCurrentUserId();
        var documents = await _context.Documents
            .Where(d => d.OwnerUserId == userId && d.Status == "ACTIVE")
            .Select(d => new { 
                d.Id, 
                d.Title, 
                d.Description, 
                d.Revision, 
                d.CreatedAt, 
                d.UpdatedAt,
                PublicShares = d.PublicShares.Where(s => s.RevokedAt == null).ToList()
            })
            .ToListAsync();
            
        return Ok(documents);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDocument(Guid id)
    {
        var userId = GetCurrentUserId();
        var document = await _context.Documents
            .Include(d => d.Blocks.OrderBy(b => b.Position))
            .Include(d => d.Attachments)
            .Include(d => d.PublicShares.Where(s => s.RevokedAt == null))
            .FirstOrDefaultAsync(d => d.Id == id && d.OwnerUserId == userId && d.Status == "ACTIVE");
            
        if (document == null) return NotFound();
        
        return Ok(document);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDocument(Guid id, [FromBody] UpdateDocumentRequest request)
    {
        var userId = GetCurrentUserId();
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id && d.OwnerUserId == userId && d.Status == "ACTIVE");
        
        if (document == null) return NotFound();

        // Optimistic Concurrency check
        if (document.Revision != request.Revision)
        {
            return Conflict(new { error = new { code = "CONFLICT", message = "Document has been modified by another client." } });
        }

        document.Title = request.Title;
        document.Description = request.Description;
        document.Revision++;
        document.UpdatedAt = DateTime.UtcNow;

        _context.Documents.Update(document);
        
        // Audit log
        _context.AuditEvents.Add(new AuditEvent { UserId = userId, EventType = "document.updated", ResourceType = "document", ResourceId = document.Id.ToString() });

        await _context.SaveChangesAsync();
        
        return Ok(document);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        var userId = GetCurrentUserId();
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id && d.OwnerUserId == userId && d.Status == "ACTIVE");
        
        if (document == null) return NotFound();

        document.Status = "DELETED";
        document.DeletedAt = DateTime.UtcNow;
        
        // Audit log
        _context.AuditEvents.Add(new AuditEvent { UserId = userId, EventType = "document.deleted", ResourceType = "document", ResourceId = document.Id.ToString() });

        await _context.SaveChangesAsync();
        
        return NoContent();
    }

    [HttpPost("{id}/share")]
    public async Task<IActionResult> ShareDocument(Guid id, [FromBody] NotesAndFileBackend.Api.DTOs.CreateShareRequest request)
    {
        var userId = GetCurrentUserId();
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id && d.OwnerUserId == userId && d.Status == "ACTIVE");
        
        if (document == null) return NotFound();

        var token = NotesAndFileBackend.Api.Helpers.TokenHelper.GenerateToken(request.Alias);
        
        var share = new PublicDocumentShare
        {
            DocumentId = document.Id,
            TokenHash = token,
            CreatedByUserId = userId,
            ExpiresAt = request.ExpiresInHours.HasValue ? DateTime.UtcNow.AddHours(request.ExpiresInHours.Value) : null
        };

        _context.PublicDocumentShares.Add(share);
        
        // Audit log
        _context.AuditEvents.Add(new AuditEvent { UserId = userId, EventType = "document.shared", ResourceType = "document", ResourceId = document.Id.ToString() });

        await _context.SaveChangesAsync();

        var publicUrl = $"{Request.Scheme}://{Request.Host}/api/v1/public/documents/{token}";

        return Ok(new NotesAndFileBackend.Api.DTOs.ShareResponseDto
        {
            Id = share.Id,
            Token = token,
            PublicUrl = publicUrl,
            ExpiresAt = share.ExpiresAt
        });
    }

    [HttpDelete("{id}/share/{shareId}")]
    public async Task<IActionResult> RevokeDocumentShare(Guid id, Guid shareId)
    {
        var userId = GetCurrentUserId();
        var share = await _context.PublicDocumentShares.FirstOrDefaultAsync(s => s.Id == shareId && s.DocumentId == id && s.CreatedByUserId == userId && s.RevokedAt == null);
        
        if (share == null) return NotFound();

        share.RevokedAt = DateTime.UtcNow;
        
        // Audit log
        _context.AuditEvents.Add(new AuditEvent { UserId = userId, EventType = "document.share_revoked", ResourceType = "document", ResourceId = id.ToString() });

        await _context.SaveChangesAsync();
        
        return NoContent();
    }
}
