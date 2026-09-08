using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace proyectosena.Middleware
{
    /// <summary>
    /// Single place where every unhandled exception ends up.
    /// Writes the full detail to the log and returns a generic ProblemDetails
    /// to the caller, so internal information never reaches the browser.
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            // TraceIdentifier is unique per request. It is the only piece of the
            // log that we hand to the caller, so they can quote it when reporting.
            var traceId = httpContext.TraceIdentifier;

            _logger.LogError(
                exception,
                "Unhandled exception on {Method} {Path}. TraceId: {TraceId}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                traceId);

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Ocurrió un error inesperado. Por favor intente más tarde.",
                Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
            };

            // Lets the user quote a code that points to the exact log entry
            problem.Extensions["traceId"] = traceId;

            httpContext.Response.StatusCode = problem.Status.Value;
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

            // true = handled, do not let the exception bubble up any further
            return true;
        }
    }
}
