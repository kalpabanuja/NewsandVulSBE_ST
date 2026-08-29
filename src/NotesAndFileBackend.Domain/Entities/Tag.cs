namespace NotesAndFileBackend.Domain.Entities;

public class Tag : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Normalized { get; set; } = string.Empty;
}
