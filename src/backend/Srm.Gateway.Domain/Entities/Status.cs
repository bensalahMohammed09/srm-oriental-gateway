namespace Srm.Gateway.Domain.Entities;


public class Status : BaseEntity
{
   public string Name { get; set; } = string.Empty;
   public string Code { get; set; } = string.Empty;

   public ICollection<Document> Documents { get; set; } = new List<Document>();
}