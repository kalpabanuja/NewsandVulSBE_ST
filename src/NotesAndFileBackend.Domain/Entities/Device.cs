namespace NotesAndFileBackend.Domain.Entities;

public class Device : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string DeviceName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
}
