using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotesAndFileBackend.Domain.Entities;
using NotesAndFileBackend.Application.Interfaces;
using NotesAndFileBackend.Infrastructure.Data;

namespace NotesAndFileBackend.Api.Controllers;

[ApiController]
[Route("api/v1/files")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IStorageService _storageService;

    // Quota limits
    private const long MAX_TOTAL_BYTES = 30L * 1024 * 1024 * 1024; // 30 GB
    private const int MAX_FILE_COUNT = 500;

    public FilesController(AppDbContext context, IStorageService storageService)
    {
        _context = context;
        _storageService = storageService;
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    private Guid GetCurrentDeviceId()
    {
        var claim = User.FindFirst("DeviceId");
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    [HttpPost("upload")]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = 21474836480)] // 20 GB
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = new { code = "INVALID_FILE", message = "File is empty or not provided." } });
        }

        var UserId = GetCurrentUserId();
        var DeviceId = GetCurrentDeviceId();

        // Check Quotas
        var userFiles = await _context.Files
            .Where(f => f.OwnerUserId == UserId && f.Status != "DELETED")
            .ToListAsync();

        if (userFiles.Count >= MAX_FILE_COUNT)
        {
            return StatusCode(403, new { error = new { code = "FILE_COUNT_LIMIT_REACHED", message = "Maximum number of stored files reached." } });
        }

        long usedBytes = userFiles.Sum(f => f.ByteSize);
        if (usedBytes + file.Length > MAX_TOTAL_BYTES)
        {
            return StatusCode(403, new { error = new { code = "STORAGE_QUOTA_EXCEEDED", message = "The upload would exceed the available storage." } });
        }

        var storedFile = new StoredFile
        {
            OwnerUserId = UserId,
            OwnerDeviceId = DeviceId,
            OriginalFilename = file.FileName,
            MimeType = file.ContentType,
            Extension = Path.GetExtension(file.FileName),
            ByteSize = file.Length,
            Status = "UPLOADING"
        };

        _context.Files.Add(storedFile);
        await _context.SaveChangesAsync();

        try
        {
            using var stream = file.OpenReadStream();
            storedFile.StoredFilename = await _storageService.UploadFileAsync(stream, file.ContentType, file.FileName);
            storedFile.Status = "ACTIVE";
            await _context.SaveChangesAsync();
            
            // Audit Log
            _context.AuditEvents.Add(new AuditEvent { UserId = UserId, DeviceId = DeviceId, EventType = "file.uploaded", ResourceType = "file", ResourceId = storedFile.Id.ToString() });
            await _context.SaveChangesAsync();

            return Ok(storedFile);
        }
        catch (Exception ex)
        {
            _context.Files.Remove(storedFile);
            await _context.SaveChangesAsync();
            return StatusCode(500, new { error = new { message = "Upload failed", details = ex.Message } });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ListFiles([FromQuery] string? search, [FromQuery] string? sortBy = "date", [FromQuery] string? sortOrder = "desc")
    {
        var UserId = GetCurrentUserId();

        // Apply Search
        bool isDesc = sortOrder?.ToLower() != "asc";

        var query = _context.Files
            .Where(f => f.OwnerUserId == UserId && f.Status == "ACTIVE");

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(f => f.OriginalFilename.ToLower().Contains(search.ToLower()));

        // Project to a DTO before materialising — avoids serialising unloaded nav properties
        var filesQuery = query.Select(f => new
        {
            f.Id,
            f.OriginalFilename,
            f.MimeType,
            f.Extension,
            f.ByteSize,
            f.Status,
            f.StorageBackend,
            f.RetentionExpiresAt,
            f.CreatedAt,
            f.UpdatedAt,
            PublicShares = f.PublicShares
                .Where(s => s.RevokedAt == null)
                .Select(s => new { s.Id, s.TokenHash, s.ExpiresAt, s.AccessCount })
        });

        var files = isDesc
            ? (sortBy?.ToLower() == "size"
                ? await filesQuery.OrderByDescending(f => f.ByteSize).ToListAsync()
                : await filesQuery.OrderByDescending(f => f.CreatedAt).ToListAsync())
            : (sortBy?.ToLower() == "size"
                ? await filesQuery.OrderBy(f => f.ByteSize).ToListAsync()
                : await filesQuery.OrderBy(f => f.CreatedAt).ToListAsync());

        return Ok(files);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetFile(Guid id)
    {
        var UserId = GetCurrentUserId();
        var file = await _context.Files
            .Where(f => f.Id == id && f.OwnerUserId == UserId && f.Status == "ACTIVE")
            .Select(f => new
            {
                f.Id,
                f.OriginalFilename,
                f.MimeType,
                f.Extension,
                f.ByteSize,
                f.Status,
                f.StorageBackend,
                f.RetentionExpiresAt,
                f.CreatedAt,
                f.UpdatedAt,
                PublicShares = f.PublicShares
                    .Where(s => s.RevokedAt == null)
                    .Select(s => new { s.Id, s.TokenHash, s.ExpiresAt, s.AccessCount })
            })
            .FirstOrDefaultAsync();

        if (file == null) return NotFound();

        return Ok(file);
    }
    
    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadFile(Guid id)
    {
        var UserId = GetCurrentUserId();
        var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == id && f.OwnerUserId == UserId && f.Status == "ACTIVE");
        
        if (file == null) return NotFound();
        
        var stream = await _storageService.DownloadFileAsync(file.StoredFilename);
        return File(stream, file.MimeType, file.OriginalFilename);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(Guid id)
    {
        var UserId = GetCurrentUserId();
        var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == id && f.OwnerUserId == UserId && f.Status == "ACTIVE");
        
        if (file == null) return NotFound();

        file.Status = "DELETED";
        file.DeletedAt = DateTime.UtcNow;
        
        // Audit log
        _context.AuditEvents.Add(new AuditEvent { UserId = UserId, EventType = "file.deleted", ResourceType = "file", ResourceId = file.Id.ToString() });
        
        await _context.SaveChangesAsync();

        // In a real app, delete from storage via background job
        try
        {
            await _storageService.DeleteFileAsync(file.StoredFilename);
        }
        catch
        {
            // log error
        }

        return NoContent();
    }

    [HttpPost("{id}/share")]
    public async Task<IActionResult> ShareFile(Guid id, [FromBody] NotesAndFileBackend.Api.DTOs.CreateShareRequest request)
    {
        var UserId = GetCurrentUserId();
        var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == id && f.OwnerUserId == UserId && f.Status == "ACTIVE");
        
        if (file == null) return NotFound();

        var token = NotesAndFileBackend.Api.Helpers.TokenHelper.GenerateToken(request.Alias);
        
        var share = new PublicFileShare
        {
            FileId = file.Id,
            TokenHash = token, // Store raw token for now, in prod you might hash it if high security
            CreatedByUserId = UserId,
            ExpiresAt = request.ExpiresInHours.HasValue ? DateTime.UtcNow.AddHours(request.ExpiresInHours.Value) : null
        };

        _context.PublicFileShares.Add(share);
        
        // Audit log
        _context.AuditEvents.Add(new AuditEvent { UserId = UserId, EventType = "file.shared", ResourceType = "file", ResourceId = file.Id.ToString() });

        await _context.SaveChangesAsync();

        var publicUrl = $"{Request.Scheme}://{Request.Host}/api/v1/public/files/{token}";

        return Ok(new NotesAndFileBackend.Api.DTOs.ShareResponseDto
        {
            Id = share.Id,
            Token = token,
            PublicUrl = publicUrl,
            ExpiresAt = share.ExpiresAt
        });
    }

    [HttpDelete("{id}/share/{shareId}")]
    public async Task<IActionResult> RevokeFileShare(Guid id, Guid shareId)
    {
        var UserId = GetCurrentUserId();
        var share = await _context.PublicFileShares.FirstOrDefaultAsync(s => s.Id == shareId && s.FileId == id && s.CreatedByUserId == UserId && s.RevokedAt == null);
        
        if (share == null) return NotFound();

        share.RevokedAt = DateTime.UtcNow;
        
        // Audit log
        _context.AuditEvents.Add(new AuditEvent { UserId = UserId, EventType = "file.share_revoked", ResourceType = "file", ResourceId = id.ToString() });

        await _context.SaveChangesAsync();
        
        return NoContent();
    }
}



