using System.Text.Json;
using System.Text.RegularExpressions;

namespace NotesAndFileBackend.Application.Services;

public static class CommandFieldValidators
{
    private static readonly Regex ShellControlChars = new Regex(@"[;&|><`$()\n\r]", RegexOptions.Compiled);

    // Basic regex for IPv4, simple Hostname, or CIDR (e.g., 192.168.1.1, 192.168.1.0/24, scan.example.com)
    private static readonly Regex TargetRegex = new Regex(@"^[a-zA-Z0-9\.\-:\/]+$", RegexOptions.Compiled);

    public static bool ContainsControlCharacters(string input)
    {
        return ShellControlChars.IsMatch(input);
    }

    public static bool IsValidTarget(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        
        // Deny any shell control chars just in case
        if (ContainsControlCharacters(input)) return false;

        // Ensure it only contains alphanumeric, dots, dashes, colons (ipv6) and slashes (cidr)
        return TargetRegex.IsMatch(input);
    }

    public static (bool isValid, string? renderedPortArg, string? error) ValidatePortSelector(JsonElement portJson)
    {
        if (portJson.ValueKind != JsonValueKind.Object)
            return (false, null, "Port selector must be an object");

        if (!portJson.TryGetProperty("mode", out var modeProp) || modeProp.ValueKind != JsonValueKind.String)
            return (false, null, "Port selector missing or invalid 'mode'");

        var mode = modeProp.GetString()?.ToLowerInvariant();

        switch (mode)
        {
            case "all":
                return (true, "-p-", null);
            case "common":
                // Instead of hardcoding, we just output top ports. In a real app this would resolve a Preset.
                return (true, "-F", null); // Example: nmap fast scan for common
            case "list":
                if (!portJson.TryGetProperty("ports", out var portsProp) || portsProp.ValueKind != JsonValueKind.Array)
                    return (false, null, "Mode 'list' requires a 'ports' array");
                
                var portList = new List<int>();
                foreach (var p in portsProp.EnumerateArray())
                {
                    if (p.ValueKind != JsonValueKind.Number || !p.TryGetInt32(out int portNum) || portNum < 1 || portNum > 65535)
                        return (false, null, "Invalid port number in list");
                    portList.Add(portNum);
                }
                return (true, $"-p {string.Join(",", portList)}", null);

            case "range":
                if (!portJson.TryGetProperty("from", out var fromProp) || !fromProp.TryGetInt32(out int from) || from < 1 || from > 65535)
                    return (false, null, "Invalid 'from' port");
                if (!portJson.TryGetProperty("to", out var toProp) || !toProp.TryGetInt32(out int to) || to < 1 || to > 65535)
                    return (false, null, "Invalid 'to' port");
                
                if (from > to) return (false, null, "'from' port must be <= 'to' port");

                return (true, $"-p {from}-{to}", null);

            default:
                return (false, null, $"Unknown mode '{mode}'");
        }
    }
}
