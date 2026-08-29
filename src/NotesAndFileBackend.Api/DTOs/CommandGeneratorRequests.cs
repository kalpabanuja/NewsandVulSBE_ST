using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace NotesAndFileBackend.Api.DTOs;

public class CreateCommandGeneratorRequest
{
    [Required]
    public Guid NoteId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [Required]
    public string ToolName { get; set; } = string.Empty;
    
    [Required]
    public string Template { get; set; } = string.Empty;
    
    [Required]
    public JsonElement Schema { get; set; }
}

public class UpdateCommandGeneratorRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ToolName { get; set; }
    public string? Template { get; set; }
    public JsonElement? Schema { get; set; }
    public bool? IsEnabled { get; set; }
}
