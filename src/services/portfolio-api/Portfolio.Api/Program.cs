using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Portfolio.Api.Endpoints;
using Portfolio.Api.Health;
using Portfolio.Api.Middleware;
using Portfolio.Api.Observability;
using Portfolio.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// --- logging ---------------------------------------------------------------------------------
// Structured JSON on the console. Whatever runs this in phase 13 — a container platform, a PaaS —
// collects stdout, so writing structured lines there is enough and avoids a logging dependency the
// project does not otherwise need.
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;
});

// --- services --------------------------------------------------------------------------------
builder.Services.AddPortfolioInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddResponseCompression();

builder.Services.AddHealthChecks()
    .AddCheck<ContentHealthCheck>("content", tags: ["ready"]);

var allowedOrigins = builder.Configuration.GetSection("Portfolio:Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    // An empty list means no cross-origin access rather than any origin: the safe default is the
    // restrictive one, and the frontend is same-origin behind nginx in production anyway.
    policy.WithOrigins(allowedOrigins)
          .WithMethods("GET")
          .WithHeaders("Accept-Language", "Content-Type", CorrelationIdMiddleware.HeaderName)
          .WithExposedHeaders(CorrelationIdMiddleware.HeaderName);
}));

builder.Services.AddOutputCache(options =>
    options.AddBasePolicy(policy => policy
        // Content only. A base policy with no predicate caches every GET, which quietly included
        // /health and /metrics: an orchestrator would have gone on reading a five-minute-old
        // "Healthy" after the service stopped being healthy, and a scraper would have recorded the
        // same counters over and over. Found by a metrics test whose counter refused to move.
        .With(context => context.HttpContext.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        .Expire(TimeSpan.FromMinutes(5))
        .SetVaryByQuery("lang")
        .SetVaryByHeader("Accept-Language")));

builder.Services.AddSingleton<PortfolioMetrics>();

// --- rate limiting -----------------------------------------------------------------------------
// The API is read-only and its answers are cached, so this is not about protecting a database. It is
// about one badly written scraper not being able to spend the compute budget of whatever this ends
// up deployed on.
builder.Services.AddOptions<RateLimitOptions>()
    .Bind(builder.Configuration.GetSection(RateLimitOptions.SectionName));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // Health and metrics are for the platform, not for callers. Limiting them would let a burst
        // of traffic convince an orchestrator the service is down and restart a healthy process.
        if (context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) ||
            context.Request.Path.StartsWithSegments("/metrics", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetNoLimiter("infrastructure");
        }

        // Resolved per request, not captured at startup — see RateLimitOptions for why that
        // distinction is not cosmetic.
        var limits = context.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;

        // Behind nginx every request arrives from the proxy, so the forwarded address is what
        // distinguishes callers. Absent that, everything shares one partition — the safe direction
        // to be wrong in.
        var caller = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(caller, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limits.PermitLimit,
            Window = TimeSpan.FromSeconds(limits.WindowSeconds),
            QueueLimit = 0,
        });
    });

    options.OnRejected = async (context, token) =>
    {
        var limits = context.HttpContext.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;

        // Without Retry-After a client has no way to back off correctly, so it retries immediately
        // and makes the situation worse.
        context.HttpContext.Response.Headers.RetryAfter =
            limits.WindowSeconds.ToString(CultureInfo.InvariantCulture);
        await context.HttpContext.Response.WriteAsync("Too many requests.", token).ConfigureAwait(false);
    };
});

var app = builder.Build();

// Schema and content before the first request is served, so a fresh environment comes up ready
// rather than healthy-but-empty. A no-op when the file source is in use.
await app.Services.InitialisePortfolioDatabaseAsync().ConfigureAwait(false);

// --- pipeline --------------------------------------------------------------------------------
app.UseExceptionHandler();

// Registered after the exception handler so it runs inside it: by the time an exception bubbles
// back up, the request already carries its correlation id and the problem document can quote it.
app.UseMiddleware<CorrelationIdMiddleware>();

// Outside the rate limiter and the cache, so the measurement covers what the caller actually
// waited for — including a rejection and including a cache hit. Timing only the handler would
// report a service that is fast at everything except answering.
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseRateLimiter();
app.UseResponseCompression();
app.UseCors();
app.UseOutputCache();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/docs", options => options
        .WithTitle("Portfolio API")
        .WithTheme(ScalarTheme.BluePlanet));
    app.MapGet("/", () => Results.Redirect("/docs")).ExcludeFromDescription();
}

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    // Liveness answers "is the process wedged?", so it must not depend on anything else. A content
    // problem should not make an orchestrator restart a perfectly healthy process.
    Predicate = _ => false,
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthResponseWriter.WriteAsync,
});

// Prometheus text format. Not proxied by nginx — the public surface is `/api` and the static files,
// so this is reachable only from inside the network, which is where a scraper lives anyway.
app.MapGet("/metrics", (PortfolioMetrics metrics) =>
        Results.Text(metrics.Render(), "text/plain; version=0.0.4; charset=utf-8"))
    .ExcludeFromDescription();

app.MapPortfolioEndpoints();

await app.RunAsync().ConfigureAwait(false);

/// <summary>Exposed so the integration tests in phase 6 can boot the real application.</summary>
public partial class Program;
