using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Portfolio.Application.Abstractions;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Api.Health;

/// <summary>
/// Readiness: can the service actually serve content, in every language it claims to support?
/// </summary>
/// <remarks>
/// A process that started but cannot read its content is worse than one that is down — it returns
/// errors while looking healthy. Checking every supported language catches the case where English
/// loads and Spanish does not, which would otherwise only surface for Spanish-speaking visitors.
/// </remarks>
public sealed class ContentHealthCheck(IPortfolioContentSource source) : IHealthCheck
{
    private readonly IPortfolioContentSource _source = source;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>(StringComparer.Ordinal);

        foreach (var language in LanguageCode.Supported)
        {
            try
            {
                var content = await _source.GetAsync(language, cancellationToken).ConfigureAwait(false);
                if (content.Experience.Count == 0)
                {
                    return HealthCheckResult.Unhealthy($"Content for '{language}' has no experience entries.", data: data);
                }

                data[language.Value] = $"{content.Experience.Count} roles, {content.Projects.Count} projects";
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return HealthCheckResult.Unhealthy($"Content for '{language}' failed to load.", ex, data);
            }
        }

        return HealthCheckResult.Healthy("Content loaded for every supported language.", data);
    }
}

public static class HealthResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(report);

        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            durationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                data = e.Value.Data,
            }),
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonSerializerOptions.Web), context.RequestAborted);
    }
}
