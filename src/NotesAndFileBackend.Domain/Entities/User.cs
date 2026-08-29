namespace NotesAndFileBackend.Domain.Entities;

public class User : BaseEntity
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Status { get; set; } = "ACTIVE";
    public bool IsActive { get; set; } = true;
    public bool IsAdmin { get; set; } = false;
    public long RowVersion { get; set; } = 0;
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public ICollection<Device> Devices { get; set; } = new List<Device>();
}
