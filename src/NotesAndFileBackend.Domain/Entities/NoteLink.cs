namespace NotesAndFileBackend.Domain.Entities;

public class NoteLink : BaseEntity
{
    public Guid NoteId { get; set; }
    public Note Note { get; set; } = null!;

    public string? BlockId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
}
