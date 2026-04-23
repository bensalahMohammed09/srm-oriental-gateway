using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Srm.Gateway.Infrastructure.Data;
using Srm.Gateway.Infrastructure.Interceptors;

namespace Srm.Gateway.IntegrationTests.Shared;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection _connection;

    public TestWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // 1. On supprime les options générées par ton DependencyInjection.cs
            services.RemoveAll(typeof(DbContextOptions<SrmDbContext>));
            services.RemoveAll(typeof(DbContextOptions));

            // 2. 🟢 LA SOLUTION MAGIQUE : On injecte directement nos propres options !
            // Cela empêche ASP.NET d'exécuter ton "UseNpgsql" en arrière-plan.
            services.AddScoped<DbContextOptions<SrmDbContext>>(sp =>
            {
                var auditInterceptor = sp.GetRequiredService<AuditInterceptor>();

                return new DbContextOptionsBuilder<SrmDbContext>()
                    .UseSqlite(_connection)
                    .UseSnakeCaseNamingConvention()
                    .AddInterceptors(auditInterceptor)
                    .Options; // <-- On renvoie l'objet final directement !
            });

            // 3. Création des tables dans la RAM
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SrmDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
            }

            // 4. Authentification factice
            services.AddAuthentication("TestScheme")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });

            services.Configure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "TestScheme";
                options.DefaultChallengeScheme = "TestScheme";
                options.DefaultScheme = "TestScheme";
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Close();
        _connection?.Dispose();
    }
}

// --- Faux système d'authentification pour les tests ---
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 🛑 LA CORRECTION EST ICI : On vérifie que la requête contient bien l'en-tête d'autorisation !
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.NoResult()); // Client anonyme -> 401 Unauthorized
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, "AgentTest"),
            new Claim(ClaimTypes.Role, "ROLE_BO")
        };
        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}