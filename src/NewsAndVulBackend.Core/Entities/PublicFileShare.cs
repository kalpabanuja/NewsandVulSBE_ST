namespace NewsAndVulBackend.Core.Entities;

public class PublicFileShare : BaseEntity
{
    public Guid FileId { get; set; }
    public StoredFile File { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;
    
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public int AccessCount { get; set; } = 0;
}
