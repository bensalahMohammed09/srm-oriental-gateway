namespace Srm.Gateway.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public required string Label { get; set; }
    //Navigation properties
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
}
