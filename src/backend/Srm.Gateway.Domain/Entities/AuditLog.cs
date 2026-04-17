namespace Srm.Gateway.Domain.Entities;

public class AuditLog : BaseEntity
{
   public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Changes {  get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}