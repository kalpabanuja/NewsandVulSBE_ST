using System.Text.Json;

namespace NewsAndVulBackend.Api.DTOs;

public class CreateDocumentRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class UpdateDocumentRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Revision { get; set; }
}

public class DocumentBlockDto
{
    public string BlockType { get; set; } = string.Empty;
    public int Position { get; set; }
    public JsonElement ContentJson { get; set; }
}
