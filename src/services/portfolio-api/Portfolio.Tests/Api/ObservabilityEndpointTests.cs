using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Portfolio.Tests.Support;

namespace Portfolio.Tests.Api;

public sealed class MetricsEndpointTests(PortfolioApp app) : IClassFixture<PortfolioApp>
{
    private readonly PortfolioApp _app = app;

    [Fact]
    public async Task Metrics_are_exposed_in_the_prometheus_text_format()
    {
        using var client = _app.CreateApiClient();
        await client.GetAsync(new Uri("/api/profile", UriKind.Relative));

        using var response = await client.GetAsync(new Uri("/metrics", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("portfolio_http_requests_total", body, StringComparison.Ordinal);
        Assert.Contains("portfolio_http_duration_ms_count", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Traffic_moves_the_counters()
    {
        using var client = _app.CreateApiClient();

        var before = await ReadCounterAsync(client, "portfolio_http_requests_total");
        await client.GetAsync(new Uri("/api/skills", UriKind.Relative));
        var after = await ReadCounterAsync(client, "portfolio_http_requests_total");

        Assert.True(after > before, $"expected the counter to advance, was {before} then {after}");
    }

    [Fact]
    public async Task A_missing_project_is_not_counted_as_a_server_error()
    {
        using var client = _app.CreateApiClient();

        var before = await ReadCounterAsync(client, "portfolio_http_errors_total");
        await client.GetAsync(new Uri("/api/projects/does-not-exist", UriKind.Relative));
        var after = await ReadCounterAsync(client, "portfolio_http_errors_total");

        // Someone asking for a page that does not exist is not an outage.
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task Metrics_are_not_part_of_the_documented_api()
    {
        // Excluded from the OpenAPI document on purpose: it is an operational surface, and nginx
        // does not proxy it, so it is not reachable from outside the network either.
        using var client = _app.CreateApiClient();

        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<long> ReadCounterAsync(HttpClient client, string name)
    {
        var body = await client.GetStringAsync(new Uri("/metrics", UriKind.Relative));
        var line = body.Split('\n').First(l => l.StartsWith($"{name} ", StringComparison.Ordinal));
        return long.Parse(line.Split(' ')[1].Trim(), System.Globalization.CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// The same application with a limit low enough to reach in a test. The production default is far
/// above what a reader produces, which is exactly why it cannot be exercised as configured.
/// </summary>
public sealed class ThrottledApp : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment(Environments.Production);
        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Portfolio:Database:Enabled"] = "false",
                ["Portfolio:Content:Path"] = TestContent.Directory,
                ["Portfolio:RateLimit:PermitLimit"] = "3",
                ["Portfolio:RateLimit:WindowSeconds"] = "60",
            }));
        builder.ConfigureLogging(logging => logging.ClearProviders());
    }
}

public sealed class RateLimitTests(ThrottledApp app) : IClassFixture<ThrottledApp>
{
    private readonly ThrottledApp _app = app;

    [Fact]
    public async Task A_caller_over_the_limit_is_rejected_with_a_retry_hint()
    {
        using var client = _app.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 6; i++)
        {
            using var response = await client.GetAsync(new Uri("/api/profile", UriKind.Relative));
            statuses.Add(response.StatusCode);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                // Without Retry-After a client has no way to back off correctly, so it retries
                // immediately and makes the situation worse.
                Assert.True(response.Headers.Contains("Retry-After"));
            }
        }

        Assert.Contains(HttpStatusCode.OK, statuses);
        Assert.Contains(HttpStatusCode.TooManyRequests, statuses);
    }

    [Fact]
    public async Task Health_checks_are_never_rate_limited()
    {
        // A burst of traffic must not make an orchestrator conclude the service is down and restart
        // a process that is working fine.
        using var client = _app.CreateClient();

        for (var i = 0; i < 12; i++)
        {
            using var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task Metrics_scraping_is_never_rate_limited()
    {
        using var client = _app.CreateClient();

        for (var i = 0; i < 12; i++)
        {
            using var response = await client.GetAsync(new Uri("/metrics", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
