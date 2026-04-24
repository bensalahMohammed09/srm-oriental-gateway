using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Srm.Gateway.Application.DTOs;
using Srm.Gateway.Application.Interfaces;
using Srm.Gateway.Application.Services;
using Srm.Gateway.Domain.Entities;
using Xunit;

namespace Srm.Gateway.UnitTests.Application;

public class DocumentServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IWorkflowService> _workflowServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly DocumentService _documentService;
    private readonly string _tempPath;

    public DocumentServiceTests()
    {
        // 🛠️ CORRECTION CRITIQUE : DefaultValue = DefaultValue.Mock
        // Cela force Moq à créer des objets factices pour .Documents au lieu de renvoyer null.
        _unitOfWorkMock = new Mock<IUnitOfWork> { DefaultValue = DefaultValue.Mock };
        _httpContextAccessorMock = new Mock<IHttpContextAccessor> { DefaultValue = DefaultValue.Mock };
        _workflowServiceMock = new Mock<IWorkflowService> { DefaultValue = DefaultValue.Mock };
        _configurationMock = new Mock<IConfiguration> { DefaultValue = DefaultValue.Mock };

        _tempPath = Path.Combine(Path.GetTempPath(), "SrmTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempPath);

        _configurationMock.Setup(c => c["Storage:PendingPath"]).Returns(Path.Combine(_tempPath, "pending"));
        _configurationMock.Setup(c => c["Storage:ProcessedPath"]).Returns(Path.Combine(_tempPath, "processed"));

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-123") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContextAccessorMock.Setup(x => x.HttpContext!.User).Returns(new ClaimsPrincipal(identity));

        var statuses = new List<Status> { new Status { Id = Guid.NewGuid(), Code = "BUS_PENDING_VAL" } };
        _unitOfWorkMock.Setup(u => u.Statuses.GetAllAsync()).ReturnsAsync(statuses);

        _documentService = new DocumentService(
            _unitOfWorkMock.Object,
            _httpContextAccessorMock.Object,
            _workflowServiceMock.Object,
            _configurationMock.Object);
    }

    [Fact]
    public async Task ConfirmIndexationAsync_ShouldUpdateDocument_AndStartWorkflow()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var existingDoc = new Document { Id = documentId, StatusId = Guid.NewGuid() };
        
        _unitOfWorkMock.Setup(u => u.Documents.GetByIdAsync(documentId)).ReturnsAsync(existingDoc);
        
        // 🛠️ CORRECTION ICI : Utilisation du constructeur du "Record" dans le bon ordre (Id, Ref, Amount, Dictionnaire)
        var request = new DocumentValidationRequest(Guid.NewGuid(), "REF-123", 500.00m, null);

        // Act
        await _documentService.ConfirmIndexationAsync(documentId, request);

        // Assert
        _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        existingDoc.Reference.Should().Be("REF-123");
        _workflowServiceMock.Verify(w => w.StartProcessAsync(documentId, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetFailedDocumentFileAsync_ShouldThrowUnauthorizedAccessException_WhenPathTraversalAttempted()
    {
        // Arrange
        var maliciousFileName = "../../../etc/passwd";

        // Act
        Func<Task> act = async () => await _documentService.GetFailedDocumentFileAsync(maliciousFileName);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
                 .WithMessage("Tentative d'accès non autorisée en dehors du dossier.");
    }

    [Fact]
    public async Task CreateManualDocumentAsync_ShouldSaveFile_AndTriggerWorkflow()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        writer.Write("Fake PDF");
        writer.Flush();
        ms.Position = 0;

        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns("test.pdf");
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default)).Returns(Task.CompletedTask);

        var request = new ManualUploadRequest(fileMock.Object, "REF-1", "Supp", 100m, Guid.NewGuid());

        // Act
        var resultId = await _documentService.CreateManualDocumentAsync(request);

        // Assert
        resultId.Should().NotBeEmpty();
        _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        _workflowServiceMock.Verify(w => w.StartProcessAsync(resultId, It.IsAny<string>()), Times.Once);
    }
}