using System;
using System.Text.Json;
using NotesAndFileBackend.Application.Services;

class Program {
    static void Main() {
        var payload = "{\"title\":\"kalpa\",\"summary\":\"\",\"tags\":[],\"categoryId\":null,\"toolName\":null,\"isPinned\":false,\"isFavorite\":false,\"isArchived\":false,\"content\":{\"version\":2,\"blocks\":[{\"id\":\"a1405ef8-ccab-422d-917d-db830ecc4f01\",\"type\":\"heading\",\"level\":4,\"text\":\"Heading 4\",\"content\":\"Heading 4\"}]},\"shareWithEveryone\":false}";

        Console.WriteLine("Parsing JSON...");
        var request = JsonSerializer.Deserialize<JsonElement>(payload);
        
        var contentJson = request.GetProperty("content").GetRawText();
        Console.WriteLine("Validating Content...");
        var errors = NoteContentValidator.Validate(contentJson);
        foreach(var e in errors) Console.WriteLine($"Error: {e.Field} - {e.Message}");

        Console.WriteLine("Extracting text...");
        var searchText = NoteTextExtractor.ExtractText(contentJson);
        Console.WriteLine($"Search text: {searchText}");
        Console.WriteLine("Done.");
    }
}
