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
    
    /// <summary>"javascript" (Jint) or "csharp_template" (legacy).</summary>
    public string Language { get; set; } = "javascript";

    /// <summary>JavaScript source for Language = "javascript" generators.</summary>
    public string? Script { get; set; }

    /// <summary>C# template string. Only used when Language = "csharp_template".</summary>
    public string Template { get; set; } = string.Empty;
    
    [Required]
    public JsonElement Schema { get; set; }
}

public class UpdateCommandGeneratorRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ToolName { get; set; }
    public string? Language { get; set; }
    public string? Script { get; set; }
    public string? Template { get; set; }
    public JsonElement? Schema { get; set; }
    public bool? IsEnabled { get; set; }
}

/// <summary>
/// Request body for POST /api/v1/command-generators/{id}/test.
/// Runs the generator with draft inputs without persisting anything.
/// </summary>
public class TestCommandGeneratorRequest
{
    /// <summary>Input values keyed by field key.</summary>
    public Dictionary<string, string> Values { get; set; } = new();

    /// <summary>
    /// Optional override script for testing a draft without saving.
    /// If omitted, the persisted script is used.
    /// </summary>
    public string? DraftScript { get; set; }
}
