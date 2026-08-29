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
}
