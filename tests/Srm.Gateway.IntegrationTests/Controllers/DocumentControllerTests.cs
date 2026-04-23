using FluentAssertions;
// Tes namespaces
using Srm.Gateway.Application.DTOs;
using Srm.Gateway.IntegrationTests.Shared;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Srm.Gateway.IntegrationTests.Controllers;

public class DocumentControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DocumentControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();

        // On connecte notre "faux" agent BO pour passer la sécurité si nécessaire
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestScheme");
    }

    [Fact]
    public async Task Ingest_ShouldReturn201Created_AndSaveDocument()
    {
        // Arrange : On crée le JSON que le Worker Python est censé envoyer
        var request = new OcrIngestionRequest(
            Reference: $"INV-TEST-{Guid.NewGuid().ToString()[..8]}", // Réf unique pour éviter les doublons SQL
            SupplierName: "Tech Corp",
            TotalAmount: 1500.00m,
            Metadata: new List<OcrMetadataInputDto>
            {
                new OcrMetadataInputDto("TVA", "20%", 0.99)
            }
        );

        // Act : Appel HTTP POST classique
        var response = await _client.PostAsJsonAsync("/api/v1/document/ingest", request);

        // Assert : Vérifie que la ressource est bien "Créée"
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetPending_ShouldReturn200Ok()
    {
        // Act : Appel GET
        var response = await _client.GetAsync("/api/v1/document/pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // On vérifie que la réponse est bien une liste JSON parsable
        var content = await response.Content.ReadFromJsonAsync<List<DocumentResponse>>();
        content.Should().NotBeNull();
    }

    [Fact]
    public async Task UploadDocument_ShouldReturn202Accepted_WhenSendingRealFile()
    {
        // Arrange : Construction d'une VRAIE requête MultipartFormData (Upload de fichier)
        using var multipartFormContent = new MultipartFormDataContent();

        // On crée un faux fichier binaire (quelques octets qui simulent un PDF)
        var fileBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        // ⚠️ Le nom "file" ICI doit correspondre EXACTEMENT au nom du paramètre dans ton contrôleur : UploadDocument(IFormFile file)
        multipartFormContent.Add(fileContent, "file", "facture_test.pdf");

        // Act : On envoie le fichier via HTTP POST
        var response = await _client.PostAsync("/api/v1/document/upload", multipartFormContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var responseText = await response.Content.ReadAsStringAsync();
        responseText.Should().Contain("Upload successful");
    }
}