namespace Srm.Gateway.Domain.Entities;


public class Status
{
    public int Id { get; set; }
    public required string Label { get; set; }
    //Navigation properties
    public virtual ICollection<Workflow> Documents { get; set; } = new List<Workflow>();
}