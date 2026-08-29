using System.Text;
using System.Text.Json;

namespace NotesAndFileBackend.Application.Services;

public static class NoteTextExtractor
{
    public static string ExtractText(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            return string.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var sb = new StringBuilder();
            ExtractTextRecursive(doc.RootElement, sb);
            return sb.ToString().Trim();
        }
        catch (JsonException)
        {
            // If it's invalid JSON, fallback to returning the raw string or empty.
            return jsonContent;
        }
    }

    private static void ExtractTextRecursive(JsonElement element, StringBuilder sb)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                var text = element.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.Append(text).Append(" ");
                }
                break;

            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    // Optionally skip keys that represent non-searchable meta-data (e.g. "id", "type")
                    if (property.Name.Equals("id", StringComparison.OrdinalIgnoreCase) ||
                        property.Name.Equals("blockId", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    ExtractTextRecursive(property.Value, sb);
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    ExtractTextRecursive(item, sb);
                }
                break;
                
            // Ignore Numbers, Booleans, Nulls for full-text search extraction
        }
    }
}
