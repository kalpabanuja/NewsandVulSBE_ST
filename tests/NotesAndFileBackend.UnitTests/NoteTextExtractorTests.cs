using System;
using NotesAndFileBackend.Application.Services;
using Xunit;

namespace NotesAndFileBackend.UnitTests.Services;

public class NoteTextExtractorTests
{
    [Fact]
    public void ExtractText_ShouldParseJsonAndExtractTextValues()
    {
        // Arrange
        var jsonb = @"
        {
            ""version"": 1,
            ""blocks"": [
                {
                    ""type"": ""header"",
                    ""data"": {
                        ""text"": ""My Header Title""
                    }
                },
                {
                    ""type"": ""paragraph"",
                    ""data"": {
                        ""text"": ""This is a paragraph of text.""
                    }
                },
                {
                    ""type"": ""list"",
                    ""data"": {
                        ""items"": [""item1"", ""item2""]
                    }
                }
            ]
        }";

        // Act
        var extracted = NoteTextExtractor.ExtractText(jsonb);

        // Assert
        Assert.Contains("My Header Title", extracted);
        Assert.Contains("This is a paragraph of text.", extracted);
        Assert.Contains("item1", extracted);
        Assert.Contains("item2", extracted);
    }

    [Fact]
    public void ExtractText_ShouldReturnEmptyStringForInvalidJson()
    {
        // Arrange
        var invalidJson = "this is not json";

        // Act
        var extracted = NoteTextExtractor.ExtractText(invalidJson);

        // Assert
        Assert.Equal(invalidJson, extracted);
    }
}
