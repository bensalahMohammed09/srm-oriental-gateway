using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides; // 🚀 Ajouté pour le Reverse Proxy
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore; // 🚀 Ajouté pour MigrateAsync
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Prometheus;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Srm.Gateway.Api.Middlewares;
using Srm.Gateway.Infrastructure;
using Srm.Gateway.Infrastructure.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- 1. LOGGING CONFIGURATION (SERILOG) ---
// Configuration optimisée pour LOKI via Promtail (format JSON)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

// --- 2. SERVICE COLLECTION ---
builder.Services.AddControllers();

builder.Services.AddHealthChecks();

// Configuration du Reverse Proxy : Pour récupérer l'IP réelle et le protocole (HTTP/HTTPS)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // On vide les réseaux connus car Docker change souvent d'IP interne
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(Srm.Gateway.Application.Mappings.DocumentMappingProfile).Assembly));

// A. Identity Bastion
builder.Services.AddIdentity<IdentityUser<Guid>, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 12;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<SrmDbContext>()
.AddDefaultTokenProviders();

// B. Double Shield (JWT + Cookie)
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"] ?? "SRM_ORIENTAL_SUPER_SECRET_KEY_2026_DO_NOT_SHARE_BY_MOHAMMED";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "srm-gateway",
        ValidAudience = jwtSettings["Audience"] ?? "srm-frontend",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    // 🚀 SRE DIAGNOSTICS : Utile pour Grafana/Loki en cas de 401
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.TryGetValue("SRM_AUTH_TOKEN", out var token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Log.Warning("[AUTH] Authentication failed: {Message}", context.Exception.Message);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// 🛡️ REVERSE PROXY SECURITY : Politique des cookies unifiée via Nginx
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.Secure = CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Servers = new List<OpenApiServer>
        {
            // On pointe vers le port exposé par Nginx (3000 dans ton .env)
            new OpenApiServer { Url = "http://localhost:3000" }
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// --- 3. INITIALISATION (MIGRATIONS & SEEDERS) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<SrmDbContext>();

    try
    {
        Log.Information("🚀 Starting Database Migration...");
        // 🛠️ AUTOMATIC MIGRATION : Applique les changements de schéma EF Core au démarrage
        await context.Database.MigrateAsync();

        Log.Information("🌱 Starting Data Seeding...");
        await DataSeeder.SeedLookupDataAsync(context);
        await IdentitySeeder.SeedRolesAndAdminAsync(services);

        Log.Information("✅ Infrastructure Readiness Check: OK");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "❌ Initial database setup failed. Application cannot start.");
        throw; // Force l'arrêt du conteneur pour que Docker le redémarre (Restart Policy)
    }
}

// --- 4. MIDDLEWARE PIPELINE ---

// Applique les headers X-Forwarded-For avant toute chose
app.UseForwardedHeaders();

app.UseHttpMetrics(); // Metrics Prometheus
app.UseMiddleware<ExceptionMiddleware>();
app.UseSerilogRequestLogging();
app.UseRouting();

// Les cookies et l'auth travaillent maintenant avec les headers de Nginx
app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("SRM Oriental Gateway API")
               .WithTheme(ScalarTheme.Mars)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        options.AddPreferredSecuritySchemes("http");
    });
}

app.MapControllers();
app.MapMetrics().AllowAnonymous(); // for prometheus
app.MapHealthChecks("/health").AllowAnonymous(); // for /health endpoint 



app.Run();
// essentials for unittests 
public partial class Program
{
    protected Program() { }
}