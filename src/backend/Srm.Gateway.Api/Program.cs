using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
using Srm.Gateway.Application.Services;
using Srm.Gateway.Application.Interfaces;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- 1. LOGGING CONFIGURATION (SERILOG) ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

// --- 2. SERVICE COLLECTION ---
builder.Services.AddControllers();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IDocumentService, DocumentService>();

// A. Identity Bastion
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
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
    // 🛡️ CORRECTION .NET 9 : Obligatoire pour écraser le cookie Identity par défaut
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

    // 🚀 AJOUT SRE : Logging de diagnostic détaillé pour comprendre les 401
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            Console.WriteLine("[DEBUG-AUTH] Requête reçue sur : " + context.Request.Path);
            if (context.Request.Cookies.TryGetValue("SRM_AUTH_TOKEN", out var token))
            {
                Console.WriteLine("[DEBUG-AUTH] Cookie 'SRM_AUTH_TOKEN' TROUVÉ ! Longueur : " + token.Length);
                context.Token = token;
            }
            else
            {
                Console.WriteLine("[DEBUG-AUTH] ❌ AUCUN Cookie 'SRM_AUTH_TOKEN' trouvé dans la requête !");
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine("[DEBUG-AUTH] ❌ ÉCHEC DE L'AUTHENTIFICATION : " + context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("[DEBUG-AUTH] ✅ TOKEN VALIDÉ AVEC SUCCÈS pour : " + context.Principal?.Identity?.Name);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine("[DEBUG-AUTH] ⚠️ 401 CHALLENGE DÉCLENCHÉ : La requête a été rejetée. Raison : " + context.Error + " - " + context.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// 🛡️ CORRECTION SRE : Nginx unifie les ports, on utilise Lax, et on enlève les restrictions "None"
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
            new OpenApiServer { Url = "http://localhost:5050" }
        };
        return Task.CompletedTask;
    });
});

// ❌ Nginx s'occupe du reverse proxy. Le bloc AddCors a été SUPPRIMÉ.

var app = builder.Build();

// --- 3. INITIALISATION DES SEEDERS ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<SrmDbContext>();

    try
    {
        await DataSeeder.SeedLookupDataAsync(context);
        await IdentitySeeder.SeedRolesAndAdminAsync(services);
        Log.Information("Initial seeding completed successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Initial seeding failed.");
    }
}

// --- 4. MIDDLEWARE PIPELINE ---
app.UseHttpMetrics();
app.UseMiddleware<ExceptionMiddleware>();
app.UseSerilogRequestLogging();
app.UseRouting();

// ❌ app.UseCors() a été SUPPRIMÉ.

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

        options.WithPreferredScheme("http");
    });
}

app.MapControllers();
app.MapMetrics();

app.Run();

public partial class Program { }