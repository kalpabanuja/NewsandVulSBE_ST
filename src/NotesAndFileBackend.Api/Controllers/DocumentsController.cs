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
            .Select(d => new { d.Id, d.Title, d.Description, d.Revision, d.CreatedAt, d.UpdatedAt })
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
}
