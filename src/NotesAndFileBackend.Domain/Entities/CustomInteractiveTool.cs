using System;

namespace NotesAndFileBackend.Domain.Entities;

public class CustomInteractiveTool : BaseEntity
{
    public Guid NoteId { get; set; }
    public Note Note { get; set; } = null!;

    public Guid OwnerUserId { get; set; }
    public User OwnerUser { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public string HtmlSource { get; set; } = string.Empty;
    public string CssSource { get; set; } = string.Empty;
    public string JavascriptSource { get; set; } = string.Empty;

    public string ContentHash { get; set; } = string.Empty;
    public int SchemaVersion { get; set; } = 1;
    public int AssetVersion { get; set; } = 1;

    public bool IsEnabled { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    public string ValidationStatus { get; set; } = "Pending"; // Pending, Valid, Invalid, Rejected
    public string SecurityStatus { get; set; } = "Unreviewed"; // Unreviewed, Approved, Rejected

    public DateTime? DeletedAt { get; set; }

    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public Guid UpdatedByUserId { get; set; }
    public User UpdatedByUser { get; set; } = null!;
}
