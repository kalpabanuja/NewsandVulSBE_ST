namespace NotesAndFileBackend.Domain.Entities;

public class FileAccess : BaseEntity
{
    public Guid FileId { get; set; }
    public StoredFile File { get; set; } = null!;

    public string AccessType { get; set; } = string.Empty; // e.g. "ALL_AUTHENTICATED_USERS"
    
    public Guid? TargetUserId { get; set; }
    public User? TargetUser { get; set; }
}
