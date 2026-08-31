namespace NotesAndFileBackend.Domain.Entities;

public class NoteCommandGenerator
{
    public Guid Id { get; set; }
    public Guid NoteId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    /// For C# template-based generators. Used when Language = "csharp_template".
    /// </summary>
    public string Template { get; set; } = string.Empty;
    
    /// <summary>
    /// Stores the CommandGeneratorDefinition (fields/options) as JSONB.
    /// </summary>
    public string SchemaJsonb { get; set; } = string.Empty;

    /// <summary>
    /// "csharp_template" (legacy) or "javascript" (Jint).
    /// </summary>
    public string Language { get; set; } = "javascript";

    /// <summary>
    /// The JavaScript source code for generators using Language = "javascript".
    /// Executed by the Jint sandboxed runtime. Never executed by the OS shell.
    /// </summary>
    public string? Script { get; set; }
    
    public bool IsEnabled { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Note? Note { get; set; }
}
