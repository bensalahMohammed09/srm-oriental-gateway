using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using Srm.Gateway.Application.Interfaces;
using Srm.Gateway.Application.Services;
using Srm.Gateway.Domain.Entities;
using Xunit;

namespace Srm.Gateway.UnitTests.Application;

public class WorkflowServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<IDocumentService> _documentServiceMock;
    private readonly WorkflowService _workflowService;

    public WorkflowServiceTests()
    {
        // 🛠️ CORRECTION : L'ajout de { DefaultValue = DefaultValue.Mock } empêche toutes les NullReferenceException 
        // liées aux "await" non configurés (comme _unitOfWork.Workflows.AddAsync).
        _unitOfWorkMock = new Mock<IUnitOfWork> { DefaultValue = DefaultValue.Mock };
        _httpContextAccessorMock = new Mock<IHttpContextAccessor> { DefaultValue = DefaultValue.Mock };
        _documentServiceMock = new Mock<IDocumentService> { DefaultValue = DefaultValue.Mock };

        var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
        _roleManagerMock = new Mock<RoleManager<IdentityRole>>(roleStoreMock.Object, null!, null!, null!, null!);
        _roleManagerMock.Setup(r => r.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((string name) => new IdentityRole { Id = Guid.NewGuid().ToString(), Name = name });

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-123") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContextAccessorMock.Setup(x => x.HttpContext!.User).Returns(new ClaimsPrincipal(identity));

        var statuses = new List<Status>
        {
            new Status { Id = Guid.NewGuid(), Code = "BUS_PENDING_VAL", Name = "En attente" },
            new Status { Id = Guid.NewGuid(), Code = "REJECTED", Name = "Rejeté" },
            new Status { Id = Guid.NewGuid(), Code = "APPROVED", Name = "Approuvé" }
        };
        _unitOfWorkMock.Setup(u => u.Statuses.GetAllAsync()).ReturnsAsync(statuses);

        _workflowService = new WorkflowService(
            _unitOfWorkMock.Object,
            _httpContextAccessorMock.Object,
            _roleManagerMock.Object,
            _documentServiceMock.Object);
    }

    [Fact]
    public async Task StartProcessAsync_ShouldAssignToFirstRoleAfterBO()
    {
        var docId = Guid.NewGuid();
        var document = new Document
        {
            Id = docId,
            Category = new Category { Name = "INFORMATIQUE_&_TÉLÉCOM" },
            Workflows = new List<Workflow>()
        };

        _unitOfWorkMock.Setup(u => u.Documents.GetByIdAsync(docId)).ReturnsAsync(document);

        await _workflowService.StartProcessAsync(docId, "Démarrage OCR");

        _unitOfWorkMock.Verify(u => u.Workflows.AddAsync(It.Is<Workflow>(w => w.AssignedRoleId != "")), Times.Once);
        _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task ApproveStepAsync_ShouldMoveToNextRole_WhenNotLastStep()
    {
        var docId = Guid.NewGuid();
        var document = new Document
        {
            Id = docId,
            Category = new Category { Name = "INFORMATIQUE_&_TÉLÉCOM" },
            Workflows = new List<Workflow>
            {
                new Workflow { AssignedRole = new IdentityRole { Name = "ROLE_TECH" }, ValidatedAt = DateTime.UtcNow }
            }
        };

        _unitOfWorkMock.Setup(u => u.Documents.GetByIdAsync(docId)).ReturnsAsync(document);

        await _workflowService.ApproveStepAsync(docId, "OK pour le service technique");

        _unitOfWorkMock.Verify(u => u.Workflows.AddAsync(It.IsAny<Workflow>()), Times.Once);
        _documentServiceMock.Verify(d => d.ArchiveDocumentFileAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ApproveStepAsync_ShouldFinalizeAndArchive_WhenLastStep()
    {
        var docId = Guid.NewGuid();
        var document = new Document
        {
            Id = docId,
            Category = new Category { Name = "INFORMATIQUE_&_TÉLÉCOM" },
            Workflows = new List<Workflow>
            {
                new Workflow { AssignedRole = new IdentityRole { Name = "ROLE_FINANCE" }, ValidatedAt = DateTime.UtcNow }
            }
        };

        _unitOfWorkMock.Setup(u => u.Documents.GetByIdAsync(docId)).ReturnsAsync(document);

        await _workflowService.ApproveStepAsync(docId, "OK pour le paiement");

        _unitOfWorkMock.Verify(u => u.Workflows.AddAsync(It.Is<Workflow>(w => w.CurrentStatus == "APPROVED")), Times.Once);
        _documentServiceMock.Verify(d => d.ArchiveDocumentFileAsync(docId), Times.Once);
    }

    [Fact]
    public async Task RejectStepAsync_ShouldAssignToBO_AndSetStatusRejected()
    {
        var docId = Guid.NewGuid();
        var document = new Document { Id = docId, Workflows = new List<Workflow>() };

        _unitOfWorkMock.Setup(u => u.Documents.GetByIdAsync(docId)).ReturnsAsync(document);

        await _workflowService.RejectStepAsync(docId, "Montant incorrect");

        _unitOfWorkMock.Verify(u => u.Workflows.AddAsync(It.Is<Workflow>(w => w.CurrentStatus == "REJECTED")), Times.Once);
        _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
    }
}