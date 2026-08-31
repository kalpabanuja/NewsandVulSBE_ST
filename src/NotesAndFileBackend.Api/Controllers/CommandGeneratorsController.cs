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
    // Keep the legacy C# generator for any existing csharp_template generators
    private readonly ICommandGenerator _legacyGeneratorService;

    public CommandGeneratorsController(AppDbContext context, ICommandGenerator generatorService)
    {
        _context = context;
        _legacyGeneratorService = generatorService;
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
                g.Language,
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

        var generator = await _context.NoteCommandGenerators
            .Include(g => g.Note)
            .FirstOrDefaultAsync(g => g.Id == id && g.Note != null && g.Note.UserId == userId && g.Note.IsDeleted == false);

        if (generator == null) return NotFound();
        if (!generator.IsEnabled) return BadRequest("This command generator is disabled.");

        if (generator.Language == "javascript")
        {
            // Return JavaScript generator metadata (fields from schema) without the script
            CommandGeneratorDefinition? definition = null;
            try
            {
                definition = JsonSerializer.Deserialize<CommandGeneratorDefinition>(generator.SchemaJsonb, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { /* fallback */ }

            return Ok(new
            {
                generator.Id,
                generator.Name,
                generator.Description,
                generator.ToolName,
                generator.Language,
                generator.IsEnabled,
                Fields = definition?.Fields ?? new List<CommandFieldDefinition>()
            });
        }
        else
        {
            // Legacy csharp_template path
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
            definition.Id = generator.Id;
            definition.Name = generator.Name;
            definition.ToolName = generator.ToolName;
            definition.Template = generator.Template;

            return Ok(definition);
        }
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

        // Audit log
        _context.AuditEvents.Add(new Domain.Entities.AuditEvent
        {
            UserId = userId,
            EventType = "command_generator_generated",
            ResourceType = "command_generator",
            ResourceId = generator.Id.ToString()
        });
        await _context.SaveChangesAsync();

        if (generator.Language == "javascript")
        {
            if (string.IsNullOrWhiteSpace(generator.Script))
                return BadRequest(new { error = new { code = "NO_SCRIPT", message = "Generator has no JavaScript script." } });

            // Convert JsonElement values to strings for the Jint runtime
            var inputs = ConvertToStringInputs(request.Values);
            var result = JintCommandGeneratorService.Execute(generator.Script, inputs);

            if (!result.Success)
                return BadRequest(new { success = false, errors = result.Errors });

            // IMPORTANT: The generated command is returned as text only.
            // It is NEVER executed by the server via any OS shell or process API.
            return Ok(new
            {
                success = true,
                command = result.Output,
                displayCommand = result.Output,
                warnings = Array.Empty<string>()
            });
        }
        else
        {
            // Legacy C# template path
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
            definition.Template = generator.Template;

            var result = _legacyGeneratorService.Generate(definition, request.Values);
            if (!result.Success) return BadRequest(result);

            return Ok(result);
        }
    }

    /// <summary>
    /// Test a generator with draft inputs. Uses the same Jint sandbox as production.
    /// Does NOT persist anything. Accepts an optional DraftScript override.
    /// </summary>
    [HttpPost("{id}/test")]
    public async Task<IActionResult> TestCommandGenerator(Guid id, [FromBody] TestCommandGeneratorRequest request)
    {
        var userId = GetCurrentUserId();

        var generator = await _context.NoteCommandGenerators
            .Include(g => g.Note)
            .FirstOrDefaultAsync(g => g.Id == id && g.Note != null && g.Note.UserId == userId && g.Note.IsDeleted == false);

        if (generator == null) return NotFound();

        var scriptToTest = request.DraftScript ?? generator.Script ?? string.Empty;

        if (string.IsNullOrWhiteSpace(scriptToTest))
            return BadRequest(new { success = false, errors = new[] { "No script to test." } });

        var inputs = request.Values ?? new Dictionary<string, string>();

        // Same sandbox, same security restrictions — no persistence
        var result = JintCommandGeneratorService.Execute(scriptToTest, inputs);

        return Ok(new
        {
            result.Success,
            Output = result.Output,
            Warnings = Array.Empty<string>(),
            Errors = result.Errors
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateCommandGenerator([FromBody] CreateCommandGeneratorRequest request)
    {
        var userId = GetCurrentUserId();

        var note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == request.NoteId && n.UserId == userId && n.IsDeleted == false);
        if (note == null) return NotFound("Associated note not found or access denied.");

        var language = request.Language?.ToLowerInvariant() == "csharp_template" ? "csharp_template" : "javascript";

        // Validate JavaScript syntax before saving
        if (language == "javascript" && !string.IsNullOrWhiteSpace(request.Script))
        {
            var (isValid, syntaxError) = JintCommandGeneratorService.ValidateSyntax(request.Script);
            if (!isValid)
                return BadRequest(new { error = new { code = "INVALID_SCRIPT_SYNTAX", message = syntaxError } });
        }

        var generator = new NotesAndFileBackend.Domain.Entities.NoteCommandGenerator
        {
            NoteId = request.NoteId,
            Name = request.Name,
            Description = request.Description,
            ToolName = request.ToolName,
            Language = language,
            Script = language == "javascript" ? request.Script : null,
            Template = language == "csharp_template" ? request.Template : string.Empty,
            SchemaJsonb = request.Schema.ValueKind != JsonValueKind.Undefined ? request.Schema.GetRawText() : "{}",
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
        if (request.Language != null)
            generator.Language = request.Language.ToLowerInvariant() == "csharp_template" ? "csharp_template" : "javascript";

        if (request.Script != null)
        {
            if (generator.Language == "javascript")
            {
                var (isValid, syntaxError) = JintCommandGeneratorService.ValidateSyntax(request.Script);
                if (!isValid)
                    return BadRequest(new { error = new { code = "INVALID_SCRIPT_SYNTAX", message = syntaxError } });
                generator.Script = request.Script;
            }
        }

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

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Converts JsonElement input values to plain strings for the Jint runtime.
    /// Only plain string/primitive values are passed — no .NET object graphs.
    /// </summary>
    private static Dictionary<string, string> ConvertToStringInputs(Dictionary<string, JsonElement>? values)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (values == null) return result;

        foreach (var (key, el) in values)
        {
            result[key] = el.ValueKind == JsonValueKind.String ? el.GetString() ?? string.Empty : el.GetRawText();
        }
        return result;
    }
}
