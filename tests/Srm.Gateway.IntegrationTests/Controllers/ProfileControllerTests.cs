using FluentAssertions;
using Srm.Gateway.IntegrationTests.Shared;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;

namespace Srm.Gateway.IntegrationTests.Controllers;

public class ProfileControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _clientWithBoRole;
    private readonly HttpClient _anonymousClient;

    public ProfileControllerTests(TestWebApplicationFactory factory)
    {
        // 1. Client avec l'identité de l'agent BO (TestScheme)
        _clientWithBoRole = factory.CreateClient();
        _clientWithBoRole.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestScheme");

        // 2. Client fantôme (Aucun token de connexion)
        _anonymousClient = factory.CreateClient();
    }

    [Fact]
    public async Task GetCurrentProfile_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        // Act : Appel sans aucun token (Anonymous)
        var response = await _anonymousClient.GetAsync("/api/v1/profile/me");

        // Assert : Le pare-feu de l'API doit bloquer la requête immédiatement
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDashboardStats_ShouldReturn403Forbidden_WhenUserLacksRequiredRole()
    {
        // Rappel : Notre faux token (dans la Factory) a le rôle "ROLE_BO".
        // Or, le contrôleur exige [Authorize(Roles = "ROLE_ADMIN,ROLE_FINANCE")]

        // Act : L'agent BO essaie d'accéder aux statistiques financières
        var response = await _clientWithBoRole.GetAsync("/api/v1/profile/stats");

        // Assert : Le middleware d'autorisation de .NET doit bloquer avec un "Accès Refusé" (403)
        // Cela prouve que ton système de rôles est parfaitement configuré et étanche !
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}