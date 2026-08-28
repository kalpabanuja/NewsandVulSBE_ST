using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotesAndFileBackend.Infrastructure.Data;

namespace NotesAndFileBackend.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize] // Ideally we should have role checks like [Authorize(Roles = "Admin")] but we are simplifying it. For real production, ensure only admins can hit this.
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics()
    {
        // Simplistic check to see if the user is the seeded admin (in a real app, use roles or a specific Admin flag)
        var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (userEmail != "admin@notesandfile.local")
        {
            return Forbid();
        }

        var totalUsers = await _context.Users.CountAsync();
        var totalFiles = await _context.Files.Where(f => f.Status != "DELETED").CountAsync();
        var totalStorageUsed = await _context.Files.Where(f => f.Status != "DELETED").SumAsync(f => f.ByteSize);
        var totalDocuments = await _context.Documents.Where(d => d.Status != "DELETED").CountAsync();

        return Ok(new
        {
            TotalUsers = totalUsers,
            TotalFiles = totalFiles,
            TotalStorageUsed = totalStorageUsed,
            TotalDocuments = totalDocuments
        });
    }
}
