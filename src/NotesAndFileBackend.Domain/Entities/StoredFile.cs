namespace NotesAndFileBackend.Domain.Entities;

public class StoredFile : BaseEntity
{
    public Guid OwnerUserId { get; set; }
    public User OwnerUser { get; set; } = null!;

    public Guid? OwnerDeviceId { get; set; }
    public Device? OwnerDevice { get; set; }

    public string OriginalFilename { get; set; } = string.Empty;
    public string StoredFilename { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long ByteSize { get; set; }
    public string Checksum { get; set; } = string.Empty;

    public string Status { get; set; } = "PENDING"; // PENDING, UPLOADING, ACTIVE, DELETED
    public bool ShareWithEveryone { get; set; } = false;
    
    public DateTime? RetentionExpiresAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public string StorageBackend { get; set; } = "LOCAL";
    public string UploadSessionId { get; set; } = string.Empty;

    public ICollection<FileAccess> AccessList { get; set; } = new List<FileAccess>();
    public ICollection<PublicFileShare> PublicShares { get; set; } = new List<PublicFileShare>();
}
