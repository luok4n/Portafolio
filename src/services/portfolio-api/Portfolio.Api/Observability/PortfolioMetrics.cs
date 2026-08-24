using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text;

namespace Portfolio.Api.Observability;

/// <summary>
/// Request count, error count and latency — the three numbers that answer "is it working?".
/// </summary>
/// <remarks>
/// <para>
/// Instrumented with <see cref="System.Diagnostics.Metrics"/>, which is the standard .NET metrics
/// API and the one OpenTelemetry consumes. Adding an OTel exporter later means registering this
/// meter by name; nothing here has to change.
/// </para>
/// <para>
/// The aggregation and the Prometheus rendering are done here rather than by pulling in an exporter
/// package, because there is nowhere to export to. One service, one process, no collector — a full
/// telemetry pipeline configured to scrape itself is ceremony. Section 23 of the development plan
/// says the same thing: OpenTelemetry when the rest of the system is stable and there is a reason.
/// </para>
/// </remarks>
public sealed class PortfolioMetrics : IDisposable
{
    public const string MeterName = "Portfolio.Api";

    /// <summary>
    /// Bucket edges in milliseconds. Chosen around what this service actually does: content is
    /// cached in memory, so a healthy request is single-digit milliseconds and anything past 500ms
    /// means something is wrong rather than merely slow. Prometheus's defaults are tuned for
    /// services with very different shapes.
    /// </summary>
    private static readonly double[] BucketBoundsMs = [1, 5, 10, 25, 50, 100, 250, 500, 1000, 5000];

    private readonly Meter _meter;
    private readonly Counter<long> _requests;
    private readonly Counter<long> _errors;
    private readonly Histogram<double> _duration;

    private readonly Lock _gate = new();
    private readonly long[] _buckets = new long[BucketBoundsMs.Length + 1];
    private readonly Dictionary<int, long> _byStatus = [];
    private long _total;
    private long _errorTotal;
    private double _durationSum;

    public PortfolioMetrics()
    {
        _meter = new Meter(MeterName);
        _requests = _meter.CreateCounter<long>("portfolio.http.requests", "requests", "HTTP requests handled.");
        _errors = _meter.CreateCounter<long>("portfolio.http.errors", "requests", "HTTP requests that failed.");
        _duration = _meter.CreateHistogram<double>("portfolio.http.duration", "ms", "Request duration.");
    }

    public void Record(string method, string route, int statusCode, double elapsedMs)
    {
        var tags = new TagList
        {
            { "method", method },
            { "route", route },
            { "status", statusCode },
        };

        _requests.Add(1, tags);
        _duration.Record(elapsedMs, tags);

        // 4xx is the caller's problem and 5xx is ours. Counting them together would hide a broken
        // deployment behind a crawler probing for /wp-admin.
        var isError = statusCode >= 500;
        if (isError)
        {
            _errors.Add(1, tags);
        }

        lock (_gate)
        {
            _total++;
            _durationSum += elapsedMs;
            if (isError)
            {
                _errorTotal++;
            }

            _byStatus[statusCode] = _byStatus.GetValueOrDefault(statusCode) + 1;

            var bucket = 0;
            while (bucket < BucketBoundsMs.Length && elapsedMs > BucketBoundsMs[bucket])
            {
                bucket++;
            }

            _buckets[bucket]++;
        }
    }

    /// <summary>Renders the Prometheus text exposition format, version 0.0.4.</summary>
    public string Render()
    {
        var text = new StringBuilder();

        lock (_gate)
        {
            text.AppendLine("# HELP portfolio_http_requests_total HTTP requests handled.");
            text.AppendLine("# TYPE portfolio_http_requests_total counter");
            text.Append("portfolio_http_requests_total ").Append(_total.ToString(CultureInfo.InvariantCulture)).AppendLine();

            text.AppendLine("# HELP portfolio_http_errors_total Requests that returned a 5xx.");
            text.AppendLine("# TYPE portfolio_http_errors_total counter");
            text.Append("portfolio_http_errors_total ").Append(_errorTotal.ToString(CultureInfo.InvariantCulture)).AppendLine();

            text.AppendLine("# HELP portfolio_http_responses_total Responses by status code.");
            text.AppendLine("# TYPE portfolio_http_responses_total counter");
            foreach (var (status, count) in _byStatus.OrderBy(p => p.Key))
            {
                text.Append("portfolio_http_responses_total{status=\"")
                    .Append(status.ToString(CultureInfo.InvariantCulture))
                    .Append("\"} ")
                    .Append(count.ToString(CultureInfo.InvariantCulture))
                    .AppendLine();
            }

            text.AppendLine("# HELP portfolio_http_duration_ms Request duration in milliseconds.");
            text.AppendLine("# TYPE portfolio_http_duration_ms histogram");

            // Prometheus histogram buckets are cumulative: each le= is "at most this", not "in this
            // range". Emitting per-bucket counts here is the classic way to produce a histogram that
            // renders as nonsense in every dashboard built on it.
            long cumulative = 0;
            for (var i = 0; i < BucketBoundsMs.Length; i++)
            {
                cumulative += _buckets[i];
                text.Append("portfolio_http_duration_ms_bucket{le=\"")
                    .Append(BucketBoundsMs[i].ToString(CultureInfo.InvariantCulture))
                    .Append("\"} ")
                    .Append(cumulative.ToString(CultureInfo.InvariantCulture))
                    .AppendLine();
            }

            cumulative += _buckets[^1];
            text.Append("portfolio_http_duration_ms_bucket{le=\"+Inf\"} ")
                .Append(cumulative.ToString(CultureInfo.InvariantCulture)).AppendLine();
            text.Append("portfolio_http_duration_ms_sum ")
                .Append(_durationSum.ToString("F3", CultureInfo.InvariantCulture)).AppendLine();
            text.Append("portfolio_http_duration_ms_count ")
                .Append(_total.ToString(CultureInfo.InvariantCulture)).AppendLine();
        }

        return text.ToString();
    }

    public void Dispose() => _meter.Dispose();
}
