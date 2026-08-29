using System.Text.Json;

namespace NotesAndFileBackend.Api.DTOs;

public class GenerateCommandRequest
{
    public Dictionary<string, JsonElement> Values { get; set; } = new Dictionary<string, JsonElement>();
}

