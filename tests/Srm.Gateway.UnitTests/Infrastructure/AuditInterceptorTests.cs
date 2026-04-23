using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

// Tes namespaces
using Srm.Gateway.Domain.Entities;
using Srm.Gateway.Infrastructure.Data;
using Srm.Gateway.Infrastructure.Interceptors;

namespace Srm.Gateway.UnitTests.Infrastructure;

public class AuditInterceptorTests : IDisposable
{
    private readonly SrmDbContext _context;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Guid _testUserId = Guid.NewGuid(); // L'ID de notre faux utilisateur

    public AuditInterceptorTests()
    {
        // 1. Mocker l'utilisateur connecté
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        SetupMockUser(_testUserId.ToString());

        // 2. Initialiser l'intercepteur avec le mock
        var interceptor = new AuditInterceptor(_mockHttpContextAccessor.Object);

        // 3. Configurer SQLite en mémoire avec l'intercepteur branché
        var options = new DbContextOptionsBuilder<SrmDbContext>()
            .UseSqlite("DataSource=:memory:")
            .AddInterceptors(interceptor)
            .Options;

        _context = new SrmDbContext(options);

        // 4. Ouvrir la connexion et créer le schéma
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    // --- Helper pour simuler l'utilisateur ---
    private void SetupMockUser(string userId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _mockHttpContextAccessor.SetupGet(x => x.HttpContext).Returns(httpContext);
    }

    // --- TESTS ---

    [Fact]
    public async Task SavingChangesAsync_ShouldCreateAuditLog_WithCorrectUserId_WhenEntityIsAdded()
    {
        // Arrange
        var category = new Category { Id = Guid.NewGuid(), Name = "Category Test" };

        // Act
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        // Assert
        var auditLogs = await _context.Set<AuditLog>().ToListAsync();

        auditLogs.Should().HaveCount(1);
        var log = auditLogs.First();

        log.Action.Should().Be("Added");
        log.EntityName.Should().Be(nameof(Category));
        log.EntityId.Should().Be(category.Id.ToString());

        // 🟢 VÉRIFICATION CRUCIALE : L'intercepteur a bien lu l'ID depuis le HttpContext !
        log.UserId.Should().Be(_testUserId);

        log.Changes.Should().Contain("Category Test");
    }

    [Fact]
    public async Task SavingChangesAsync_ShouldRecordFromAndTo_WhenEntityIsModified()
    {
        // Arrange
        var category = new Category { Id = Guid.NewGuid(), Name = "Nom Original" };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        // Act
        category.Name = "Nom Modifie";
        _context.Categories.Update(category);
        await _context.SaveChangesAsync();

        // Assert
        var auditLogs = await _context.Set<AuditLog>().OrderBy(a => a.CreatedAt).ToListAsync();

        auditLogs.Should().HaveCount(2);

        var modifiedLog = auditLogs.Last();
        modifiedLog.Action.Should().Be("Modified");
        modifiedLog.EntityName.Should().Be(nameof(Category));
        modifiedLog.UserId.Should().Be(_testUserId);

        modifiedLog.Changes.Should().Contain("Nom Original");
        modifiedLog.Changes.Should().Contain("Nom Modifie");
    }

    [Fact]
    public async Task SavingChangesAsync_ShouldHandleNullUser_Gracefully()
    {
        // Arrange : On simule une requête sans utilisateur connecté (ex: tâche planifiée)
        _mockHttpContextAccessor.SetupGet(x => x.HttpContext).Returns(new DefaultHttpContext());

        var category = new Category { Id = Guid.NewGuid(), Name = "Anonymous Category" };

        // Act
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        // Assert
        var auditLogs = await _context.Set<AuditLog>().ToListAsync();

        auditLogs.Should().HaveCount(1);
        var log = auditLogs.First();

        // 🟢 VÉRIFICATION : Si personne n'est connecté, UserId doit être null, et ça ne doit pas planter
        log.UserId.Should().BeNull();
    }
}