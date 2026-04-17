namespace Srm.Gateway.Domain.Entities;

public class Document : BaseEntity
{
   public string Reference { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;   
    public decimal? TotalAmount { get; set; }

    public Guid StatusId { get; set; }
    public Status Status { get; set; } = null!;

    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    public ICollection<OcrMetadata> Metadata { get; set; } = new List<OcrMetadata>();
    public ICollection<Workflow> Workflows { get; set; } = new List<Workflow>();
}