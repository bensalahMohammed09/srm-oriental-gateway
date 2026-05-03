using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Prometheus;
using Microsoft.EntityFrameworkCore; // Nécessaire pour DbUpdateConcurrencyException

namespace Srm.Gateway.Api.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        private static readonly Counter ErrorCounter = Prometheus.Metrics
            .CreateCounter("srm_gateway_errors_total", "Count of unhandled exceptions",
                new CounterConfiguration
                {
                    LabelNames = new[] { "endpoint", "exception_type" }
                });

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                ErrorCounter.WithLabels(context.Request.Path.Value ?? "/unknown", ex.GetType().Name).Inc();

                // On log en Error uniquement si c'est un vrai plantage (500).
                // Pour un 404, un Warning suffit.
                if (ex is KeyNotFoundException)
                    _logger.LogWarning("Ressource non trouvée sur {Path}: {Message}", context.Request.Path, ex.Message);
                else
                    _logger.LogError(ex, "Exception non gérée sur {Path}: {Message}", context.Request.Path, ex.Message);

                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/problem+json";

            // 🌟 CHANGEMENT MAJEUR : Mapping intelligent des statuts HTTP (SRE)
            var statusCode = exception switch
            {
                KeyNotFoundException => (int)HttpStatusCode.NotFound, // 404
                FileNotFoundException => (int)HttpStatusCode.NotFound, // 404
                UnauthorizedAccessException => (int)HttpStatusCode.Forbidden, // 403
                DbUpdateConcurrencyException => (int)HttpStatusCode.Conflict, // 409 (Concurrence)
                InvalidOperationException => (int)HttpStatusCode.BadRequest, // 400
                _ => (int)HttpStatusCode.InternalServerError // 500 par défaut
            };

            context.Response.StatusCode = statusCode;

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = GetTitleForStatusCode(statusCode),
                Detail = exception.Message,
                Instance = context.Request.Path,
                Type = $"https://srm-oriental.ma/errors/{statusCode}"
            };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            return context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
        }

        private static string GetTitleForStatusCode(int statusCode) => statusCode switch
        {
            400 => "Requête invalide (Règle métier)",
            403 => "Accès refusé",
            404 => "Ressource introuvable",
            409 => "Conflit de modification (Concurrence)",
            _ => "Une erreur serveur interne est survenue"
        };
    }
}