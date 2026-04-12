using System.Net;
using System.Text.Json;
using Prometheus;

namespace Srm.Gateway.Api.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        // We define the counter here, strictly inside the class scope
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
                // Explicitly use the static counter
                ErrorCounter.WithLabels(context.Request.Path.Value ?? "/unknown", ex.GetType().Name).Inc();

                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);

                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                statusCode = context.Response.StatusCode,
                message = "Internal Server Error!!",
                detailed = exception.Message
            };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            return context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }
}
