namespace Portfolio.Api.Middleware;

/// <summary>
/// Gives every request an id, echoes it back, and puts it in the log scope.
/// </summary>
/// <remarks>
/// When something goes wrong in production, the useful question is "what else happened during that
/// request". Without a correlation id, answering it means guessing from timestamps. The id is
/// accepted from the caller when supplied so a trace can span the frontend and the API, and
/// generated otherwise.
/// </remarks>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";

    private const int MaxLength = 128;

    private readonly RequestDelegate _next = next;
    private readonly ILogger<CorrelationIdMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = Sanitise(context.Request.Headers[HeaderName]) ?? context.TraceIdentifier;
        context.TraceIdentifier = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// A caller-supplied id ends up in log files and in a response header, so it is length-capped
    /// and restricted to characters that cannot forge a header or a log line.
    /// </summary>
    private static string? Sanitise(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength)
        {
            return null;
        }

        foreach (var c in value)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_' or '.' or ':'))
            {
                return null;
            }
        }

        return value;
    }
}
