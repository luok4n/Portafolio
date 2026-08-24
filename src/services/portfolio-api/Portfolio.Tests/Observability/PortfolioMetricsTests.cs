using Portfolio.Api.Observability;

namespace Portfolio.Tests.Observability;

/// <summary>
/// The Prometheus rendering, which is the part that is easy to get subtly wrong and impossible to
/// notice: a malformed histogram does not error, it just draws a graph that lies.
/// </summary>
public sealed class PortfolioMetricsTests
{
    private static PortfolioMetrics WithRequests(params (int Status, double Ms)[] requests)
    {
        var metrics = new PortfolioMetrics();
        foreach (var (status, ms) in requests)
        {
            metrics.Record("GET", "/api/content", status, ms);
        }

        return metrics;
    }

    [Fact]
    public void Counts_every_request()
    {
        using var metrics = WithRequests((200, 1), (200, 2), (404, 3));

        Assert.Contains("portfolio_http_requests_total 3", metrics.Render(), StringComparison.Ordinal);
    }

    [Fact]
    public void Counts_only_server_errors_as_errors()
    {
        // A crawler probing for /wp-admin generates 404s all day. Counting those as errors would
        // hide a genuinely broken deployment in the noise.
        using var metrics = WithRequests((200, 1), (404, 1), (429, 1), (500, 1), (503, 1));

        var rendered = metrics.Render();

        Assert.Contains("portfolio_http_errors_total 2", rendered, StringComparison.Ordinal);
        Assert.Contains("portfolio_http_requests_total 5", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Breaks_responses_down_by_status()
    {
        using var metrics = WithRequests((200, 1), (200, 1), (404, 1));

        var rendered = metrics.Render();

        Assert.Contains("portfolio_http_responses_total{status=\"200\"} 2", rendered, StringComparison.Ordinal);
        Assert.Contains("portfolio_http_responses_total{status=\"404\"} 1", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Histogram_buckets_are_cumulative()
    {
        // This is the bug worth a test. Prometheus reads `le=` as "at most this", not "in this
        // range". Emitting per-bucket counts produces a histogram that renders as nonsense in every
        // dashboard built on it, and nothing anywhere reports an error.
        using var metrics = WithRequests((200, 0.5), (200, 3), (200, 30), (200, 2000));

        var rendered = metrics.Render();

        Assert.Contains("portfolio_http_duration_ms_bucket{le=\"1\"} 1", rendered, StringComparison.Ordinal);
        Assert.Contains("portfolio_http_duration_ms_bucket{le=\"5\"} 2", rendered, StringComparison.Ordinal);
        Assert.Contains("portfolio_http_duration_ms_bucket{le=\"50\"} 3", rendered, StringComparison.Ordinal);
        Assert.Contains("portfolio_http_duration_ms_bucket{le=\"5000\"} 4", rendered, StringComparison.Ordinal);
        Assert.Contains("portfolio_http_duration_ms_bucket{le=\"+Inf\"} 4", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void The_infinity_bucket_always_equals_the_count()
    {
        // Prometheus rejects a histogram where they disagree, and a value past the last bound must
        // still land somewhere.
        using var metrics = WithRequests((200, 0.1), (200, 99_999));

        var rendered = metrics.Render();

        Assert.Contains("portfolio_http_duration_ms_bucket{le=\"+Inf\"} 2", rendered, StringComparison.Ordinal);
        Assert.Contains("portfolio_http_duration_ms_count 2", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_the_duration_sum_with_an_invariant_decimal_point()
    {
        // A comma decimal separator makes the whole exposition unparseable, and this project runs on
        // a machine whose culture uses one.
        using var metrics = WithRequests((200, 1.5), (200, 2.25));

        Assert.Contains("portfolio_http_duration_ms_sum 3.750", metrics.Render(), StringComparison.Ordinal);
    }

    [Fact]
    public void Declares_a_type_for_every_metric()
    {
        using var metrics = WithRequests((200, 1));

        var rendered = metrics.Render();

        Assert.Contains("# TYPE portfolio_http_requests_total counter", rendered, StringComparison.Ordinal);
        Assert.Contains("# TYPE portfolio_http_errors_total counter", rendered, StringComparison.Ordinal);
        Assert.Contains("# TYPE portfolio_http_duration_ms histogram", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void An_untouched_service_still_renders_valid_output()
    {
        // Scraped before the first request, which is exactly what happens on startup.
        using var metrics = new PortfolioMetrics();

        var rendered = metrics.Render();

        Assert.Contains("portfolio_http_requests_total 0", rendered, StringComparison.Ordinal);
        Assert.Contains("portfolio_http_duration_ms_count 0", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Recording_from_many_threads_loses_nothing()
    {
        using var metrics = new PortfolioMetrics();

        Parallel.For(0, 500, i => metrics.Record("GET", "/api/content", 200, i % 10));

        Assert.Contains("portfolio_http_requests_total 500", metrics.Render(), StringComparison.Ordinal);
    }
}
