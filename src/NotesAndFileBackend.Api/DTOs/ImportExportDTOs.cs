using System.Text.Json;

namespace NotesAndFileBackend.Api.DTOs;

public class ExportRequestDto
{
    public string Format { get; set; } = "json";
    public List<Guid>? NoteIds { get; set; }
    public bool IncludeRevisions { get; set; } = false;
}

public class NoteExportFormat
{
    public string Format { get; set; } = "notes";
    public int Version { get; set; } = 1;
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public List<ExportedNote> Notes { get; set; } = new List<ExportedNote>();
}

public class ExportedNote
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public JsonElement ContentJsonb { get; set; }
    public List<string> Tags { get; set; } = new List<string>();
    public string? Category { get; set; }
    public bool IsPinned { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ImportRequestDto
{
    public NoteExportFormat Payload { get; set; } = null!;
}

public class ImportResultDto
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Processed { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
}
