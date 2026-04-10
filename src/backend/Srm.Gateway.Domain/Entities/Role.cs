namespace Srm.Gateway.Domain.Entities;

public class Role
{
	public int Id { get; set; }
	public required string Label { get; set; }

	//Navigation properties
	public virtual ICollection<User> Users { get; set; } = new List<User>();
}