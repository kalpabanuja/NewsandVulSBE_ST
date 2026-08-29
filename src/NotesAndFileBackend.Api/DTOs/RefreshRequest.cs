using System.ComponentModel.DataAnnotations;

namespace NotesAndFileBackend.Api.DTOs;

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;

    [Required]
    public Guid DeviceId { get; set; }
}
