using System.Diagnostics;

namespace Portfolio.Api.Observability;

/// <summary>
/// One structured line per request, and the measurement behind the metrics.
/// </summary>
/// <remarks>
/// <para>
/// The fields are the ones an incident actually needs: method, route, status, duration and the
/// correlation id. The <b>route template</b> is recorded rather than the raw path, so a thousand
/// requests for different projects aggregate into one series instead of a thousand — the mistake
/// that turns a metrics backend into a bill.
/// </para>
/// <para>
/// Health checks are logged at Debug. They run every fifteen seconds forever, and at Information
/// they would bury every line that matters.
/// </para>
/// </remarks>
public sealed partial class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger,
    PortfolioMetrics metrics)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<RequestLoggingMiddleware> _logger = logger;
    private readonly PortfolioMetrics _metrics = metrics;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var timestamp = Stopwatch.GetTimestamp();

        try
        {
            await _next(context).ConfigureAwait(false);
        }
        finally
        {
            var elapsedMs = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
            var route = RouteOf(context);
            var status = context.Response.StatusCode;

            _metrics.Record(context.Request.Method, route, status, elapsedMs);

            if (IsHealthCheck(context))
            {
                LogHealth(_logger, route, status, elapsedMs);
            }
            else if (status >= StatusCodes.Status500InternalServerError)
            {
                LogServerError(_logger, context.Request.Method, route, status, elapsedMs);
            }
            else
            {
                LogRequest(_logger, context.Request.Method, route, status, elapsedMs);
            }
        }
    }

    /// <summary>
    /// The route template when one matched, so <c>/api/projects/slang</c> and
    /// <c>/api/projects/moa</c> share a series. Falls back to a literal for unmatched paths, which
    /// are bounded by whatever a crawler tries and are worth seeing individually anyway.
    /// </summary>
    private static string RouteOf(HttpContext context)
    {
        var pattern = context.GetEndpoint()?.Metadata
            .GetMetadata<Microsoft.AspNetCore.Routing.RouteNameMetadata>()?.RouteName;

        if (!string.IsNullOrEmpty(pattern))
        {
            return pattern;
        }

        var endpoint = context.GetEndpoint() as Microsoft.AspNetCore.Routing.RouteEndpoint;
        return endpoint?.RoutePattern.RawText ?? "(unmatched)";
    }

    private static bool IsHealthCheck(HttpContext context) =>
        context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);

    [LoggerMessage(EventId = 3000, Level = LogLevel.Information,
        Message = "{Method} {Route} responded {StatusCode} in {ElapsedMs:F1}ms")]
    private static partial void LogRequest(ILogger logger, string method, string route, int statusCode, double elapsedMs);

    [LoggerMessage(EventId = 3001, Level = LogLevel.Error,
        Message = "{Method} {Route} responded {StatusCode} in {ElapsedMs:F1}ms")]
    private static partial void LogServerError(ILogger logger, string method, string route, int statusCode, double elapsedMs);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Debug,
        Message = "health {Route} responded {StatusCode} in {ElapsedMs:F1}ms")]
    private static partial void LogHealth(ILogger logger, string route, int statusCode, double elapsedMs);
}
