namespace NotesAndFileBackend.Domain.Entities;

public class NoteImportJob : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string? FileName { get; set; }
    
    // Status can be: PENDING, PROCESSING, COMPLETED, FAILED
    public string Status { get; set; } = "PENDING";
    
    public int? TotalItems { get; set; }
    public int Processed { get; set; } = 0;
    public int Failed { get; set; } = 0;
    
    public string? ErrorJsonb { get; set; }
    
    public DateTime? CompletedAt { get; set; }
}
