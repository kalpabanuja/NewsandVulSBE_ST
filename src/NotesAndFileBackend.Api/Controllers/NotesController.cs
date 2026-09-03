using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotesAndFileBackend.Api.DTOs;
using NotesAndFileBackend.Domain.Entities;
using NotesAndFileBackend.Infrastructure.Data;
using NotesAndFileBackend.Application.Services;
using NotesAndFileBackend.Api.Services;
using NotesAndFileBackend.Api.Filters;
using BCrypt.Net;

namespace NotesAndFileBackend.Api.Controllers;

[ApiController]
[Route("api/v1/notes")]
[Authorize]
public class NotesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IImportExportService _importExportService;

    public NotesController(AppDbContext context, IImportExportService importExportService)
    {
        _context = context;
        _importExportService = importExportService;
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    private Guid? GetCurrentDeviceId()
    {
        var claim = User.FindFirst("deviceId");
        return claim != null && Guid.TryParse(claim.Value, out var id) ? id : null;
    }

    private async Task<List<NoteTag>> ResolveTagsAsync(Guid userId, List<string> tagNames)
    {
        var result = new List<NoteTag>();
        if (tagNames == null || !tagNames.Any()) return result;

        var normalizedNames = tagNames.Select(t => t.ToLowerInvariant().Trim()).Distinct().ToList();

        var existingTags = await _context.Tags
            .Where(t => t.UserId == userId && normalizedNames.Contains(t.Normalized))
            .ToListAsync();

        foreach (var name in normalizedNames)
        {
            var tag = existingTags.FirstOrDefault(t => t.Normalized == name);
            if (tag == null)
            {
                tag = new Tag
                {
                    UserId = userId,
                    Name = tagNames.First(t => t.ToLowerInvariant().Trim() == name),
                    Normalized = name
                };
                _context.Tags.Add(tag);
            }
            result.Add(new NoteTag { Tag = tag });
        }

        return result;
    }

    private string GenerateSlug(string title)
    {
        var slug = title.ToLower().Replace(" ", "-").Trim();
        var suffix = Guid.NewGuid().ToString("N").Substring(0, 4); // basic uniqueness
        return $"{slug}-{suffix}";
    }

    [HttpPost]
    public async Task<IActionResult> CreateNote([FromBody] CreateNoteRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var deviceId = GetCurrentDeviceId();

        // Validate visibility value
        var visibility = request.Visibility?.ToUpperInvariant();
        if (visibility != "PRIVATE" && visibility != "PUBLIC")
            return BadRequest(new { errors = new[] { new { field = "visibility", code = "invalid_visibility", message = "Visibility must be PRIVATE or PUBLIC." } } });

        var contentJson = request.Content.ValueKind != JsonValueKind.Undefined ? request.Content.GetRawText() : "{\"version\": 2, \"blocks\": []}";

        // Validate block content
        var contentErrors = NoteContentValidator.Validate(contentJson);
        if (contentErrors.Count > 0)
            return BadRequest(new { errors = contentErrors.Select(e => new { field = e.Field, code = e.Code, message = e.Message }) });

        var searchText = NoteTextExtractor.ExtractText(contentJson) + " " + request.Title + " " + request.Summary;

        var note = new Note
        {
            UserId = userId,
            DeviceId = deviceId,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            Title = request.Title,
            Summary = request.Summary,
            Slug = GenerateSlug(request.Title),
            CategoryId = request.CategoryId,
            ToolName = request.ToolName,
            ContentJsonb = contentJson,
            SearchText = searchText,
            IsPinned = request.IsPinned,
            IsFavorite = request.IsFavorite,
            Visibility = visibility,
            Version = 1,
            NoteTags = await ResolveTagsAsync(userId, request.Tags)
        };

        _context.Notes.Add(note);
        
        // Audit Log
        _context.AuditEvents.Add(new AuditEvent { UserId = userId, DeviceId = deviceId, EventType = "note.created", ResourceType = "note", ResourceId = note.Id.ToString() });

        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetNote), new { id = note.Id }, new {
            id = note.Id,
            slug = note.Slug,
            version = note.Version,
            createdAt = note.CreatedAt
        });
    }

    [HttpGet]
    public async Task<IActionResult> ListNotes([FromQuery] bool includeArchived = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        pageSize = Math.Min(pageSize, 100);
        page = Math.Max(1, page);
        var userId = GetCurrentUserId();
        
        var query = _context.Notes
            .Include(n => n.NoteTags).ThenInclude(nt => nt.Tag)
            .Include(n => n.Category)
            .Where(d => d.UserId == userId && d.IsDeleted == false);

        if (!includeArchived)
        {
            query = query.Where(d => d.IsArchived == false);
        }

        var notes = await query
            .Select(d => new { 
                d.Id, 
                d.Title, 
                d.Summary,
                d.CategoryId,
                Category = d.Category != null ? d.Category.Name : null,
                Tags = d.NoteTags.Select(nt => nt.Tag.Name).ToList(),
                d.ToolName,
                d.IsFavorite,
                d.IsPinned,
                d.IsArchived,
                d.IsDeleted,
                d.UpdatedAt,
                d.CreatedAt,
                d.DeletedAt,
                d.Visibility
            })
            .OrderByDescending(d => d.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
            
        return Ok(notes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetNote(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var note = await _context.Notes
            .Include(d => d.User)
            .Include(d => d.Attachments)
            .Include(d => d.PublicShares.Where(s => s.RevokedAt == null))
            .Include(d => d.NoteTags).ThenInclude(nt => nt.Tag)
            .Include(d => d.Category)
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId && d.IsDeleted == false, ct);
            
        if (note == null) return NotFound();
        
        return Ok(new {
            note.Id,
            note.Title,
            note.Summary,
            note.Slug,
            note.Visibility,
            note.CategoryId,
            Category = note.Category?.Name,
            Tags = note.NoteTags.Select(nt => nt.Tag.Name).ToList(),
            note.ToolName,
            note.ContentJsonb,
            note.IsPinned,
            note.IsFavorite,
            note.IsArchived,
            note.IsDeleted,
            note.Version,
            note.CreatedAt,
            note.UpdatedAt,
            note.DeletedAt,
            OwnerId = note.UserId,
            OwnerName = note.User?.DisplayName,
            PublicShares = note.PublicShares.Select(ps => new { ps.Id, ps.TokenHash, ps.ExpiresAt })
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateNote(Guid id, [FromBody] UpdateNoteRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        
        var note = await _context.Notes
            .Include(n => n.NoteTags)
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId && d.IsDeleted == false, ct);
        
        if (note == null) return NotFound();

        // Optimistic Concurrency check
        if (note.Version != request.Version)
        {
            return Conflict(new { error = new { code = "CONFLICT", message = "Note has been modified by another client." } });
        }

        // Validate visibility
        var visibility = request.Visibility?.ToUpperInvariant();
        if (visibility != "PRIVATE" && visibility != "PUBLIC")
            return BadRequest(new { errors = new[] { new { field = "visibility", code = "invalid_visibility", message = "Visibility must be PRIVATE or PUBLIC." } } });

        // Validate block content
        var contentJson = request.Content.ValueKind != JsonValueKind.Undefined ? request.Content.GetRawText() : note.ContentJsonb;
        var contentErrors = NoteContentValidator.Validate(contentJson);
        if (contentErrors.Count > 0)
            return BadRequest(new { errors = contentErrors.Select(e => new { field = e.Field, code = e.Code, message = e.Message }) });

        var searchText = NoteTextExtractor.ExtractText(contentJson) + " " + request.Title + " " + request.Summary;

        // Save Revision before updating
        var revision = new NoteRevision
        {
            NoteId = note.Id,
            Version = note.Version,
            Title = note.Title,
            Summary = note.Summary,
            ContentJsonb = note.ContentJsonb,
            EditedByUserId = userId
        };
        _context.NoteRevisions.Add(revision);

        note.Title = request.Title;
        note.Summary = request.Summary;
        note.CategoryId = request.CategoryId;
        note.ToolName = request.ToolName;
        note.ContentJsonb = contentJson;
        note.SearchText = searchText;
        note.IsPinned = request.IsPinned;
        note.IsFavorite = request.IsFavorite;
        note.IsArchived = request.IsArchived;
        
        // Visibility change — emit audit event if changed
        if (note.Visibility != visibility)
        {
            note.Visibility = visibility;
            _context.AuditEvents.Add(new AuditEvent { UserId = userId, EventType = "note.visibility_changed", ResourceType = "note", ResourceId = note.Id.ToString() });
        }
        
        note.Version++;
        note.UpdatedAt = DateTime.UtcNow;
        note.UpdatedByUserId = userId;

        // Update Tags
        _context.NoteTags.RemoveRange(note.NoteTags);
        note.NoteTags = await ResolveTagsAsync(userId, request.Tags);

        _context.Notes.Update(note);
        
        // Audit log
        _context.AuditEvents.Add(new AuditEvent { UserId = userId, EventType = "note.updated", ResourceType = "note", ResourceId = note.Id.ToString() });

        await _context.SaveChangesAsync(ct);
        
        return Ok(new { version = note.Version, updatedAt = note.UpdatedAt });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNote(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var note = await _context.Notes
            .Include(n => n.PublicShares)
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId && d.IsDeleted == false, ct);
        
        if (note == null) return NotFound();

        // Soft Delete
        note.IsDeleted = true;
        note.DeletedAt = DateTime.UtcNow;

        // Revoke Shares
        foreach (var share in note.PublicShares.Where(s => s.RevokedAt == null))
        {
            share.RevokedAt = DateTime.UtcNow;
        }
        
        // Audit log
        _context.AuditEvents.Add(new AuditEvent { UserId = userId, EventType = "note.deleted", ResourceType = "note", ResourceId = note.Id.ToString() });

        await _context.SaveChangesAsync(ct);
        
        return NoContent();
    }

    [HttpGet("deleted")]
    public async Task<IActionResult> ListDeletedNotes([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        pageSize = Math.Min(pageSize, 100);
        page = Math.Max(1, page);
        var userId = GetCurrentUserId();
        
        var query = _context.Notes
            .Include(n => n.NoteTags).ThenInclude(nt => nt.Tag)
            .Include(n => n.Category)
            .Where(d => d.UserId == userId && d.IsDeleted == true);

        var notes = await query
            .Select(d => new { 
                d.Id, 
                d.Title, 
                d.Summary,
                d.CategoryId,
                Category = d.Category != null ? d.Category.Name : null,
                Tags = d.NoteTags.Select(nt => nt.Tag.Name).ToList(),
                d.ToolName,
                d.IsFavorite,
                d.IsPinned,
                d.IsArchived,
                d.IsDeleted,
                d.UpdatedAt,
                d.CreatedAt,
                d.DeletedAt,
                Visibility = d.Visibility
            })
            .OrderByDescending(d => d.DeletedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
            
        return Ok(notes);
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> RestoreNote(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var note = await _context.Notes
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId && d.IsDeleted == true, ct);
        
        if (note == null) return NotFound();

        // Restore
        note.IsDeleted = false;
        note.DeletedAt = null;
        note.UpdatedAt = DateTime.UtcNow;
        note.UpdatedByUserId = userId;

        // Audit log
        _context.AuditEvents.Add(new AuditEvent { UserId = userId, EventType = "note.restored", ResourceType = "note", ResourceId = note.Id.ToString() });

        await _context.SaveChangesAsync(ct);
        
        return Ok(new { id = note.Id, title = note.Title });
    }

    [HttpPost("{id}/duplicate")]
    public async Task<IActionResult> DuplicateNote(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var deviceId = GetCurrentDeviceId();

        var originalNote = await _context.Notes
            .Include(n => n.NoteTags).ThenInclude(nt => nt.Tag)
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId && d.IsDeleted == false, ct);
            
        if (originalNote == null) return NotFound();

        var newNote = new Note
        {
            UserId = userId,
            DeviceId = deviceId,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            Title = originalNote.Title + " (Copy)",
            Summary = originalNote.Summary,
            Slug = GenerateSlug(originalNote.Title + " Copy"),
            CategoryId = originalNote.CategoryId,
            ToolName = originalNote.ToolName,
            ContentJsonb = originalNote.ContentJsonb,
            SearchText = originalNote.SearchText,
            Version = 1,
            IsPinned = false,
            IsFavorite = false,
            IsArchived = false
        };

        // Copy tags
        foreach (var nt in originalNote.NoteTags)
        {
            newNote.NoteTags.Add(new NoteTag { TagId = nt.TagId });
        }

        _context.Notes.Add(newNote);
        _context.AuditEvents.Add(new AuditEvent { UserId = userId, EventType = "note.duplicated", ResourceType = "note", ResourceId = newNote.Id.ToString() });

        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetNote), new { id = newNote.Id }, new {
            id = newNote.Id,
            slug = newNote.Slug,
            version = newNote.Version
        });
    }

    [HttpGet("{id}/revisions")]
    public async Task<IActionResult> ListNoteRevisions(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();

        // Ensure note exists and belongs to user
        var noteExists = await _context.Notes.AnyAsync(n => n.Id == id && n.UserId == userId && n.IsDeleted == false, ct);
        if (!noteExists) return NotFound();

        var revisions = await _context.NoteRevisions
            .Where(r => r.NoteId == id)
            .OrderByDescending(r => r.Version)
            .Select(r => new {
                r.Id,
                r.Version,
                r.Title,
                r.Summary,
                r.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(revisions);
    }

    [HttpGet("{id}/revisions/{version}")]
    public async Task<IActionResult> GetNoteRevision(Guid id, int version, CancellationToken ct)
    {
        var userId = GetCurrentUserId();

        var noteExists = await _context.Notes.AnyAsync(n => n.Id == id && n.UserId == userId && n.IsDeleted == false, ct);
        if (!noteExists) return NotFound();

        var revision = await _context.NoteRevisions
            .FirstOrDefaultAsync(r => r.NoteId == id && r.Version == version, ct);

        if (revision == null) return NotFound();

        return Ok(new {
            revision.Id,
            revision.Version,
            revision.Title,
            revision.Summary,
            revision.ContentJsonb,
            revision.CreatedAt
        });
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchNotes([FromQuery] string q, [FromQuery] Guid? categoryId, [FromQuery] string? tag, [FromQuery] string? tool, [FromQuery] bool? favorite, [FromQuery] bool? pinned, [FromQuery] bool? archived, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        pageSize = Math.Min(pageSize, 100);
        page = Math.Max(1, page);
        var userId = GetCurrentUserId();
        
        var query = _context.Notes
            .Include(n => n.Category)
            .Include(n => n.NoteTags).ThenInclude(nt => nt.Tag)
            .Where(n => n.UserId == userId && n.IsDeleted == false);

        if (!string.IsNullOrWhiteSpace(q))
        {
            // PostgreSQL Full Text Search
            query = query.Where(n => EF.Functions.ToTsVector("simple", n.SearchText).Matches(EF.Functions.PlainToTsQuery("simple", q)));
        }

        if (categoryId.HasValue) query = query.Where(n => n.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(tag)) query = query.Where(n => n.NoteTags.Any(nt => nt.Tag.Normalized == tag.ToLower()));
        if (!string.IsNullOrWhiteSpace(tool)) query = query.Where(n => n.ToolName == tool);
        if (favorite.HasValue) query = query.Where(n => n.IsFavorite == favorite.Value);
        if (pinned.HasValue) query = query.Where(n => n.IsPinned == pinned.Value);
        
        if (archived.HasValue && archived.Value)
        {
            query = query.Where(n => n.IsArchived == true);
        }
        else
        {
            query = query.Where(n => n.IsArchived == false); // Default exclude archived
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(n => n.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NoteSearchItem
            {
                Id = n.Id,
                Title = n.Title,
                Summary = n.Summary,
                CategoryId = n.CategoryId,
                Category = n.Category != null ? n.Category.Name : null,
                Tags = n.NoteTags.Select(nt => nt.Tag.Name).ToList(),
                ToolName = n.ToolName,
                IsFavorite = n.IsFavorite,
                IsPinned = n.IsPinned,
                IsArchived = n.IsArchived,
                IsDeleted = n.IsDeleted,
                UpdatedAt = n.UpdatedAt,
                CreatedAt = n.CreatedAt,
                DeletedAt = n.DeletedAt,
                Visibility = n.Visibility
            })
            .ToListAsync(ct);

        return Ok(new NoteSearchResponse
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        });
    }

    [HttpGet("{id}/search")]
    public async Task<IActionResult> InsideNoteSearch(Guid id, [FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q)) return BadRequest("Query 'q' is required.");

        var userId = GetCurrentUserId();
        var note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId && n.IsDeleted == false, ct);
        
        if (note == null) return NotFound();

        var matches = new List<NoteBlockMatch>();
        
        try
        {
            using var doc = JsonDocument.Parse(note.ContentJsonb);
            if (doc.RootElement.TryGetProperty("blocks", out var blocks) && blocks.ValueKind == JsonValueKind.Array)
            {
                foreach (var block in blocks.EnumerateArray())
                {
                    var blockText = NoteTextExtractor.ExtractText(block.GetRawText());
                    if (blockText.Contains(q, StringComparison.OrdinalIgnoreCase))
                    {
                        var blockId = block.TryGetProperty("id", out var idProp) ? idProp.GetString() : string.Empty;
                        var blockType = block.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : string.Empty;
                        
                        // Extract a crude snippet around the match
                        var idx = blockText.IndexOf(q, StringComparison.OrdinalIgnoreCase);
                        var start = Math.Max(0, idx - 20);
                        var length = Math.Min(blockText.Length - start, q.Length + 40);
                        var snippet = blockText.Substring(start, length);

                        matches.Add(new NoteBlockMatch
                        {
                            BlockId = blockId ?? string.Empty,
                            BlockType = blockType ?? string.Empty,
                            Snippet = snippet
                        });
                    }
                }
            }
        }
        catch (JsonException) { }

        return Ok(matches);
    }

    [HttpPost("{id}/share")]
    [ServiceFilter(typeof(IdempotencyFilterAttribute))]
    public async Task<IActionResult> ShareNote(Guid id, [FromBody] CreateShareRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var note = await _context.Notes.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId && d.IsDeleted == false, ct);
        
        if (note == null) return NotFound();

        var token = string.IsNullOrWhiteSpace(request.Alias) 
            ? NotesAndFileBackend.Api.Helpers.TokenHelper.GenerateToken(null)
            : NotesAndFileBackend.Api.Helpers.TokenHelper.GenerateToken(request.Alias);
        
        var share = new PublicNoteShare
        {
            NoteId = note.Id,
            TokenHash = token,
            CreatedByUserId = userId,
            ExpiresAt = request.ExpiresInHours.HasValue ? DateTime.UtcNow.AddHours(request.ExpiresInHours.Value) : null,
            PasswordHash = !string.IsNullOrWhiteSpace(request.Password) ? BCrypt.Net.BCrypt.HashPassword(request.Password) : null,
            AllowIndexing = request.AllowIndexing,
            MaxViews = request.MaxViews
        };

        _context.PublicNoteShares.Add(share);
        
        // Audit log
        _context.AuditEvents.Add(new AuditEvent { UserId = userId, EventType = "note.shared", ResourceType = "note", ResourceId = note.Id.ToString() });

        await _context.SaveChangesAsync(ct);

        var publicUrl = $"{Request.Scheme}://{Request.Host}/s/{token}";

        return Ok(new ShareResponseDto
        {
            Id = share.Id,
            Token = token,
            PublicUrl = publicUrl,
            ExpiresAt = share.ExpiresAt
        });
    }

    [HttpDelete("{id}/share/{shareId}")]
    public async Task<IActionResult> RevokeNoteShare(Guid id, Guid shareId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var share = await _context.PublicNoteShares.FirstOrDefaultAsync(s => s.Id == shareId && s.NoteId == id && s.CreatedByUserId == userId && s.RevokedAt == null, ct);
        
        if (share == null) return NotFound();

        share.RevokedAt = DateTime.UtcNow;
        
        // Audit log
        _context.AuditEvents.Add(new AuditEvent { UserId = userId, EventType = "note.share_revoked", ResourceType = "note", ResourceId = id.ToString() });

        await _context.SaveChangesAsync(ct);
        
        return NoContent();
    }

    [HttpPost("export")]
    [ServiceFilter(typeof(IdempotencyFilterAttribute))]
    public async Task<IActionResult> ExportNotes([FromBody] ExportRequestDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _importExportService.ExportNotesAsync(userId, request);
        return Ok(result);
    }

    [HttpPost("import")]
    [ServiceFilter(typeof(IdempotencyFilterAttribute))]
    public async Task<IActionResult> ImportNotes([FromBody] ImportRequestDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _importExportService.ImportNotesAsync(userId, request);
        return Ok(result);
    }
}
