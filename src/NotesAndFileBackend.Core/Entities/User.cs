namespace NotesAndFileBackend.Core.Entities;

public class User : BaseEntity
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Status { get; set; } = "ACTIVE";
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public ICollection<Device> Devices { get; set; } = new List<Device>();
}
