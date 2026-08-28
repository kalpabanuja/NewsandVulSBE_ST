namespace NotesAndFileBackend.Core.Entities;

public class DocumentAttachment : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    public Guid? BlockId { get; set; }

    public string ObjectKey { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long ByteSize { get; set; }
    public string Checksum { get; set; } = string.Empty;
}
