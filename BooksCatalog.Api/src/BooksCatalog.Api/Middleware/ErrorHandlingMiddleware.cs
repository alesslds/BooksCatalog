using BooksCatalog.Domain.Exceptions;
using System.Text.Json;

namespace BooksCatalog.Api.Middleware
{
    public sealed class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (DomainValidationException ex)
            {
                _logger.LogWarning(ex, "Domain validation error");
                await WriteProblemDetailsAsync(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Validation error",
                    ex.Message,
                    new Dictionary<string, object?>
                    {
                        ["errors"] = ex.Errors
                    });
            }
            catch (NotFoundException ex)
            {
                _logger.LogInformation(ex, "Resource not found");
                await WriteProblemDetailsAsync(
                    context,
                    StatusCodes.Status404NotFound,
                    "Not found",
                    ex.Message,
                    null);
            }
            catch (ConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict");
                await WriteProblemDetailsAsync(
                    context,
                    StatusCodes.Status409Conflict,
                    "Concurrency conflict",
                    ex.Message,
                    null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                await WriteProblemDetailsAsync(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "Internal server error",
                    "An unexpected error occurred.",
                    null);
            }
        }

        private static async Task WriteProblemDetailsAsync(
            HttpContext context,
            int statusCode,
            string title,
            string detail,
            IDictionary<string, object?>? extensions)
        {
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = statusCode;

            var problem = new Dictionary<string, object?>
            {
                ["type"] = "about:blank",
                ["title"] = title,
                ["status"] = statusCode,
                ["detail"] = detail,
                ["instance"] = context.Request.Path.Value
            };

            if (extensions is not null)
            {
                foreach (var kvp in extensions)
                {
                    problem[kvp.Key] = kvp.Value;
                }
            }

            var json = JsonSerializer.Serialize(problem);
            await context.Response.WriteAsync(json);
        }
    }
}
