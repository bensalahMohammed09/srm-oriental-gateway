using Microsoft.AspNetCore.Identity;

namespace Srm.Gateway.Domain.Entities;

public class Workflow : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    public string StepName { get; set; } = string.Empty;

    // 🛡️ Modification pour IdentityRole
    // On utilise 'string' car c'est le type par défaut d'IdentityRole.Id
    public Guid? AssignedRoleId { get; set; }
    public IdentityRole<Guid>? AssignedRole { get; set; }

    public string CurrentStatus { get; set; } = string.Empty;
    public string? Comment { get; set; }

    // 🛡️ Modification pour IdentityUser
    public Guid? ValidatedByUserId { get; set; }
    public IdentityUser<Guid>? ValidatedByUser { get; set; }

    public DateTime? ValidatedAt { get; set; }
}