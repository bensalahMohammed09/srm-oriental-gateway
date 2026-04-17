namespace Srm.Gateway.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ICollection<Document> Documents { get; set; } = new List<Document>();

}
