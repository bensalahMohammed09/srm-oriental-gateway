namespace Srm.Gateway.Domain.Entities;

public class Document
{
    public int Id { get; set; }
    public required string ReferenceNumber { get; set; }
    public int CategoryId { get; set; }
    public int StatusId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public virtual Category Category { get; set; } = null!;
    public virtual Status Status { get; set; } = null!;
    public virtual ICollection<Workflow> Workflows { get; set; } = new List<Workflow>();
    public virtual OcrMetadata? OcrMetadata { get; set; }
}