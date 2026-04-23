using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Srm.Gateway.Application.Interfaces
{
    public interface IAuthService
    {
        /// <summary>
        /// Authenticates a user and returns a JWT token along with their roles.
        /// Returns null if authentication fails.
        /// </summary>
        Task<AuthResult?> LoginAsync(string email, string password);

        /// <summary>
        /// Assigns the ROLE_ADMIN to the specified email.
        /// </summary>
        Task<RoleAssignmentResult> AssignAdminRoleAsync(string email);
    }

    // DTOs to pass data safely between Service and Controller
    public record AuthResult(string Token, IList<string> Roles);
    public record RoleAssignmentResult(bool Success, string Message, IEnumerable<string>? Errors = null);
}
