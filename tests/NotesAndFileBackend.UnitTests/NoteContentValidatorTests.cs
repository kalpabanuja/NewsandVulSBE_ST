using NotesAndFileBackend.Application.Services;
using Xunit;

namespace NotesAndFileBackend.UnitTests;

public class NoteContentValidatorTests
{
    [Fact]
    public void Validate_ValidBlocks_ReturnsNoErrors()
    {
        var json = @"
        {
            ""version"": 2,
            ""blocks"": [
                { ""id"": ""b1"", ""type"": ""heading"", ""level"": 1, ""text"": ""Hello"" },
                { ""id"": ""b2"", ""type"": ""paragraph"", ""text"": ""World"" },
                { ""id"": ""b3"", ""type"": ""bulletList"", ""style"": ""disc"", ""items"": [{ ""text"": ""item 1"" }] },
                { ""id"": ""b4"", ""type"": ""numberedList"", ""items"": [{ ""text"": ""item 2"" }] },
                { ""id"": ""b5"", ""type"": ""checkList"", ""items"": [{ ""text"": ""item 3"", ""checked"": true }] },
                { ""id"": ""b6"", ""type"": ""divider"", ""style"": ""singleLine"" },
                { ""id"": ""b7"", ""type"": ""link"", ""url"": ""https://google.com"", ""text"": ""Google"" },
                { ""id"": ""b8"", ""type"": ""code"", ""code"": ""console.log('hi');"", ""language"": ""javascript"", ""ui"": { ""backgroundColor"": ""#333333"" } },
                { ""id"": ""b9"", ""type"": ""displayAttachment"", ""attachmentId"": ""att-1"" },
                { ""id"": ""b10"", ""type"": ""downloadAttachment"", ""attachmentId"": ""att-2"" },
                { ""id"": ""b11"", ""type"": ""commandGenerator"", ""generatorId"": ""gen-1"" }
            ]
        }";

        var errors = NoteContentValidator.Validate(json);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_InvalidBlockType_ReturnsError()
    {
        var json = @"
        {
            ""version"": 2,
            ""blocks"": [
                { ""id"": ""b1"", ""type"": ""unknownType"" }
            ]
        }";

        var errors = NoteContentValidator.Validate(json);

        Assert.Contains(errors, e => e.Field == "content.blocks[0].type" && e.Code == "unsupported_block_type");
    }

    [Fact]
    public void Validate_DangerousUrl_ReturnsError()
    {
        var json = @"
        {
            ""version"": 2,
            ""blocks"": [
                { ""id"": ""b1"", ""type"": ""link"", ""url"": ""javascript:alert(1)"" }
            ]
        }";

        var errors = NoteContentValidator.Validate(json);

        Assert.Contains(errors, e => e.Field == "content.blocks[0].url" && e.Code == "dangerous_url_scheme");
    }
}
