using NotesAndFileBackend.Application.Services;
using Xunit;

namespace NotesAndFileBackend.UnitTests;

public class JintCommandGeneratorServiceTests
{
    [Fact]
    public void Execute_ValidScript_ReturnsSuccess()
    {
        var script = @"
            var name = inputs.name || 'world';
            return 'echo hello ' + name;
        ";
        var inputs = new Dictionary<string, string> { { "name", "Alice" } };

        var result = JintCommandGeneratorService.Execute(script, inputs);

        Assert.True(result.Success);
        Assert.Equal("echo hello Alice", result.Output);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Execute_InfiniteLoop_ReturnsErrorDueToStatementLimit()
    {
        var script = @"
            while(true) {}
            return 'done';
        ";

        var result = JintCommandGeneratorService.Execute(script, new Dictionary<string, string>());

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("resource limits"));
    }

    [Fact]
    public void Execute_InvalidSyntax_ReturnsError()
    {
        var script = "return 'test'"; // Missing function wrapping or just plain syntax error if not formatted right, but this is actually valid in Jint. Let's make a real syntax error.
        
        var (isValid, error) = JintCommandGeneratorService.ValidateSyntax("this is not valid js!!");
        
        Assert.False(isValid);
        Assert.NotNull(error);
    }
}
