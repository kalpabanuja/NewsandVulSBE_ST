using System.Text.RegularExpressions;

namespace NotesAndFileBackend.Application.Services;

public class CommandTemplateRenderer
{
    public string Render(string template, IReadOnlyDictionary<string, string> values)
    {
        var result = template;

        foreach (var pair in values)
        {
            result = result.Replace("{" + pair.Key + "}", pair.Value, StringComparison.Ordinal);
        }

        // Verify no unresolved placeholders remain
        if (Regex.IsMatch(result, @"\{[^{}]+\}"))
        {
            throw new InvalidOperationException("Template contains unresolved placeholders.");
        }

        // Final safety check across the entire rendered string
        if (CommandFieldValidators.ContainsControlCharacters(result))
        {
            throw new InvalidOperationException("Generated command contains forbidden control characters.");
        }

        return result;
    }
}
