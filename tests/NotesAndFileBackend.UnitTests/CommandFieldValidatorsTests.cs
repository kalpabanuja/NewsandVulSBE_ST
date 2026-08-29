using NotesAndFileBackend.Application.Services;
using Xunit;

namespace NotesAndFileBackend.UnitTests;

public class CommandFieldValidatorsTests
{
    [Theory]
    [InlineData("192.168.1.10")]
    [InlineData("2001:db8::1")]
    [InlineData("10.0.0.0/8")]
    [InlineData("scan.example.com")]
    [InlineData("localhost")]
    public void IsValidTarget_WithValidInputs_ReturnsTrue(string target)
    {
        var isValid = CommandFieldValidators.IsValidTarget(target);
        Assert.True(isValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("192.168.1.10; rm -rf /")]
    [InlineData("192.168.1.10 && whoami")]
    [InlineData("192.168.1.10 || whoami")]
    [InlineData("192.168.1.10 > output.txt")]
    [InlineData("`whoami`.example.com")]
    [InlineData("$(whoami)")]
    public void IsValidTarget_WithInvalidInputsOrInjections_ReturnsFalse(string target)
    {
        var isValid = CommandFieldValidators.IsValidTarget(target);
        Assert.False(isValid);
    }

    [Theory]
    [InlineData("test;echo 1")]
    [InlineData("foo&bar")]
    [InlineData("foo|bar")]
    [InlineData("foo>bar")]
    [InlineData("foo<bar")]
    [InlineData("foo`bar")]
    [InlineData("foo$bar")]
    [InlineData("foo\nbar")]
    public void ContainsControlCharacters_WithMaliciousInput_ReturnsTrue(string input)
    {
        var result = CommandFieldValidators.ContainsControlCharacters(input);
        Assert.True(result);
    }
}
