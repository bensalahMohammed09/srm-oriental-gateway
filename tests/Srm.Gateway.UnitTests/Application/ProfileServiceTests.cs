using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

// Tes namespaces
using Srm.Gateway.Application.Services;
using Srm.Gateway.Domain.Entities;
using Srm.Gateway.Application.Interfaces;

namespace Srm.Gateway.UnitTests.Application;

public class ProfileServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IBaseRepository<Document>> _mockDocRepo;
    private readonly Mock<UserManager<IdentityUser>> _mockUserManager;
    private readonly ProfileService _profileService;

    public ProfileServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockDocRepo = new Mock<IBaseRepository<Document>>();

        _mockUow.SetupGet(u => u.Documents).Returns(_mockDocRepo.Object);

        // 🟢 Astuce : Mocker le UserManager nécessite de mocker d'abord le IUserStore
        var store = new Mock<IUserStore<IdentityRole>>(); // Type factice pour satisfaire le constructeur
        var userStoreMock = new Mock<IUserStore<IdentityUser>>();
        _mockUserManager = new Mock<UserManager<IdentityUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _profileService = new ProfileService(_mockUserManager.Object, _mockUow.Object);
    }

    // --- TESTS POUR GetProfileAsync ---

    [Fact]
    public async Task GetProfileAsync_ShouldReturnProfile_WhenUserExists()
    {
        // Arrange
        var userId = "user-123";
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var user = new IdentityUser { Id = userId, Email = "test@srm.ma", UserName = "TestUser" };
        var roles = new List<string> { "ROLE_BO", "ROLE_TECH" };

        _mockUserManager.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(user);
        _mockUserManager.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(roles);

        // Act
        var result = await _profileService.GetProfileAsync(principal);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.Email.Should().Be("test@srm.ma");
        result.Roles.Should().BeEquivalentTo("ROLE_BO", "ROLE_TECH");
    }

    [Fact]
    public async Task GetProfileAsync_ShouldThrowUnauthorized_WhenNoNameIdentifierClaim()
    {
        // Arrange : Un principal vide (sans NameIdentifier)
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        Func<Task> act = async () => await _profileService.GetProfileAsync(principal);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
                 .WithMessage("Utilisateur non identifié dans le jeton.");
    }

    [Fact]
    public async Task GetProfileAsync_ShouldThrowKeyNotFound_WhenUserNotInDatabase()
    {
        // Arrange
        var userId = "ghost-user";
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        _mockUserManager.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync((IdentityUser)null!);

        // Act
        Func<Task> act = async () => await _profileService.GetProfileAsync(principal);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
                 .WithMessage("Utilisateur introuvable dans la base de données.");
    }

    // --- TESTS POUR GetDashboardStatsAsync ---

    [Fact]
    public async Task GetDashboardStatsAsync_ShouldCalculateCorrectStats()
    {
        // Arrange : Création d'un faux jeu de données de documents
        var pendingStatus = new Status { Code = "BUS_PENDING_VAL" };
        var approvedStatus = new Status { Code = "APPROVED" };
        var rejectedStatus = new Status { Code = "REJECTED" };

        var categoryIt = new Category { Name = "IT" };
        var categoryMaint = new Category { Name = "MAINTENANCE" };

        var documents = new List<Document>
        {
            new Document { Reference = "DOC-1", Status = pendingStatus, Category = categoryIt, TotalAmount = 1000m, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new Document { Reference = "DOC-2", Status = pendingStatus, Category = categoryIt, TotalAmount = 500m, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new Document { Reference = "DOC-3", Status = approvedStatus, Category = categoryMaint, TotalAmount = 2000m, CreatedAt = DateTime.UtcNow }, // Seul l'APPROVED compte pour le montant
            new Document { Reference = "DOC-4", Status = rejectedStatus, Category = categoryMaint, TotalAmount = 8000m, CreatedAt = DateTime.UtcNow.AddHours(-5) }
        };

        _mockDocRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(documents);

        // Act
        var stats = await _profileService.GetDashboardStatsAsync();

        // Assert
        stats.TotalDocuments.Should().Be(4);

        // Il y a 2 documents en BUS_PENDING_VAL
        stats.PendingValidation.Should().Be(2);

        // Seul le DOC-3 est approuvé (2000m)
        stats.ApprovedAmount.Should().Be(2000m);

        // Il y a 1 document rejeté
        stats.RejectedCount.Should().Be(1);

        // Vérification de la distribution par catégorie (2 IT, 2 MAINTENANCE)
        stats.Distribution.Should().HaveCount(2);
        stats.Distribution.First(d => d.Name == "IT").Value.Should().Be(2);
        stats.Distribution.First(d => d.Name == "MAINTENANCE").Value.Should().Be(2);

        // Vérification des activités récentes (les 5 plus récents, on en a 4)
        stats.RecentActivity.Should().HaveCount(4);
        stats.RecentActivity.First().Reference.Should().Be("DOC-3"); // Le plus récent (DateTime.UtcNow)
    }
}