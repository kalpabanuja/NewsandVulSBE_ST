namespace NotesAndFileBackend.Domain.Entities;

public class NoteCommandGenerator
{
    public Guid Id { get; set; }
    public Guid NoteId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public string ToolName { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    
    /// <summary>
    /// Stores the CommandGeneratorDefinition (fields/options) as JSONB.
    /// </summary>
    public string SchemaJsonb { get; set; } = string.Empty;
    
    public bool IsEnabled { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Note? Note { get; set; }
}
