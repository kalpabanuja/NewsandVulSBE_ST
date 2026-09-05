namespace NotesAndFileBackend.Api.DTOs;

public class CreateShareRequest
{
    public string Alias { get; set; } = string.Empty;
    public int? ExpiresInHours { get; set; }
    public string? Password { get; set; }
    public bool AllowIndexing { get; set; } = false;
    public int? MaxViews { get; set; }
}

public class ShareResponseDto
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}

public class SharedNoteDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public System.Text.Json.JsonElement ContentJsonb { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateFileVisibilityRequest
{
    public bool ShareWithEveryone { get; set; }
}
