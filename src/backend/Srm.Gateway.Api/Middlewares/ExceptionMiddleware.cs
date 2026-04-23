using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

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
                _logger.LogError(ex, "Exception non gérée sur {Path}: {Message}", context.Request.Path, ex.Message);

                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/problem+json"; // Standard RFC 7807
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            // Utilisation du standard ProblemDetails de ASP.NET Core
            var problemDetails = new ProblemDetails
            {
                Status = context.Response.StatusCode,
                Title = "Une erreur serveur est survenue",
                Detail = exception.Message, // En production, on pourrait masquer ce détail pour la sécurité
                Instance = context.Request.Path,
                Type = "https://srm-oriental.ma/errors/internal-server-error"
            };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            return context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
        }
    }
}