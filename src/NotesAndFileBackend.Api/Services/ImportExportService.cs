using System.Text.Json;
using NotesAndFileBackend.Api.DTOs;
using NotesAndFileBackend.Domain.Entities;
using NotesAndFileBackend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NotesAndFileBackend.Application.Services;

namespace NotesAndFileBackend.Api.Services;

public interface IImportExportService
{
    Task<NoteExportFormat> ExportNotesAsync(Guid userId, ExportRequestDto request);
    Task<ImportResultDto> ImportNotesAsync(Guid userId, ImportRequestDto request);
}

public class ImportExportService : IImportExportService
{
    private readonly AppDbContext _context;

    public ImportExportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<NoteExportFormat> ExportNotesAsync(Guid userId, ExportRequestDto request)
    {
        var query = _context.Notes
            .Include(n => n.Category)
            .Include(n => n.NoteTags).ThenInclude(nt => nt.Tag)
            .Where(n => n.UserId == userId && n.IsDeleted == false);

        if (request.NoteIds != null && request.NoteIds.Any())
        {
            query = query.Where(n => request.NoteIds.Contains(n.Id));
        }

        var notes = await query.ToListAsync();

        var exportFormat = new NoteExportFormat
        {
            Format = "notes",
            Version = 1,
            ExportedAt = DateTime.UtcNow,
            Notes = notes.Select(n => new ExportedNote
            {
                Title = n.Title,
                Summary = n.Summary,
                ToolName = n.ToolName ?? string.Empty,
                ContentJsonb = JsonSerializer.Deserialize<JsonElement>(n.ContentJsonb),
                Tags = n.NoteTags.Select(nt => nt.Tag.Name).ToList(),
                Category = n.Category?.Name,
                IsPinned = n.IsPinned,
                IsFavorite = n.IsFavorite,
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt
            }).ToList()
        };

        return exportFormat;
    }

    public async Task<ImportResultDto> ImportNotesAsync(Guid userId, ImportRequestDto request)
    {
        var job = new NoteImportJob
        {
            UserId = userId,
            FileName = "Manual Import",
            Status = "PROCESSING",
            TotalItems = request.Payload.Notes?.Count ?? 0,
            Processed = 0,
            Failed = 0
        };

        _context.NoteImportJobs.Add(job);
        await _context.SaveChangesAsync();

        var errors = new List<string>();

        if (request.Payload.Format != "notes" || request.Payload.Version != 1)
        {
            errors.Add("Invalid format or unsupported version.");
            job.Status = "FAILED";
            job.ErrorJsonb = JsonSerializer.Serialize(errors);
            job.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return new ImportResultDto { JobId = job.Id, Status = job.Status, Errors = errors };
        }

        if (request.Payload.Notes == null || !request.Payload.Notes.Any())
        {
            job.Status = "COMPLETED";
            job.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return new ImportResultDto { JobId = job.Id, Status = job.Status, Processed = 0 };
        }

        // Cache categories/tags to avoid n+1 overhead during import mapping
        var existingCategories = await _context.Categories.Where(c => c.UserId == userId).ToListAsync();
        var existingTags = await _context.Tags.Where(t => t.UserId == userId).ToListAsync();

        foreach (var exportNote in request.Payload.Notes)
        {
            try
            {
                // Find or create Category
                Guid? categoryId = null;
                if (!string.IsNullOrWhiteSpace(exportNote.Category))
                {
                    var catSlug = exportNote.Category.ToLower().Replace(" ", "-");
                    var cat = existingCategories.FirstOrDefault(c => c.Slug == catSlug);
                    if (cat == null)
                    {
                        cat = new Category { UserId = userId, Name = exportNote.Category, Slug = catSlug };
                        _context.Categories.Add(cat);
                        existingCategories.Add(cat);
                    }
                    categoryId = cat.Id;
                }

                // Title and Slug
                var title = string.IsNullOrWhiteSpace(exportNote.Title) ? "Untitled Import" : exportNote.Title;
                var slug = $"{title.ToLower().Replace(" ", "-")}-{Guid.NewGuid().ToString("N").Substring(0, 4)}";

                var newNote = new Note
                {
                    UserId = userId,
                    CreatedByUserId = userId,
                    UpdatedByUserId = userId,
                    Title = title,
                    Summary = exportNote.Summary ?? string.Empty,
                    Slug = slug,
                    CategoryId = categoryId,
                    ToolName = exportNote.ToolName,
                    ContentJsonb = exportNote.ContentJsonb.ValueKind != JsonValueKind.Undefined ? exportNote.ContentJsonb.GetRawText() : "{}",
                    SearchText = NoteTextExtractor.ExtractText(exportNote.ContentJsonb.ValueKind != JsonValueKind.Undefined ? exportNote.ContentJsonb.GetRawText() : "{}"),
                    IsPinned = exportNote.IsPinned,
                    IsFavorite = exportNote.IsFavorite,
                    Version = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // NoteTags
                if (exportNote.Tags != null && exportNote.Tags.Any())
                {
                    var normalizedNames = exportNote.Tags.Select(t => t.ToLowerInvariant().Trim()).Distinct().ToList();
                    foreach (var name in normalizedNames)
                    {
                        var tag = existingTags.FirstOrDefault(t => t.Normalized == name);
                        if (tag == null)
                        {
                            tag = new Tag { UserId = userId, Name = exportNote.Tags.First(t => t.ToLowerInvariant().Trim() == name), Normalized = name };
                            _context.Tags.Add(tag);
                            existingTags.Add(tag);
                        }
                        newNote.NoteTags.Add(new NoteTag { Tag = tag });
                    }
                }

                _context.Notes.Add(newNote);
                job.Processed++;
            }
            catch (Exception ex)
            {
                job.Failed++;
                errors.Add($"Failed to import note '{exportNote.Title}': {ex.Message}");
            }
        }

        job.Status = job.Failed > 0 ? "PARTIAL" : "COMPLETED";
        if (errors.Any()) job.ErrorJsonb = JsonSerializer.Serialize(errors);
        job.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new ImportResultDto
        {
            JobId = job.Id,
            Status = job.Status,
            Processed = job.Processed,
            Failed = job.Failed,
            Errors = errors
        };
    }
}
