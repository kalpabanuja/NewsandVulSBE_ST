namespace NewsAndVulBackend.Core.Entities;

public class DocumentBlock : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    public string BlockType { get; set; } = string.Empty; // heading, paragraph, code_block, copy_card, attachment
    public int Position { get; set; }
    
    // Storing content as JSON string to support different block structures
    public string ContentJson { get; set; } = string.Empty;
}
