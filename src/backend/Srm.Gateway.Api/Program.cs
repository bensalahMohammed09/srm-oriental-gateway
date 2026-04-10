using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Srm.Gateway.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<SrmDbContext>(options =>
    options.UseNpgsql(connectionString)
    .UseSnakeCaseNamingConvention());

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// CORS for React Dashboard
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// Healthckeck 
builder.Services.AddHealthChecks();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Database Initialization (Seed data )
using (var scope = app.Services.CreateScope())
{
    var srmContext = scope.ServiceProvider.GetRequiredService<SrmDbContext>();
    DbInitializer.Seed(srmContext);
}

app.MapHealthChecks("/health");

app.Run();
