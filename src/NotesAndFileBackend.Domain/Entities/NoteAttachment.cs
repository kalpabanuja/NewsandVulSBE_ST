namespace NotesAndFileBackend.Domain.Entities;

public class NoteAttachment : BaseEntity
{
    public Guid NoteId { get; set; }
    public Note Note { get; set; } = null!;

    /// <summary>The owner for authorization without joining through Note.</summary>
    public Guid OwnerUserId { get; set; }
    public User OwnerUser { get; set; } = null!;

    /// <summary>The block in ContentJsonb that references this attachment.</summary>
    public Guid? BlockId { get; set; }

    /// <summary>Display or Downloadable</summary>
    public string AttachmentType { get; set; } = "Downloadable";

    /// <summary>User-supplied display name for downloadable attachments.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Storage key (object key in MinIO/S3). Never exposed directly to clients.</summary>
    public string ObjectKey { get; set; } = string.Empty;

    /// <summary>Original filename as supplied by the client (sanitized on server).</summary>
    public string Filename { get; set; } = string.Empty;

    /// <summary>Server-detected MIME type (not trusted from client).</summary>
    public string MimeType { get; set; } = string.Empty;

    public long ByteSize { get; set; }

    /// <summary>SHA-256 checksum of file content.</summary>
    public string Checksum { get; set; } = string.Empty;

    // Optional display attachment metadata
    public int? Width { get; set; }
    public int? Height { get; set; }
    public double? DurationSeconds { get; set; }
    public string? ThumbnailObjectKey { get; set; }
}
