using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Srm.Gateway.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Srm.Gateway.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        public AuthService(UserManager<IdentityUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<AuthResult?> LoginAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, password))
            {
                return null; // Invalid credentials
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var token = CreateToken(authClaims);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return new AuthResult(tokenString, userRoles);
        }

        public async Task<RoleAssignmentResult> AssignAdminRoleAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return new RoleAssignmentResult(false, "User not found");

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Contains("ROLE_ADMIN"))
                return new RoleAssignmentResult(true, "Already has ROLE_ADMIN");

            var result = await _userManager.AddToRoleAsync(user, "ROLE_ADMIN");

            if (result.Succeeded)
                return new RoleAssignmentResult(true, "ROLE_ADMIN assigned successfully");

            return new RoleAssignmentResult(false, "Failed to assign role", result.Errors.Select(e => e.Description));
        }

        private JwtSecurityToken CreateToken(List<Claim> authClaims)
        {
            var secret = _configuration["JwtSettings:Secret"]
                ?? "SRM_ORIENTAL_SUPER_SECRET_KEY_2026_DO_NOT_SHARE_BY_MOHAMMED";
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

            return new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"] ?? "srm-gateway",
                audience: _configuration["JwtSettings:Audience"] ?? "srm-frontend",
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );
        }
    }
}
