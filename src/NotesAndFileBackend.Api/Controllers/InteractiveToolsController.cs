using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotesAndFileBackend.Application.Models;
using NotesAndFileBackend.Application.Services;

namespace NotesAndFileBackend.Api.Controllers;

[ApiController]
[Route("api/notes/{noteId}/interactive-tools")]
[Authorize]
public class InteractiveToolsController : ControllerBase
{
    private readonly IInteractiveToolService _toolService;

    public InteractiveToolsController(IInteractiveToolService toolService)
    {
        _toolService = toolService;
    }

    private Guid GetUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdString, out var userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("Invalid user identity.");
    }

    [HttpGet]
    public async Task<IActionResult> ListTools(Guid noteId)
    {
        try
        {
            var userId = GetUserId();
            var tools = await _toolService.ListToolsAsync(noteId, userId);
            return Ok(tools);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{toolId}")]
    public async Task<IActionResult> GetTool(Guid noteId, Guid toolId)
    {
        try
        {
            var userId = GetUserId();
            var tool = await _toolService.GetToolAsync(noteId, toolId, userId);
            if (tool == null)
            {
                return NotFound();
            }
            return Ok(tool);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateTool(Guid noteId, [FromBody] CreateInteractiveToolRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetUserId();
            var tool = await _toolService.CreateToolAsync(noteId, request, userId);
            return CreatedAtAction(nameof(GetTool), new { noteId, toolId = tool.Id }, tool);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{toolId}")]
    public async Task<IActionResult> UpdateTool(Guid noteId, Guid toolId, [FromBody] UpdateInteractiveToolRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetUserId();
            var tool = await _toolService.UpdateToolAsync(noteId, toolId, request, userId);
            return Ok(tool);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{toolId}")]
    public async Task<IActionResult> DeleteTool(Guid noteId, Guid toolId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _toolService.DeleteToolAsync(noteId, toolId, userId);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
