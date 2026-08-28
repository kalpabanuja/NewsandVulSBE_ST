namespace NewsAndVulBackend.Core.Entities;

public class AuditEvent : BaseEntity
{
    public Guid? UserId { get; set; }
    public Guid? DeviceId { get; set; }

    public string EventType { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    
    public string MetadataJson { get; set; } = "{}";
    
    public string IpHash { get; set; } = string.Empty;
}
