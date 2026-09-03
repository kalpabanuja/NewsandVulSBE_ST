using System.Text.Json;

namespace NotesAndFileBackend.Api.DTOs;

public class CreateNoteRequest
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public List<string> Tags { get; set; } = new List<string>();
    public string? ToolName { get; set; }
    public JsonElement Content { get; set; }
    public bool IsPinned { get; set; }
    public bool IsFavorite { get; set; }
    /// <summary>PRIVATE or PUBLIC</summary>
    public string Visibility { get; set; } = "PRIVATE";
}

public class UpdateNoteRequest
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public List<string> Tags { get; set; } = new List<string>();
    public string? ToolName { get; set; }
    public JsonElement Content { get; set; }
    public bool IsPinned { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsArchived { get; set; }
    public int Version { get; set; }
    /// <summary>PRIVATE or PUBLIC</summary>
    public string Visibility { get; set; } = "PRIVATE";
}

public class NoteBlockDto
{
    public string BlockType { get; set; } = string.Empty;
    public int Position { get; set; }
    public JsonElement ContentJson { get; set; }
}

public class NoteSearchResponse
{
    public List<NoteSearchItem> Items { get; set; } = new List<NoteSearchItem>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
}

public class NoteSearchItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public string? Category { get; set; }
    public List<string> Tags { get; set; } = new List<string>();
    public string? ToolName { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsPinned { get; set; }
    public bool IsArchived { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string Visibility { get; set; } = "PRIVATE";
    public List<NoteBlockMatch> MatchedBlocks { get; set; } = new List<NoteBlockMatch>();
}

public class NoteBlockMatch
{
    public string BlockId { get; set; } = string.Empty;
    public string BlockType { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
}


