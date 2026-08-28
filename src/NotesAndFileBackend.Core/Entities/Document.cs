namespace NotesAndFileBackend.Core.Entities;

public class Document : BaseEntity
{
    public Guid OwnerUserId { get; set; }
    public User OwnerUser { get; set; } = null!;

    public Guid? OwnerDeviceId { get; set; }
    public Device? OwnerDevice { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public string Status { get; set; } = "ACTIVE"; // ACTIVE, DELETED
    public string Visibility { get; set; } = "PRIVATE"; // PRIVATE, SHARED_WITH_ALL

    public int Revision { get; set; } = 1;
    public DateTime? DeletedAt { get; set; }

    public ICollection<DocumentBlock> Blocks { get; set; } = new List<DocumentBlock>();
    public ICollection<DocumentAttachment> Attachments { get; set; } = new List<DocumentAttachment>();
    public ICollection<PublicDocumentShare> PublicShares { get; set; } = new List<PublicDocumentShare>();
}
