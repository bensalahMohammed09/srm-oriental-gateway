namespace Srm.Gateway.Domain.Entities;

public class User
{
	
		public int Id { get; set; }
		public required string FirstName { get; set; }
		public required string LastName { get; set; }
		public required string Email { get; set; }
		public required string PasswordHash { get; set; }

       //Navigation properties
	   public int RoleId { get; set; }
	  public virtual Role Role { get; set; }
	  public virtual ICollection<Workflow> Workflows { get; set; } = new List<Workflow>();

}
