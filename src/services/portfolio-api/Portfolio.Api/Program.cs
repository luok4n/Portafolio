using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Portfolio.Api.Endpoints;
using Portfolio.Api.Health;
using Portfolio.Api.Middleware;
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
        .Expire(TimeSpan.FromMinutes(5))
        .SetVaryByQuery("lang")
        .SetVaryByHeader("Accept-Language")));

var app = builder.Build();

// Schema and content before the first request is served, so a fresh environment comes up ready
// rather than healthy-but-empty. A no-op when the file source is in use.
await app.Services.InitialisePortfolioDatabaseAsync().ConfigureAwait(false);

// --- pipeline --------------------------------------------------------------------------------
app.UseExceptionHandler();

// Registered after the exception handler so it runs inside it: by the time an exception bubbles
// back up, the request already carries its correlation id and the problem document can quote it.
app.UseMiddleware<CorrelationIdMiddleware>();
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

app.MapPortfolioEndpoints();

await app.RunAsync().ConfigureAwait(false);

/// <summary>Exposed so the integration tests in phase 6 can boot the real application.</summary>
public partial class Program;
