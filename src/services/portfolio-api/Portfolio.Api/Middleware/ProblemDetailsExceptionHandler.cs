using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application;
using Portfolio.Infrastructure.Json;

namespace Portfolio.Api.Middleware;

/// <summary>
/// Turns every unhandled exception into an RFC 7807 problem document.
/// </summary>
/// <remarks>
/// One place decides how a failure looks to a caller, so no endpoint has to remember. Only
/// exceptions this application defines on purpose contribute their message to the response —
/// anything unexpected gets a generic title, because a stack trace or a file path is not the
/// public's business. The correlation id is always included, so a report of "it broke" can be tied
/// to the log lines for that exact request.
/// </remarks>
public sealed partial class ProblemDetailsExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<ProblemDetailsExceptionHandler> logger) : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails = problemDetails;
    private readonly ILogger<ProblemDetailsExceptionHandler> _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var (status, title, exposeMessage) = exception switch
        {
            ContentNotFoundException => (StatusCodes.Status404NotFound, "Not found", true),
            InvalidContentException => (StatusCodes.Status500InternalServerError, "Content is not valid", true),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error", false),
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandled(_logger, httpContext.Request.Method, httpContext.Request.Path.Value ?? string.Empty, exception);
        }
        else if (_logger.IsEnabled(LogLevel.Information))
        {
            // Guarded because building the message is work that is wasted when the level is off.
            LogHandled(_logger, httpContext.Request.Method, httpContext.Request.Path.Value ?? string.Empty, exception.Message);
        }

        httpContext.Response.StatusCode = status;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = exposeMessage ? exception.Message : null,
                Instance = httpContext.Request.Path,
                Extensions = { ["correlationId"] = httpContext.TraceIdentifier },
            },
        }).ConfigureAwait(false);
    }

    [LoggerMessage(EventId = 5000, Level = LogLevel.Error, Message = "Unhandled exception on {Method} {Path}.")]
    private static partial void LogUnhandled(ILogger logger, string method, string path, Exception exception);

    [LoggerMessage(EventId = 4000, Level = LogLevel.Information, Message = "Request failed on {Method} {Path}: {Reason}")]
    private static partial void LogHandled(ILogger logger, string method, string path, string reason);
}
