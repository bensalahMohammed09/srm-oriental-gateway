namespace Srm.Gateway.Domain.Entities;

public class Workflow : BaseEntity
{
   public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public string StepName { get; set; } = string.Empty;
    public Guid AssignedRoleId { get; set; }
    public Role AssignedRole { get; set; } = null!;

    public string CurrentStatus { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public Guid? ValidatedByUserId { get; set; }
    public User? ValidatedbyUser { get; set; }
    public DateTime? ValidatedAt { get; set; }
       
}