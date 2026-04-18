namespace Srm.Gateway.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Changes { get; set; }
    public Guid? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}