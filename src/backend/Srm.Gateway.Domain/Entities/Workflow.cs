namespace Srm.Gateway.Domain.Entities;

public class Workflow : BaseEntity
{
   public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public string StepName { get; set; } = string.Empty;
    public string RequiredRole { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = "Pending";
    public string? Comement;
    public Guid? ValidatedBy { get; set; }
    public DateTime? ValidateAt { get; set; }
       
}