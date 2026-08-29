using System;
using NotesAndFileBackend.Api.Helpers;
using Xunit;

namespace NotesAndFileBackend.UnitTests.Helpers;

public class TokenHelperTests
{
    [Fact]
    public void GenerateToken_WithNoAlias_ShouldReturnRandom32CharString()
    {
        // Act
        var token = TokenHelper.GenerateToken(null);
        var token2 = TokenHelper.GenerateToken("");

        // Assert
        Assert.NotNull(token);
        Assert.Equal(32, token.Length);
        
        Assert.NotNull(token2);
        Assert.Equal(32, token2.Length);

        Assert.NotEqual(token, token2);
    }

    [Fact]
    public void GenerateToken_WithAlias_ShouldReturnAliasWithRandomSuffix()
    {
        // Arrange
        var alias = "my-custom-alias";

        // Act
        var token = TokenHelper.GenerateToken(alias);

        // Assert
        Assert.NotNull(token);
        Assert.StartsWith(alias + "_", token);
        Assert.True(token.Length > alias.Length + 1);
    }
}
