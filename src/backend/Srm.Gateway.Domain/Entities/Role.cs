namespace Srm.Gateway.Domain.Entities;

public class Role : BaseEntity
{
     public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public ICollection<User> Users { get; set; } = new List<User>();
}