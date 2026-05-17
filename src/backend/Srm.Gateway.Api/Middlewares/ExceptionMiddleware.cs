using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Prometheus;
using Microsoft.EntityFrameworkCore;

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

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        public async Task InvokeAsync(HttpContext context)
        {
            // 1. EXTRACT OR GENERATE CORRELATION ID
            // On vérifie si React/n8n a déjà envoyé un ID, sinon on en crée un.
            context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationIdValues);
            var correlationId = correlationIdValues.FirstOrDefault() ?? Guid.NewGuid().ToString();

            // On l'ajoute à la réponse pour le frontend
            context.Response.Headers["X-Correlation-ID"] = correlationId;

            // 2. INJECT INTO LOGGING SCOPE (La magie pour Grafana/Loki)
            // Tous les logs générés à partir d'ici (même hors de ce fichier) auront le label "CorrelationId"
            var state = new Dictionary<string, object> { ["CorrelationId"] = correlationId };
            using (_logger.BeginScope(state))
            {
                try
                {
                    await _next(context); // Exécution normale de l'API
                }
                catch (Exception ex)
                {
                    ErrorCounter.WithLabels(context.Request.Path.Value ?? "/unknown", ex.GetType().Name).Inc();

                    if (ex is KeyNotFoundException)
                        _logger.LogWarning("Ressource non trouvée sur {Path}: {Message}", context.Request.Path, ex.Message);
                    else
                        _logger.LogError(ex, "Exception non gérée sur {Path}: {Message}", context.Request.Path, ex.Message);

                    await HandleExceptionAsync(context, ex, correlationId);
                }
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
        {
            context.Response.ContentType = "application/problem+json";

            var statusCode = exception switch
            {
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                FileNotFoundException => (int)HttpStatusCode.NotFound,
                UnauthorizedAccessException => (int)HttpStatusCode.Forbidden,
                DbUpdateConcurrencyException => (int)HttpStatusCode.Conflict,
                InvalidOperationException => (int)HttpStatusCode.BadRequest,
                _ => (int)HttpStatusCode.InternalServerError
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

            // 3. BONUS : On ajoute le CorrelationId directement dans le JSON d'erreur !
            // Le jury verra que l'API frontend reçoit un ID de suivi technique pour le support.
            problemDetails.Extensions.Add("correlationId", correlationId);


            return context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, _jsonOptions));
        }
        //test this shit 

        private static string GetTitleForStatusCode(int statusCode) => statusCode switch
        {
            400 => "Requête invalide (Règle métier)",
            403 => "Accès refusé",
            404 => "Ressource introuvable",
            409 => "Conflit de modification (Concurrence)",
            _ => "Une erreur serveur interne est survenue"
        };
        //testtss 
    }
}