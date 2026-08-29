using NotesAndFileBackend.Application.Services;
using Xunit;

namespace NotesAndFileBackend.UnitTests;

public class CommandTemplateRendererTests
{
    private readonly CommandTemplateRenderer _renderer;

    public CommandTemplateRendererTests()
    {
        _renderer = new CommandTemplateRenderer();
    }

    [Fact]
    public void Render_WithValidInputs_ReturnsCorrectCommand()
    {
        var template = "nmap {scanType} {ports} {target}";
        var values = new Dictionary<string, string>
        {
            { "scanType", "-sS" },
            { "ports", "-p 80,443" },
            { "target", "192.168.1.10" }
        };

        var result = _renderer.Render(template, values);

        Assert.Equal("nmap -sS -p 80,443 192.168.1.10", result);
    }

    [Fact]
    public void Render_WithUnresolvedPlaceholders_ThrowsException()
    {
        var template = "nmap {scanType} {ports} {target}";
        var values = new Dictionary<string, string>
        {
            { "scanType", "-sS" },
            { "target", "192.168.1.10" }
            // missing {ports}
        };

        var ex = Assert.Throws<InvalidOperationException>(() => _renderer.Render(template, values));
        Assert.Contains("unresolved placeholders", ex.Message);
    }

    [Fact]
    public void Render_WithControlCharactersInOutput_ThrowsException()
    {
        // This simulates a scenario where an unexpected control character sneaks through 
        // into the final rendered string. The renderer performs a final sweep.
        var template = "echo {message}";
        var values = new Dictionary<string, string>
        {
            { "message", "hello; whoami" }
        };

        var ex = Assert.Throws<InvalidOperationException>(() => _renderer.Render(template, values));
        Assert.Contains("forbidden control characters", ex.Message);
    }
}
