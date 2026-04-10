namespace Srm.Gateway.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public string? EventType { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}