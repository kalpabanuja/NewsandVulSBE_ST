namespace NotesAndFileBackend.Api.DTOs;

public class CreateShareRequest
{
    public int? ExpiresInHours { get; set; }
    
    // If provided, creates a link like {Alias}_{RandomNumber}. Otherwise, a secure 32-char string is generated.
    public string? Alias { get; set; } 
}

public class ShareResponseDto
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}
