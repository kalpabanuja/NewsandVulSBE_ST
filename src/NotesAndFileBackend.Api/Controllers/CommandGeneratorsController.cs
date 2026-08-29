using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotesAndFileBackend.Api.DTOs;
using NotesAndFileBackend.Application.Models;
using NotesAndFileBackend.Application.Services;
using NotesAndFileBackend.Infrastructure.Data;

namespace NotesAndFileBackend.Api.Controllers;

[ApiController]
[Route("api/v1/command-generators")]
[Authorize]
public class CommandGeneratorsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICommandGenerator _generatorService;

    public CommandGeneratorsController(AppDbContext context, ICommandGenerator generatorService)
    {
        _context = context;
        _generatorService = generatorService;
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> ListCommandGenerators()
    {
        var userId = GetCurrentUserId();

        // Get all generators attached to notes owned by this user
        var generators = await _context.NoteCommandGenerators
            .Include(g => g.Note)
            .Where(g => g.Note != null && g.Note.UserId == userId && g.Note.IsDeleted == false)
            .Select(g => new
            {
                g.Id,
                g.NoteId,
                g.Name,
                g.Description,
                g.ToolName,
                g.IsEnabled,
                g.CreatedAt
            })
            .OrderBy(g => g.Name)
            .ToListAsync();

        return Ok(generators);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCommandGenerator(Guid id)
    {
        var userId = GetCurrentUserId();

        // Must verify the generator belongs to an accessible note (i.e. not deleted, user owns it)
        var generator = await _context.NoteCommandGenerators
            .Include(g => g.Note)
            .FirstOrDefaultAsync(g => g.Id == id && g.Note != null && g.Note.UserId == userId && g.Note.IsDeleted == false);

        if (generator == null) return NotFound();

        if (!generator.IsEnabled) return BadRequest("This command generator is disabled.");

        CommandGeneratorDefinition? definition;
        try
        {
            definition = JsonSerializer.Deserialize<CommandGeneratorDefinition>(generator.SchemaJsonb, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return StatusCode(500, "Invalid schema structure stored.");
        }

        if (definition == null) return StatusCode(500, "Definition could not be loaded.");

        // Inject dynamic values from DB entity
        definition.Id = generator.Id;
        definition.Name = generator.Name;
        definition.ToolName = generator.ToolName;
        definition.Template = generator.Template;

        return Ok(definition);
    }

    [HttpPost("{id}/generate")]
    public async Task<IActionResult> GenerateCommand(Guid id, [FromBody] GenerateCommandRequest request)
    {
        var userId = GetCurrentUserId();

        var generator = await _context.NoteCommandGenerators
            .Include(g => g.Note)
            .FirstOrDefaultAsync(g => g.Id == id && g.Note != null && g.Note.UserId == userId && g.Note.IsDeleted == false);

        if (generator == null) return NotFound();
        if (!generator.IsEnabled) return BadRequest("This command generator is disabled.");

        CommandGeneratorDefinition? definition;
        try
        {
            definition = JsonSerializer.Deserialize<CommandGeneratorDefinition>(generator.SchemaJsonb, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return StatusCode(500, "Invalid schema structure stored.");
        }

        if (definition == null) return StatusCode(500, "Definition could not be loaded.");
        definition.Template = generator.Template; // Inject template

        var result = _generatorService.Generate(definition, request.Values);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        // Return generated command
        // Note: As per architecture security guidelines, the server MUST NOT execute this command.
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCommandGenerator([FromBody] CreateCommandGeneratorRequest request)
    {
        var userId = GetCurrentUserId();

        // Verify the Note exists and belongs to the user
        var note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == request.NoteId && n.UserId == userId && n.IsDeleted == false);
        if (note == null) return NotFound("Associated note not found or access denied.");

        var generator = new NotesAndFileBackend.Domain.Entities.NoteCommandGenerator
        {
            NoteId = request.NoteId,
            Name = request.Name,
            Description = request.Description,
            ToolName = request.ToolName,
            Template = request.Template,
            SchemaJsonb = request.Schema.GetRawText(),
            IsEnabled = true
        };

        _context.NoteCommandGenerators.Add(generator);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCommandGenerator), new { id = generator.Id }, new { id = generator.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCommandGenerator(Guid id, [FromBody] UpdateCommandGeneratorRequest request)
    {
        var userId = GetCurrentUserId();

        var generator = await _context.NoteCommandGenerators
            .Include(g => g.Note)
            .FirstOrDefaultAsync(g => g.Id == id && g.Note != null && g.Note.UserId == userId && g.Note.IsDeleted == false);

        if (generator == null) return NotFound();

        if (request.Name != null) generator.Name = request.Name;
        if (request.Description != null) generator.Description = request.Description;
        if (request.ToolName != null) generator.ToolName = request.ToolName;
        if (request.Template != null) generator.Template = request.Template;
        if (request.Schema.HasValue && request.Schema.Value.ValueKind != JsonValueKind.Undefined) 
            generator.SchemaJsonb = request.Schema.Value.GetRawText();
        if (request.IsEnabled.HasValue) generator.IsEnabled = request.IsEnabled.Value;

        generator.UpdatedAt = DateTime.UtcNow;
        _context.NoteCommandGenerators.Update(generator);
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCommandGenerator(Guid id)
    {
        var userId = GetCurrentUserId();

        var generator = await _context.NoteCommandGenerators
            .Include(g => g.Note)
            .FirstOrDefaultAsync(g => g.Id == id && g.Note != null && g.Note.UserId == userId && g.Note.IsDeleted == false);

        if (generator == null) return NotFound();

        _context.NoteCommandGenerators.Remove(generator);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
