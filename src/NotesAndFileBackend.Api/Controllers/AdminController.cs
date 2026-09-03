using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotesAndFileBackend.Infrastructure.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NotesAndFileBackend.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize] // Ideally we should have role checks like [Authorize(Roles = "Admin")] but we are simplifying it. For real production, ensure only admins can hit this.
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private const long MAX_STORAGE_BYTES = 100L * 1024 * 1024 * 1024; // 100 GB

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    private bool IsAdmin()
    {
        var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        return userEmail == "admin@notesandfile.local";
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics()
    {
        if (!IsAdmin()) return Forbid();

        var totalUsers = await _context.Users.CountAsync();
        var totalFiles = await _context.Files.Where(f => f.Status != "DELETED").CountAsync();
        var totalStorageUsed = await _context.Files.Where(f => f.Status != "DELETED").SumAsync(f => (long?)f.ByteSize) ?? 0;
        var totalNotes = await _context.Notes.Where(d => d.Status != "DELETED").CountAsync();

        return Ok(new
        {
            TotalUsers = totalUsers,
            TotalFiles = totalFiles,
            TotalStorageUsed = totalStorageUsed,
            TotalNotes = totalNotes
        });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!IsAdmin()) return Forbid();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var totalUsers = await _context.Users.CountAsync();

        var usersQuery = await _context.Users
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Email,
                StorageUsed = _context.Files.Where(f => f.OwnerUserId == u.Id && f.Status != "DELETED").Sum(f => (long?)f.ByteSize) ?? 0,
                LifetimeFiles = _context.Files.Count(f => f.OwnerUserId == u.Id),
                CurrentFiles = _context.Files.Count(f => f.OwnerUserId == u.Id && f.Status != "DELETED"),
                TotalNotes = _context.Notes.Count(n => n.UserId == u.Id && n.Status != "DELETED")
            })
            .ToListAsync();

        return Ok(new
        {
            TotalCount = totalUsers,
            Page = page,
            PageSize = pageSize,
            Data = usersQuery
        });
    }

    [HttpGet("storage/stats")]
    public async Task<IActionResult> GetStorageStats([FromQuery] string filter = "all")
    {
        if (!IsAdmin()) return Forbid();

        var totalStorageUsed = await _context.Files.Where(f => f.Status != "DELETED").SumAsync(f => (long?)f.ByteSize) ?? 0;
        var totalStorageLeft = Math.Max(0, MAX_STORAGE_BYTES - totalStorageUsed);

        DateTime? startDate = filter.ToLower() switch
        {
            "daily" => DateTime.UtcNow.AddDays(-1),
            "weekly" => DateTime.UtcNow.AddDays(-7),
            "monthly" => DateTime.UtcNow.AddMonths(-1),
            _ => null
        };

        var filesQuery = _context.Files.AsQueryable();
        var notesQuery = _context.Notes.AsQueryable();

        if (startDate.HasValue)
        {
            filesQuery = filesQuery.Where(f => f.CreatedAt >= startDate.Value);
            notesQuery = notesQuery.Where(n => n.CreatedAt >= startDate.Value);
        }

        var filesUploaded = await filesQuery.CountAsync();
        var notesUploaded = await notesQuery.CountAsync();

        return Ok(new
        {
            TotalStorageUsed = totalStorageUsed,
            TotalStorageLeft = totalStorageLeft,
            MaxStorageBytes = MAX_STORAGE_BYTES,
            Filter = filter,
            FilesUploaded = filesUploaded,
            NotesUploaded = notesUploaded
        });
    }
}
