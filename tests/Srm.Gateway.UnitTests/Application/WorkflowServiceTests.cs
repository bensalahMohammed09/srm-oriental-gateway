using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

// Tes namespaces
using Srm.Gateway.Application.Services;
using Srm.Gateway.Domain.Entities;
using Srm.Gateway.Application.Interfaces;

namespace Srm.Gateway.UnitTests.Application;

public class WorkflowServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IBaseRepository<Document>> _mockDocRepo;
    private readonly Mock<IBaseRepository<Status>> _mockStatusRepo;
    private readonly Mock<IBaseRepository<Workflow>> _mockWorkflowRepo;
    private readonly Mock<IHttpContextAccessor> _mockHttpContext;
    private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
    private readonly Mock<IDocumentService> _mockDocService; // 🟢 NOUVEAU

    private readonly WorkflowService _workflowService;

    public WorkflowServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockDocRepo = new Mock<IBaseRepository<Document>>();
        _mockStatusRepo = new Mock<IBaseRepository<Status>>();
        _mockWorkflowRepo = new Mock<IBaseRepository<Workflow>>();

        _mockUow.SetupGet(u => u.Documents).Returns(_mockDocRepo.Object);
        _mockUow.SetupGet(u => u.Statuses).Returns(_mockStatusRepo.Object);
        _mockUow.SetupGet(u => u.Workflows).Returns(_mockWorkflowRepo.Object);

        _mockHttpContext = new Mock<IHttpContextAccessor>();
        _mockDocService = new Mock<IDocumentService>();

        // Configuration basique du RoleManager (Moq demande des paramètres factices pour les classes Identity)
        var store = new Mock<IRoleStore<IdentityRole>>();
        _mockRoleManager = new Mock<RoleManager<IdentityRole>>(store.Object, null!, null!, null!, null!);

        // 🟢 Injection du nouveau service dans le constructeur
        _workflowService = new WorkflowService(
            _mockUow.Object,
            _mockHttpContext.Object,
            _mockRoleManager.Object,
            _mockDocService.Object);
    }

    // --- SCÉNARIO 1 : Le document passe à l'étape suivante ---
    [Fact]
    public async Task ApproveDocumentAsync_ShouldMoveToNextRole_BasedOnCategory()
    {
        // Arrange
        SetupMockUser(Guid.NewGuid().ToString(), "ROLE_BO");

        var docId = Guid.NewGuid();
        var pendingStatus = new Status { Id = Guid.NewGuid(), Code = "BUS_PENDING_VAL" };
        var category = new Category { Id = Guid.NewGuid(), Name = "INFORMATIQUE_&_TÉLÉCOM" };

        var document = new Document
        {
            Id = docId,
            Category = category,
            Workflows = new List<Workflow>
            {
                new Workflow { AssignedRole = new IdentityRole { Name = "ROLE_BO" }, ValidatedAt = DateTime.UtcNow }
            }
        };

        var targetRole = new IdentityRole { Id = "role-tech-id", Name = "ROLE_TECH" };

        _mockDocRepo.Setup(r => r.GetByIdAsync(docId)).ReturnsAsync(document);
        _mockStatusRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Status> { pendingStatus });
        _mockRoleManager.Setup(r => r.FindByNameAsync("ROLE_TECH")).ReturnsAsync(targetRole);

        // Act
        await _workflowService.ApproveDocumentAsync(docId, "Validé par BO");

        // Assert
        document.StatusId.Should().Be(pendingStatus.Id);

        _mockWorkflowRepo.Verify(r => r.AddAsync(It.Is<Workflow>(w =>
            w.AssignedRoleId == targetRole.Id &&
            w.CurrentStatus == "BUS_PENDING_VAL" &&
            w.StepName == "Transmission à ROLE_TECH")), Times.Once);

        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    // --- SCÉNARIO 2 : Fin du workflow et archivage physique ---
    [Fact]
    public async Task ApproveDocumentAsync_ShouldFinalizeAndArchive_WhenLastRoleApproves()
    {
        // Arrange
        SetupMockUser(Guid.NewGuid().ToString(), "ROLE_FINANCE");

        var docId = Guid.NewGuid();
        var approvedStatus = new Status { Id = Guid.NewGuid(), Code = "APPROVED" };
        var category = new Category { Id = Guid.NewGuid(), Name = "INFORMATIQUE_&_TÉLÉCOM" };

        var document = new Document
        {
            Id = docId,
            Category = category,
            Workflows = new List<Workflow>
            {
                // Le dernier rôle du dictionnaire BPMN est ROLE_FINANCE
                new Workflow { AssignedRole = new IdentityRole { Name = "ROLE_FINANCE" }, ValidatedAt = DateTime.UtcNow }
            }
        };

        _mockDocRepo.Setup(r => r.GetByIdAsync(docId)).ReturnsAsync(document);
        _mockStatusRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Status> { approvedStatus });

        // Act
        await _workflowService.ApproveDocumentAsync(docId, "OK pour paiement");

        // Assert
        document.StatusId.Should().Be(approvedStatus.Id);

        _mockWorkflowRepo.Verify(r => r.AddAsync(It.Is<Workflow>(w =>
            w.CurrentStatus == "APPROVED" &&
            w.AssignedRoleId == "")), Times.Once);

        // 🟢 Vérification SRE : L'archivage a bien été déclenché !
        _mockDocService.Verify(s => s.ArchiveDocumentFileAsync(docId), Times.Once);

        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    // --- SCÉNARIO 3 : Rejet par un validateur ---
    [Fact]
    public async Task RejectDocumentAsync_ShouldSetStatusToRejected_AndAssignToBO()
    {
        // Arrange
        SetupMockUser(Guid.NewGuid().ToString(), "ROLE_TECH");

        var docId = Guid.NewGuid();
        var rejectedStatus = new Status { Id = Guid.NewGuid(), Code = "REJECTED" };
        var boRole = new IdentityRole { Id = "role-bo-id", Name = "ROLE_BO" };

        var document = new Document { Id = docId };

        _mockDocRepo.Setup(r => r.GetByIdAsync(docId)).ReturnsAsync(document);
        _mockStatusRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Status> { rejectedStatus });
        _mockRoleManager.Setup(r => r.FindByNameAsync("ROLE_BO")).ReturnsAsync(boRole);

        // Act
        await _workflowService.RejectDocumentAsync(docId, "Montant incorrect");

        // Assert
        document.StatusId.Should().Be(rejectedStatus.Id);

        _mockWorkflowRepo.Verify(r => r.AddAsync(It.Is<Workflow>(w =>
            w.CurrentStatus == "REJECTED" &&
            w.AssignedRoleId == boRole.Id &&
            w.Comment == "Montant incorrect")), Times.Once);

        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    // --- Helper pour simuler l'utilisateur et ses rôles ---
    private void SetupMockUser(string userId, string role)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _mockHttpContext.SetupGet(x => x.HttpContext).Returns(httpContext);
    }
}