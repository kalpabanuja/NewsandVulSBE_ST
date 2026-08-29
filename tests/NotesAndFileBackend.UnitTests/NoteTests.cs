using System;
using NotesAndFileBackend.Domain.Entities;
using Xunit;

namespace NotesAndFileBackend.UnitTests.Domain.Entities;

public class NoteTests
{
    [Fact]
    public void Note_Should_Initialize_With_Defaults()
    {
        // Act
        var note = new Note();

        // Assert
        Assert.Equal("ACTIVE", note.Status);
        Assert.Equal("PRIVATE", note.Visibility);
        Assert.Equal("{\"version\": 1, \"blocks\": []}", note.ContentJsonb);
        Assert.False(note.IsArchived);
        Assert.False(note.IsPinned);
        Assert.False(note.IsFavorite);
        Assert.Empty(note.Attachments);
        Assert.Empty(note.PublicShares);
    }
}
