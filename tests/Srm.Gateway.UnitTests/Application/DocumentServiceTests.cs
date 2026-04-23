using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

// Tes namespaces
using Srm.Gateway.Application.DTOs;
using Srm.Gateway.Application.Services;
using Srm.Gateway.Domain.Entities;
using Srm.Gateway.Application.Interfaces;

namespace Srm.Gateway.UnitTests.Application;

public class DocumentServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IBaseRepository<Document>> _mockDocRepo;
    private readonly Mock<IBaseRepository<Status>> _mockStatusRepo;
    private readonly Mock<IBaseRepository<OcrMetadata>> _mockMetadataRepo;
    private readonly Mock<IBaseRepository<Workflow>> _mockWorkflowRepo;
    private readonly Mock<IHttpContextAccessor> _mockHttpContext;
    private readonly DocumentService _documentService;

    public DocumentServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockDocRepo = new Mock<IBaseRepository<Document>>();
        _mockStatusRepo = new Mock<IBaseRepository<Status>>();
        _mockMetadataRepo = new Mock<IBaseRepository<OcrMetadata>>();
        _mockWorkflowRepo = new Mock<IBaseRepository<Workflow>>();

        // Lier les repos au UoW
        _mockUow.SetupGet(u => u.Documents).Returns(_mockDocRepo.Object);
        _mockUow.SetupGet(u => u.Statuses).Returns(_mockStatusRepo.Object);
        _mockUow.SetupGet(u => u.Metadata).Returns(_mockMetadataRepo.Object);
        _mockUow.SetupGet(u => u.Workflows).Returns(_mockWorkflowRepo.Object);

        _mockHttpContext = new Mock<IHttpContextAccessor>();

        _documentService = new DocumentService(_mockUow.Object, _mockHttpContext.Object);
    }

    // --- SCÉNARIO 1 : Ingestion OCR ---
    [Fact]
    public async Task IngestDocumentAsync_ShouldCreateDocument_WithTechToIndexStatus()
    {
        // Arrange
        var targetStatus = new Status { Id = Guid.NewGuid(), Code = "TECH_TO_INDEX" };
        _mockStatusRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Status> { targetStatus });

        var request = new OcrIngestionRequest(
            "INV-2024-001",
            "Fournisseur Test",
            1500.50m,
            new List<OcrMetadataInputDto>
            {
                new OcrMetadataInputDto("TVA", "20", 0.95)
            }
        );

        // Act
        var resultId = await _documentService.IngestDocumentAsync(request);

        // Assert
        resultId.Should().NotBeEmpty();

        _mockDocRepo.Verify(r => r.AddAsync(It.Is<Document>(d =>
            d.Reference == "INV-2024-001" &&
            d.StatusId == targetStatus.Id &&
            d.TotalAmount == 1500.50m)), Times.Once);

        _mockMetadataRepo.Verify(r => r.AddAsync(It.Is<OcrMetadata>(m =>
            m.Key == "TVA" && m.Value == "20")), Times.Once);

        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    // --- SCÉNARIO 2 : Validation par l'agent BO ---
    [Fact]
    public async Task ConfirmIndexationAsync_ShouldUpdateDocument_AndCreateWorkflowEntry()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        SetupMockUser(userId); // Simuler l'utilisateur connecté

        var documentId = Guid.NewGuid();
        var pendingStatus = new Status { Id = Guid.NewGuid(), Code = "BUS_PENDING_VAL" };
        var categoryId = Guid.NewGuid();

        var document = new Document { Id = documentId, Reference = "OLD-REF", TotalAmount = 0 };

        _mockDocRepo.Setup(r => r.GetByIdAsync(documentId)).ReturnsAsync(document);
        _mockStatusRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Status> { pendingStatus });

        var request = new DocumentValidationRequest(
            categoryId,
            "NEW-REF",
            2000m,
            new List<OcrMetadataUpdateDto>()
        );

        // Act
        await _documentService.ConfirmIndexationAsync(documentId, request);

        // Assert
        document.Reference.Should().Be("NEW-REF");
        document.TotalAmount.Should().Be(2000m);
        document.CategoryId.Should().Be(categoryId);
        document.StatusId.Should().Be(pendingStatus.Id);

        // Vérifier que le Workflow contient bien l'ID de l'utilisateur qui a validé
        _mockWorkflowRepo.Verify(r => r.AddAsync(It.Is<Workflow>(w =>
            w.DocumentId == documentId &&
            w.ValidatedByUserId == userId &&
            w.CurrentStatus == "VALIDATED_BY_BO" &&
            w.StepName == "Indexation initiale")), Times.Once);

        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    // --- SCÉNARIO 3 : Document introuvable lors de la validation ---
    [Fact]
    public async Task ConfirmIndexationAsync_ShouldThrowException_WhenDocumentNotFound()
    {
        // Arrange
        var invalidDocId = Guid.NewGuid();
        _mockDocRepo.Setup(r => r.GetByIdAsync(invalidDocId)).ReturnsAsync((Document)null!);

        var request = new DocumentValidationRequest(Guid.NewGuid(), "REF", 100, new List<OcrMetadataUpdateDto>());

        // Act
        Func<Task> act = async () => await _documentService.ConfirmIndexationAsync(invalidDocId, request);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Document introuvable.");
        _mockWorkflowRepo.Verify(r => r.AddAsync(It.IsAny<Workflow>()), Times.Never);
    }

    // --- SCÉNARIO 4 : Protection contre les doublons lors de la récupération manuelle ---
    [Fact]
    public async Task RecoverFailedDocumentAsync_ShouldThrowException_IfReferenceAlreadyExists()
    {
        // Arrange
        var request = new ManualRecoveryRequest("error_file.pdf", "DUPLICATE-REF", 500m);

        // On simule que la base de données contient déjà cette référence
        _mockDocRepo.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Document, bool>>>()))
                    .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _documentService.RecoverFailedDocumentAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("Une facture avec la référence 'DUPLICATE-REF' existe déjà dans le système.");

        _mockDocRepo.Verify(r => r.AddAsync(It.IsAny<Document>()), Times.Never);
    }

    // --- SCÉNARIO 5 : Filtrage des documents en attente ---
    [Fact]
    public async Task GetPendingIndexationAsync_ShouldReturnOnlyTechToIndexDocuments()
    {
        // Arrange
        var techToIndexStatusId = Guid.NewGuid();
        var targetStatus = new Status { Id = techToIndexStatusId, Code = "TECH_TO_INDEX" };
        var otherStatus = new Status { Id = Guid.NewGuid(), Code = "OTHER_STATUS" };

        _mockStatusRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Status> { targetStatus });

        var docsInDb = new List<Document>
        {
            new Document { Id = Guid.NewGuid(), StatusId = techToIndexStatusId, Reference = "DOC-1" },
            new Document { Id = Guid.NewGuid(), StatusId = otherStatus.Id, Reference = "DOC-2" }, // Ne doit pas être retourné
            new Document { Id = Guid.NewGuid(), StatusId = techToIndexStatusId, Reference = "DOC-3" }
        };

        _mockDocRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(docsInDb);

        // Act
        var result = await _documentService.GetPendingIndexationAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Select(d => d.Reference).Should().Contain(new[] { "DOC-1", "DOC-3" });
    }

    // --- Helper pour simuler le IHttpContextAccessor ---
    private void SetupMockUser(string userId)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _mockHttpContext.SetupGet(x => x.HttpContext).Returns(httpContext);
    }
}