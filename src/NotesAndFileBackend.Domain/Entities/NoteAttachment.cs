namespace NotesAndFileBackend.Domain.Entities;

public class NoteAttachment : BaseEntity
{
    public Guid NoteId { get; set; }
    public Note Note { get; set; } = null!;

    public Guid? BlockId { get; set; }

    public string ObjectKey { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long ByteSize { get; set; }
    public string Checksum { get; set; } = string.Empty;
}
