using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Prometheus;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Srm.Gateway.Api.Middlewares;
using Srm.Gateway.Application.Interfaces;
using Srm.Gateway.Infrastructure;
using Srm.Gateway.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// --- 1. LOGGING CONFIGURATION (SERILOG) ---
// Configure Serilog to write structured JSON logs to the console for Loki/Promtail
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

// --- 2. SERVICE COLLECTION (DEPENDENCY INJECTION) ---
builder.Services.AddControllers();


builder.Services.AddInfrastructure(builder.Configuration);

// Documentation & OpenApi (Scalar)
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        // On remplace les adresses génériques [::] par localhost
        document.Servers = new List<OpenApiServer>
        {
            new OpenApiServer { Url = "http://localhost:5050" }
        };
        return Task.CompletedTask;
    });
});

// CORS (Essential for React Dashboard)
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// --- 3. MIDDLEWARE PIPELINE ORDER (SRE BEST PRACTICES) ---



// B. Monitoring: UseHttpMetrics captures response times and status codes
app.UseHttpMetrics();

// A. Global Exception Handler: MUST be first to catch errors from all subsequent layers
app.UseMiddleware<ExceptionMiddleware>();

// C. Logging: UseSerilogRequestLogging creates a single log entry per request
app.UseSerilogRequestLogging();

// D. Security & Routing
app.UseRouting();
app.UseCors();
app.UseAuthorization();

// --- 4. ENDPOINT MAPPING ---

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // Génère le fichier openapi.json

    // On configure Scalar pour qu'il pointe vers localhost
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("SRM Oriental Gateway API")
               .WithTheme(ScalarTheme.Mars)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);

        // On force l'URL du serveur pour éviter le [::]
        options.WithPreferredScheme("http");
    });
}

// Prometheus Endpoint: Where the scraper collects metrics
app.MapMetrics();

// API Controllers
app.MapControllers();

app.Run();