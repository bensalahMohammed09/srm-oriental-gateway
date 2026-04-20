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
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["SRM_AUTH_TOKEN"];
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// --- TON BLOC OPENAPI (REPRIS À L'IDENTIQUE) ---
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

builder.Services.AddCors(options => {
    options.AddDefaultPolicy(p =>
        p.WithOrigins("http://localhost:3000")
         .AllowAnyMethod()
         .AllowAnyHeader()
         .AllowCredentials());
});

var app = builder.Build();

// --- INITIALISATION DU SEEDER ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try { await IdentitySeeder.SeedRolesAndAdminAsync(services); }
    catch (Exception ex) { Log.Error(ex, "Seeding failure"); }
}

// --- 3. MIDDLEWARE PIPELINE ---
app.UseHttpMetrics();
app.UseMiddleware<ExceptionMiddleware>();
app.UseSerilogRequestLogging();
app.UseRouting();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// --- TON BLOC ENDPOINT MAPPING (REPRIS À L'IDENTIQUE) ---
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