using System;
using System.Collections.Generic;

namespace NotesAndFileBackend.Domain.Entities;

public class Note : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid? DeviceId { get; set; }
    public Device? Device { get; set; }

    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ToolName { get; set; }

    public string ContentJsonb { get; set; } = "{\"version\": 1, \"blocks\": []}";
    public string SearchText { get; set; } = string.Empty;
    
    public string Status { get; set; } = "ACTIVE"; // Keep existing as fallback or auxiliary
    public bool IsDeleted { get; set; } = false;
    public string Visibility { get; set; } = "PRIVATE"; // PRIVATE, SHARED_WITH_ALL

    public bool IsArchived { get; set; } = false;
    public bool IsPinned { get; set; } = false;
    public bool IsFavorite { get; set; } = false;
    
    public ICollection<NoteTag> NoteTags { get; set; } = new List<NoteTag>();

    public int Version { get; set; } = 1;
    public DateTime? DeletedAt { get; set; }
    
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    
    public Guid UpdatedByUserId { get; set; }
    public User UpdatedByUser { get; set; } = null!;

    public ICollection<NoteAttachment> Attachments { get; set; } = new List<NoteAttachment>();
    public ICollection<PublicNoteShare> PublicShares { get; set; } = new List<PublicNoteShare>();
    public ICollection<NoteRevision> Revisions { get; set; } = new List<NoteRevision>();
    public ICollection<NoteLink> Links { get; set; } = new List<NoteLink>();
}
