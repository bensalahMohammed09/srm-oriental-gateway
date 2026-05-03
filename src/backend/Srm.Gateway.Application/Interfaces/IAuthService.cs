using System.Collections.Generic;
using System.Threading.Tasks;

namespace Srm.Gateway.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResult?> LoginAsync(string email, string password);
    Task<RoleAssignmentResult> AssignAdminRoleAsync(string email);
}

// 🌟 Result DTOs specifically for Authentication
public record AuthResult(
    string Token,
    IList<string> Roles
);

public record RoleAssignmentResult(
    bool Success,
    string Message,
    IEnumerable<string>? Errors = null
);