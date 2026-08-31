using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotesAndFileBackend.Application.Interfaces;
using NotesAndFileBackend.Domain.Entities;
using NotesAndFileBackend.Infrastructure.Data;

namespace NotesAndFileBackend.Api.Controllers;

[ApiController]
[Route("api/v1/note-attachments")]
[Authorize]
public class NoteAttachmentsController : ControllerBase
{
    // 10 MiB for downloadable files
    private const long MaxDownloadableBytes = 10L * 1024 * 1024;

    // 50 MiB default for display attachments; configurable via AppConfig:MaxDisplayAttachmentBytes
    private readonly long _maxDisplayBytes;

    private static readonly HashSet<string> AllowedDisplayMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml",
        "video/mp4", "video/webm", "video/ogg"
    };

    private readonly AppDbContext _context;
    private readonly IStorageService _storageService;

    public NoteAttachmentsController(AppDbContext context, IStorageService storageService, IConfiguration config)
    {
        _context = context;
        _storageService = storageService;
        _maxDisplayBytes = config.GetValue<long>("AppConfig:MaxDisplayAttachmentBytes", 50L * 1024 * 1024);
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    /// <summary>
    /// Upload a note attachment. Pass noteId and attachmentType (Display | Downloadable) as query params.
    /// </summary>
    [HttpPost]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = 53_687_091_200)]
    public async Task<IActionResult> UploadAttachment(
        [FromQuery] Guid noteId,
        [FromQuery] string attachmentType,
        [FromQuery] string? displayName,
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = new { code = "INVALID_FILE", message = "File is empty." } });

        var userId = GetCurrentUserId();

        // Verify note ownership
        var note = await _context.Notes.FirstOrDefaultAsync(
            n => n.Id == noteId && n.UserId == userId && !n.IsDeleted, ct);
        if (note == null) return NotFound(new { error = new { message = "Note not found or access denied." } });

        // Validate attachment type
        var normalizedType = attachmentType?.Trim();
        if (normalizedType != "Display" && normalizedType != "Downloadable")
            return BadRequest(new { error = new { code = "INVALID_TYPE", message = "attachmentType must be Display or Downloadable." } });

        // Validate size limits
        if (normalizedType == "Downloadable" && file.Length > MaxDownloadableBytes)
            return StatusCode(413, new { error = new { code = "FILE_TOO_LARGE", message = $"Downloadable attachments cannot exceed {MaxDownloadableBytes / 1024 / 1024} MiB." } });

        if (normalizedType == "Display" && file.Length > _maxDisplayBytes)
            return StatusCode(413, new { error = new { code = "FILE_TOO_LARGE", message = $"Display attachments cannot exceed {_maxDisplayBytes / 1024 / 1024} MiB." } });

        // Validate MIME for display attachments
        if (normalizedType == "Display")
        {
            var detectedMime = file.ContentType?.ToLowerInvariant() ?? string.Empty;
            if (!AllowedDisplayMimeTypes.Contains(detectedMime))
                return StatusCode(415, new { error = new { code = "UNSUPPORTED_MEDIA_TYPE", message = $"MIME type '{detectedMime}' is not allowed for display attachments." } });
        }

        // Compute checksum
        string checksum;
        byte[] fileBytes;
        using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms, ct);
            fileBytes = ms.ToArray();
        }
        checksum = Convert.ToHexString(SHA256.HashData(fileBytes));

        // Upload to storage
        string objectKey;
        try
        {
            using var uploadStream = new MemoryStream(fileBytes);
            objectKey = await _storageService.UploadFileAsync(uploadStream, file.ContentType ?? "application/octet-stream", file.FileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { message = "Storage upload failed.", details = ex.Message } });
        }

        // Sanitize filename — strip directory components
        var safeFilename = Path.GetFileName(file.FileName);

        var attachment = new NoteAttachment
        {
            NoteId = noteId,
            OwnerUserId = userId,
            AttachmentType = normalizedType,
            DisplayName = !string.IsNullOrWhiteSpace(displayName) ? displayName.Trim() : safeFilename,
            ObjectKey = objectKey,
            Filename = safeFilename,
            MimeType = file.ContentType ?? "application/octet-stream",
            ByteSize = file.Length,
            Checksum = checksum
        };

        _context.NoteAttachments.Add(attachment);
        _context.AuditEvents.Add(new AuditEvent
        {
            UserId = userId,
            EventType = "attachment.uploaded",
            ResourceType = "note_attachment",
            ResourceId = attachment.Id.ToString()
        });
        await _context.SaveChangesAsync(ct);

        return Ok(new
        {
            attachment.Id,
            attachment.NoteId,
            attachment.AttachmentType,
            attachment.DisplayName,
            attachment.Filename,
            attachment.MimeType,
            attachment.ByteSize,
            attachment.Checksum,
            attachment.CreatedAt
        });
    }

    /// <summary>
    /// Stream a display attachment for rendering. No download headers.
    /// Only the note owner can access this endpoint.
    /// </summary>
    [HttpGet("{id}/preview")]
    public async Task<IActionResult> PreviewAttachment(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var attachment = await _context.NoteAttachments
            .FirstOrDefaultAsync(a => a.Id == id && a.OwnerUserId == userId, ct);

        if (attachment == null) return NotFound();
        if (attachment.AttachmentType != "Display")
            return BadRequest(new { error = new { code = "NOT_DISPLAY_ATTACHMENT", message = "This attachment is not a display attachment." } });

        var stream = await _storageService.DownloadFileAsync(attachment.ObjectKey);
        // No Content-Disposition: attachment header — this is preview/inline only
        return File(stream, attachment.MimeType);
    }

    /// <summary>
    /// Download a downloadable attachment with a safe Content-Disposition header.
    /// Only the note owner can access this endpoint (unauthenticated access goes through PublicController).
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadAttachment(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var attachment = await _context.NoteAttachments
            .FirstOrDefaultAsync(a => a.Id == id && a.OwnerUserId == userId, ct);

        if (attachment == null) return NotFound();
        if (attachment.AttachmentType != "Downloadable")
            return BadRequest(new { error = new { code = "NOT_DOWNLOADABLE_ATTACHMENT", message = "Use /preview for display attachments." } });

        var stream = await _storageService.DownloadFileAsync(attachment.ObjectKey);
        // Safe filename — never expose the storage key
        var safeFilename = Uri.EscapeDataString(attachment.DisplayName ?? attachment.Filename);
        Response.Headers.Append("Content-Disposition", $"attachment; filename*=UTF-8''{safeFilename}");
        return File(stream, attachment.MimeType);
    }

    /// <summary>
    /// Delete a note attachment. Owner only.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAttachment(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var attachment = await _context.NoteAttachments
            .FirstOrDefaultAsync(a => a.Id == id && a.OwnerUserId == userId, ct);

        if (attachment == null) return NotFound();

        // Remove from storage
        try
        {
            await _storageService.DeleteFileAsync(attachment.ObjectKey);
        }
        catch
        {
            // Log but don't block the DB delete
        }

        _context.NoteAttachments.Remove(attachment);
        _context.AuditEvents.Add(new AuditEvent
        {
            UserId = userId,
            EventType = "attachment.deleted",
            ResourceType = "note_attachment",
            ResourceId = attachment.Id.ToString()
        });
        await _context.SaveChangesAsync(ct);

        return NoContent();
    }
}
