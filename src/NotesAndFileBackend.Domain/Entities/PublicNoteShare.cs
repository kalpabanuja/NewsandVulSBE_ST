namespace NotesAndFileBackend.Domain.Entities;

public class PublicNoteShare : BaseEntity
{
    public Guid NoteId { get; set; }
    public Note Note { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;
    
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    
    public string? PasswordHash { get; set; }
    public bool AllowIndexing { get; set; } = false;
    public int? MaxViews { get; set; }
    public int ViewCount { get; set; } = 0;
}
