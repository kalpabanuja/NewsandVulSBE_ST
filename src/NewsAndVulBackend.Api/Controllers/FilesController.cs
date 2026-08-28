using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAndVulBackend.Core.Entities;
using NewsAndVulBackend.Core.Interfaces;
using NewsAndVulBackend.Infrastructure.Data;

namespace NewsAndVulBackend.Api.Controllers;

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
        var claim = User.FindFirst("deviceId");
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = new { code = "INVALID_FILE", message = "File is empty or not provided." } });
        }

        var userId = GetCurrentUserId();
        var deviceId = GetCurrentDeviceId();

        // Check Quotas
        var userFiles = await _context.Files
            .Where(f => f.OwnerUserId == userId && f.Status != "DELETED")
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
            OwnerUserId = userId,
            OwnerDeviceId = deviceId,
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
            _context.AuditEvents.Add(new AuditEvent { UserId = userId, DeviceId = deviceId, EventType = "file.uploaded", ResourceType = "file", ResourceId = storedFile.Id.ToString() });
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
    public async Task<IActionResult> ListFiles()
    {
        var userId = GetCurrentUserId();
        var files = await _context.Files
            .Where(f => f.OwnerUserId == userId && f.Status == "ACTIVE")
            .ToListAsync();
            
        return Ok(files);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetFile(Guid id)
    {
        var userId = GetCurrentUserId();
        var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == id && f.OwnerUserId == userId && f.Status == "ACTIVE");
        
        if (file == null) return NotFound();
        
        return Ok(file);
    }
    
    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadFile(Guid id)
    {
        var userId = GetCurrentUserId();
        var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == id && f.OwnerUserId == userId && f.Status == "ACTIVE");
        
        if (file == null) return NotFound();
        
        var url = await _storageService.GeneratePresignedDownloadUrlAsync(file.StoredFilename, TimeSpan.FromHours(1));
        return Ok(new { DownloadUrl = url });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(Guid id)
    {
        var userId = GetCurrentUserId();
        var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == id && f.OwnerUserId == userId && f.Status == "ACTIVE");
        
        if (file == null) return NotFound();

        file.Status = "DELETED";
        file.DeletedAt = DateTime.UtcNow;
        
        // Audit log
        _context.AuditEvents.Add(new AuditEvent { UserId = userId, EventType = "file.deleted", ResourceType = "file", ResourceId = file.Id.ToString() });
        
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
}
