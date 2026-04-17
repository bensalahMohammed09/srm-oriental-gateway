namespace Srm.Gateway.Domain.Entities;

public class OcrMetadata : BaseEntity
{
   public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public double Confidence { get; set; }
}