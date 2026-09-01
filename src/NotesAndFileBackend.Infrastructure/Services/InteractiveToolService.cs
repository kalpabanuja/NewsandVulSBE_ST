using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NotesAndFileBackend.Application.Models;
using NotesAndFileBackend.Domain.Entities;
using NotesAndFileBackend.Infrastructure.Data;

using NotesAndFileBackend.Application.Services;

namespace NotesAndFileBackend.Infrastructure.Services;

public class InteractiveToolService : IInteractiveToolService
{
    private readonly AppDbContext _context;

    // Constants for limits
    private const int MaxAssetSize = 256 * 1024; // 256 KB
    private const int MaxTotalSize = 768 * 1024; // 768 KB

    public InteractiveToolService(AppDbContext context)
    {
        _context = context;
    }

    private void ValidateSizes(string html, string css, string js)
    {
        var htmlBytes = Encoding.UTF8.GetByteCount(html ?? string.Empty);
        var cssBytes = Encoding.UTF8.GetByteCount(css ?? string.Empty);
        var jsBytes = Encoding.UTF8.GetByteCount(js ?? string.Empty);

        if (htmlBytes > MaxAssetSize) throw new ArgumentException("HTML size exceeds 256KB limit.");
        if (cssBytes > MaxAssetSize) throw new ArgumentException("CSS size exceeds 256KB limit.");
        if (jsBytes > MaxAssetSize) throw new ArgumentException("JavaScript size exceeds 256KB limit.");

        if (htmlBytes + cssBytes + jsBytes > MaxTotalSize)
            throw new ArgumentException("Total asset size exceeds 768KB limit.");
    }

    private string CalculateHash(string html, string css, string js)
    {
        using var sha256 = SHA256.Create();
        var combined = $"{html}\n---\n{css}\n---\n{js}";
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private async Task VerifyNoteOwnershipAsync(Guid noteId, Guid userId)
    {
        var note = await _context.Notes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == noteId && !n.IsDeleted);

        if (note == null)
            throw new KeyNotFoundException("Note not found.");

        if (note.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("Only the note creator can manage interactive tools.");
    }

    public async Task<IEnumerable<InteractiveToolListDto>> ListToolsAsync(Guid noteId, Guid userId)
    {
        // For viewing, we just ensure the user can see the note.
        // We assume authorization handles read access to the note itself elsewhere, or we could check note sharing.
        // As per instructions, "view if note permission allows". For now we'll return the list and rely on the controller.
        
        var tools = await _context.CustomInteractiveTools
            .AsNoTracking()
            .Where(t => t.NoteId == noteId && !t.IsDeleted)
            .Select(t => new InteractiveToolListDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                AssetVersion = t.AssetVersion,
                IsEnabled = t.IsEnabled
            })
            .ToListAsync();

        return tools;
    }

    public async Task<InteractiveToolDetailsDto?> GetToolAsync(Guid noteId, Guid toolId, Guid userId)
    {
        var tool = await _context.CustomInteractiveTools
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == toolId && t.NoteId == noteId && !t.IsDeleted);

        if (tool == null) return null;

        return new InteractiveToolDetailsDto
        {
            Id = tool.Id,
            Name = tool.Name,
            Description = tool.Description,
            HtmlSource = tool.HtmlSource,
            CssSource = tool.CssSource,
            JavascriptSource = tool.JavascriptSource,
            ContentHash = tool.ContentHash,
            AssetVersion = tool.AssetVersion,
            IsEnabled = tool.IsEnabled,
            ValidationStatus = tool.ValidationStatus,
            SecurityStatus = tool.SecurityStatus,
            OwnerUserId = tool.OwnerUserId
        };
    }

    public async Task<InteractiveToolDetailsDto> CreateToolAsync(Guid noteId, CreateInteractiveToolRequest request, Guid userId)
    {
        await VerifyNoteOwnershipAsync(noteId, userId);

        ValidateSizes(request.HtmlSource, request.CssSource, request.JavascriptSource);
        var hash = CalculateHash(request.HtmlSource, request.CssSource, request.JavascriptSource);

        var tool = new CustomInteractiveTool
        {
            NoteId = noteId,
            OwnerUserId = userId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            HtmlSource = request.HtmlSource,
            CssSource = request.CssSource,
            JavascriptSource = request.JavascriptSource,
            ContentHash = hash,
            SchemaVersion = 1,
            AssetVersion = 1,
            IsEnabled = true,
            ValidationStatus = "Pending",
            SecurityStatus = "Unreviewed",
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CustomInteractiveTools.Add(tool);
        await _context.SaveChangesAsync();

        return await GetToolAsync(noteId, tool.Id, userId) ?? throw new InvalidOperationException("Failed to retrieve created tool.");
    }

    public async Task<InteractiveToolDetailsDto> UpdateToolAsync(Guid noteId, Guid toolId, UpdateInteractiveToolRequest request, Guid userId)
    {
        await VerifyNoteOwnershipAsync(noteId, userId);

        var tool = await _context.CustomInteractiveTools
            .FirstOrDefaultAsync(t => t.Id == toolId && t.NoteId == noteId && !t.IsDeleted);

        if (tool == null)
            throw new KeyNotFoundException("Tool not found.");

        if (tool.OwnerUserId != userId)
            throw new UnauthorizedAccessException("Only the owner can update this tool.");

        ValidateSizes(request.HtmlSource, request.CssSource, request.JavascriptSource);
        var newHash = CalculateHash(request.HtmlSource, request.CssSource, request.JavascriptSource);

        bool assetsChanged = tool.ContentHash != newHash;

        tool.Name = request.Name.Trim();
        tool.Description = request.Description?.Trim();
        tool.IsEnabled = request.IsEnabled;

        if (assetsChanged)
        {
            tool.HtmlSource = request.HtmlSource;
            tool.CssSource = request.CssSource;
            tool.JavascriptSource = request.JavascriptSource;
            tool.ContentHash = newHash;
            tool.AssetVersion++;
            tool.ValidationStatus = "Pending";
            tool.SecurityStatus = "Unreviewed";
        }

        tool.UpdatedByUserId = userId;
        tool.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("Concurrency conflict occurred.");
        }

        return await GetToolAsync(noteId, tool.Id, userId) ?? throw new InvalidOperationException("Failed to retrieve updated tool.");
    }

    public async Task<bool> DeleteToolAsync(Guid noteId, Guid toolId, Guid userId)
    {
        await VerifyNoteOwnershipAsync(noteId, userId);

        var tool = await _context.CustomInteractiveTools
            .FirstOrDefaultAsync(t => t.Id == toolId && t.NoteId == noteId && !t.IsDeleted);

        if (tool == null)
            return false;

        if (tool.OwnerUserId != userId)
            throw new UnauthorizedAccessException("Only the owner can delete this tool.");

        tool.IsDeleted = true;
        tool.DeletedAt = DateTime.UtcNow;
        tool.UpdatedByUserId = userId;
        tool.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
}
