using FluentAssertions;
// Tes namespaces
using Srm.Gateway.Api.Controllers;
using Srm.Gateway.IntegrationTests.Shared;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Srm.Gateway.IntegrationTests.Controllers;

public class AuthControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(TestWebApplicationFactory factory)
    {
        // Pour ce test, on a besoin d'un client totalement "vierge" sans faux jeton.
        // On veut tester la VRAIE connexion avec le VRAI UserManager.
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ShouldReturn200Ok_AndSetCookie_WhenCredentialsAreValid()
    {
        // Arrange : On utilise les identifiants créés par ton IdentitySeeder.cs au démarrage !
        var request = new LoginRequest("admin@srm.ma", "Srm_Admin_2026!");

        // Act : Tentative de connexion
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert : La connexion doit réussir
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 🛡️ VÉRIFICATION CRITIQUE : Le serveur doit avoir envoyé le cookie HttpOnly
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();
        setCookieHeaders.Should().Contain(cookie => cookie.StartsWith("SRM_AUTH_TOKEN="));
        setCookieHeaders.Should().Contain(cookie => cookie.Contains("httponly"));
    }

    [Fact]
    public async Task Login_ShouldReturn401Unauthorized_WhenPasswordIsWrong()
    {
        // Arrange : Un mot de passe invalide
        var request = new LoginRequest("admin@srm.ma", "MauvaisMotDePasse123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert : Rejet immédiat
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ShouldReturn200Ok_AndClearCookie()
    {
        // Act : Appel à la déconnexion
        var response = await _client.PostAsync("/api/v1/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Vérifie que le serveur ordonne au navigateur d'écraser/supprimer le cookie
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();
        setCookieHeaders.Should().Contain(cookie => cookie.StartsWith("SRM_AUTH_TOKEN=") && cookie.Contains("expires="));
    }
}