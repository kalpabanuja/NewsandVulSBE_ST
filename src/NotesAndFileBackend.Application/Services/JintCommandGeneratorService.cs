using Jint;
using Jint.Runtime;

namespace NotesAndFileBackend.Application.Services;

/// <summary>
/// Executes JavaScript command-generator scripts using the Jint engine (v3.x).
///
/// Security model:
///  - Fresh isolated Engine per request (stateless, no shared state between requests).
///  - Strict execution timeout (default 2 seconds) — prevents runaway scripts.
///  - Maximum statement and recursion limits applied.
///  - Only a plain 'inputs' object is injected — no .NET objects, DI services,
///    database contexts, file-system APIs, process APIs, or secrets are exposed.
///  - The generated string result is NEVER executed by the OS shell.
/// </summary>
public static class JintCommandGeneratorService
{
    private const int DefaultTimeoutMs = 2000;
    private const int MaxStatements = 10_000;
    private const int MaxRecursionDepth = 64;

    public record GenerationResult(bool Success, string? Output, IReadOnlyList<string> Errors);

    /// <summary>
    /// Execute a JavaScript command-generator script with validated plain-data inputs.
    /// </summary>
    public static GenerationResult Execute(string script, Dictionary<string, string> inputs, int timeoutMs = DefaultTimeoutMs)
    {
        if (string.IsNullOrWhiteSpace(script))
            return new GenerationResult(false, null, new[] { "Generator script is empty." });

        try
        {
            // Create a fresh, isolated engine for every request.
            // Do NOT cache or share the Engine instance between requests.
            var engine = new Engine(options =>
            {
                // Hard execution timeout
                options.TimeoutInterval(TimeSpan.FromMilliseconds(timeoutMs));
                // Bound statement count — prevents infinite loops that bypass timeout
                options.MaxStatements(MaxStatements);
                // Recursion depth guard
                options.LimitRecursion(MaxRecursionDepth);
                // Strict mode — prevents accidental global variable leakage
                options.Strict();
            });

            // Inject ONLY a plain inputs object (string key → string value).
            // Jint 3.x: use SetValue with a CLR Dictionary — Jint maps it to a plain JS object.
            // No .NET object graphs, reflection handles, or service references are exposed.
            engine.SetValue("inputs", inputs);

            // Execute the script and capture the return value.
            var result = engine.Evaluate(script);

            // Ensure result is a usable string
            if (result == null || result.IsUndefined() || result.IsNull())
                return new GenerationResult(false, null, new[] { "Generator must return a non-empty string." });

            var output = result.ToString();
            if (string.IsNullOrWhiteSpace(output))
                return new GenerationResult(false, null, new[] { "Generator returned an empty command." });

            return new GenerationResult(true, output.Trim(), Array.Empty<string>());
        }
        catch (TimeoutException)
        {
            return new GenerationResult(false, null, new[] { "Generator timed out. Ensure the script completes within the time limit." });
        }
        catch (JavaScriptException jsEx)
        {
            // Never leak internal paths or stack frames — return only the JS error message
            return new GenerationResult(false, null, new[] { $"Generator runtime error: {jsEx.Error}" });
        }
        catch (Exception ex) when (ex is ExecutionCanceledException)
        {
            return new GenerationResult(false, null, new[] { "Generator exceeded resource limits. Simplify the script." });
        }
        catch (Exception ex) when (ex.Message.Contains("statement") || ex.Message.Contains("recursion"))
        {
            return new GenerationResult(false, null, new[] { "Generator exceeded resource limits. Simplify the script." });
        }
        catch (Exception ex)
        {
            // Generic catch — do not expose internal file paths or stack details
            return new GenerationResult(false, null, new[] { $"Generator error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Validates that a script is syntactically parseable without executing it.
    /// Use for pre-save validation of generator definitions.
    /// </summary>
    public static (bool IsValid, string? Error) ValidateSyntax(string script)
    {
        if (string.IsNullOrWhiteSpace(script))
            return (false, "Script is empty.");

        try
        {
            // Use Jint's own parser to validate syntax only
            var parser = new Esprima.JavaScriptParser();
            parser.ParseScript(script); // throws ParserException on syntax error
            return (true, null);
        }
        catch (Esprima.ParserException ex)
        {
            return (false, $"Syntax error: {ex.Message}");
        }
        catch
        {
            return (false, "Could not parse script.");
        }
    }
}
