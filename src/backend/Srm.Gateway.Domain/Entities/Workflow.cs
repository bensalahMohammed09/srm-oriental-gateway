namespace Srm.Gateway.Domain.Entities;

public class Workflow
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public int UserId { get; set; }
    public string? ActionTaken { get; set; }
    public DateTime ActionDate { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual Document Document { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}