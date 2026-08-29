namespace NotesAndFileBackend.Domain.Entities;

public class NoteRevision : BaseEntity
{
    public Guid NoteId { get; set; }
    public Note Note { get; set; } = null!;

    public int Version { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ContentJsonb { get; set; } = string.Empty;
    public string? Summary { get; set; }
    
    public Guid EditedByUserId { get; set; }
    public User EditedByUser { get; set; } = null!;
}
