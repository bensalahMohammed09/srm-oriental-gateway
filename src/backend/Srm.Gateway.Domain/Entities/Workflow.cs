using Microsoft.AspNetCore.Identity;

namespace Srm.Gateway.Domain.Entities;

public class Workflow : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    public string StepName { get; set; } = string.Empty;

    // 🛡️ Modification pour IdentityRole
    // On utilise 'string' car c'est le type par défaut d'IdentityRole.Id
    public string AssignedRoleId { get; set; } = string.Empty;
    public IdentityRole AssignedRole { get; set; } = null!;

    public string CurrentStatus { get; set; } = string.Empty;
    public string? Comment { get; set; }

    // 🛡️ Modification pour IdentityUser
    public string? ValidatedByUserId { get; set; }
    public IdentityUser? ValidatedByUser { get; set; }

    public DateTime? ValidatedAt { get; set; }
}