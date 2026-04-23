using FluentAssertions;
// Tes namespaces
using Srm.Gateway.Api.Controllers;
using Srm.Gateway.IntegrationTests.Shared;
using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Srm.Gateway.IntegrationTests.Controllers;

// IClassFixture permet à xUnit de garder l'API allumée en mémoire pour tous les tests de ce fichier
public class WorkflowControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public WorkflowControllerTests(TestWebApplicationFactory factory)
    {
        // On crée un faux navigateur web (HttpClient) connecté à notre API en mémoire
        _client = factory.CreateClient();

        // 🛡️ MAGIE : On force le client à utiliser notre faux token d'Agent BO
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestScheme");
    }

    [Fact]
    public async Task GetMyTasks_ShouldReturnOk_WithEmptyOrPopulatedList()
    {
        // Act : On fait un vrai appel HTTP GET (comme le ferait React)
        var response = await _client.GetAsync("/api/v1/workflow/my-tasks");

        // Assert : On vérifie que le serveur répond 200 OK
        // Si ça passe, ça prouve que :
        // 1. Le routage fonctionne
        // 2. Le middleware [Authorize] a accepté notre faux token
        // 3. Le contrôleur a appelé le service sans planter
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RejectDocument_ShouldReturnBadRequest_WhenCommentIsMissing()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var requestBody = new WorkflowActionRequest(Comment: ""); // Commentaire vide !

        // Act : On fait un appel HTTP POST avec un body JSON
        var response = await _client.PostAsJsonAsync($"/api/v1/workflow/{documentId}/reject", requestBody);

        // Assert : On vérifie que l'API bloque bien la requête
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // On vérifie même le contenu de la réponse renvoyée par le contrôleur
        var responseText = await response.Content.ReadAsStringAsync();
        responseText.Should().Contain("Un motif de rejet est obligatoire");
    }
}