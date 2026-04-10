namespace Srm.Gateway.Domain.Entities;

public class OcrMetadata
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public string? rawText { get; set; }
    public double? ConfidenceScore { get; set; }
    public DateOnly? ExtractedDate { get; set; }

    // Navigation
    public virtual Document Document { get; set; } = null!;
}