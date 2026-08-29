using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotesAndFileBackend.Api.DTOs;
using NotesAndFileBackend.Domain.Entities;
using NotesAndFileBackend.Infrastructure.Data;

namespace NotesAndFileBackend.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriesController(AppDbContext context)
    {
        _context = context;
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> ListCategories()
    {
        var userId = GetCurrentUserId();

        var categories = await _context.Categories
            .Where(c => c.UserId == userId)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.SortOrder,
                c.CreatedAt
            })
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        var userId = GetCurrentUserId();

        var category = new Category
        {
            UserId = userId,
            Name = request.Name,
            Slug = request.Name.ToLower().Replace(" ", "-").Trim(),
            Description = request.Description,
            SortOrder = request.SortOrder
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(ListCategories), new { id = category.Id }, new { 
            category.Id, 
            category.Name, 
            category.Description,
            category.SortOrder
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryRequest request)
    {
        var userId = GetCurrentUserId();

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            category.Name = request.Name;
            category.Slug = request.Name.ToLower().Replace(" ", "-").Trim();
        }

        if (request.Description != null)
            category.Description = request.Description;

        if (request.SortOrder.HasValue)
            category.SortOrder = request.SortOrder.Value;

        _context.Categories.Update(category);
        await _context.SaveChangesAsync();

        return Ok(new { category.Id, category.Name, category.Description, category.SortOrder });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var userId = GetCurrentUserId();

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category == null) return NotFound();

        // Nullify the category on any notes that use it (since it's an optional foreign key).
        // Alternatively, EF Core's OnDelete behavior might handle this based on mapping,
        // but it's safe to be explicit if it's not configured to Cascade/SetNull natively.
        var notesToUpdate = await _context.Notes.Where(n => n.CategoryId == id).ToListAsync();
        foreach (var note in notesToUpdate)
        {
            note.CategoryId = null;
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
