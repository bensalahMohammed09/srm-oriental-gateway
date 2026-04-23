using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Srm.Gateway.Application.Interfaces;
using System.Security.Claims;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Srm.Gateway.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        // 🚀 DÉCOUPLAGE TOTAL : Le contrôleur ne connaît que le Service !
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // 1. On demande au service de faire tout le travail lourd (Vérification + Création JWT)
            var result = await _authService.LoginAsync(request.Email, request.Password);

            if (result != null)
            {
                // 2. 🛡️ CONFIGURATION SRE NGINX .NET 9
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,    // S'adapte dynamiquement
                    SameSite = SameSiteMode.Lax, // Nginx permet d'utiliser ce standard sécurisé
                    Path = "/",                  // CRITIQUE : Autorise React à envoyer le cookie sur /profile/me
                    Expires = DateTime.UtcNow.AddHours(3)
                };

                Response.Cookies.Append("SRM_AUTH_TOKEN", result.Token, cookieOptions);

                return Ok(new { message = "Connexion réussie", roles = result.Roles });
            }

            return Unauthorized("Identifiants invalides.");
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("SRM_AUTH_TOKEN", new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/" // Doit correspondre exactement à la création
            });
            return Ok(new { message = "Déconnexion réussie" });
        }

        [HttpGet("debug-token")]
        [AllowAnonymous]
        public IActionResult DebugToken()
        {
            var token = Request.Cookies["SRM_AUTH_TOKEN"];

            if (string.IsNullOrEmpty(token))
                return Ok(new { received = false, message = "No cookie found" });

            try
            {
                // 🚀 Lecture avec le nouveau moteur .NET 9
                var handler = new JsonWebTokenHandler();
                var jwt = handler.ReadToken(token) as JsonWebToken;

                return Ok(new
                {
                    received = true,
                    issuer = jwt?.Issuer,
                    audience = jwt?.Audiences,
                    expires = jwt?.ValidTo,
                    notBefore = jwt?.ValidFrom,
                    claims = jwt?.Claims.Select(c => new { c.Type, c.Value }),
                    nowUtc = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return Ok(new { received = true, parseError = ex.Message });
            }
        }

        [HttpPost("fix-admin-role")]
        [AllowAnonymous]
        public async Task<IActionResult> FixAdminRole()
        {
            // Délégation totale au service !
            var result = await _authService.AssignAdminRoleAsync("admin@srm.ma");

            if (result.Success)
                return Ok(new { message = result.Message });

            if (result.Message == "User not found")
                return NotFound(result.Message);

            return BadRequest(new { errors = result.Errors });
        }
    }

    public record LoginRequest(string Email, string Password);
}