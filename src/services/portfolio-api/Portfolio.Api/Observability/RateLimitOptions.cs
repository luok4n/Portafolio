namespace Portfolio.Api.Observability;

/// <summary>
/// How much traffic one caller may send.
/// </summary>
/// <remarks>
/// Bound through the options system and read per request rather than once while the host is being
/// built. Reading configuration eagerly in <c>Program.cs</c> looks equivalent and is not: sources
/// added after that point — which is how the integration tests supply a limit low enough to reach —
/// are simply not there yet, so the value silently stays at its default and the limiter appears not
/// to work at all.
/// </remarks>
public sealed class RateLimitOptions
{
    public const string SectionName = "Portfolio:RateLimit";

    /// <summary>
    /// Deliberately far above what reading the site produces: the whole page is one request, so a
    /// caller who trips this was not reading. It exists so one badly written scraper cannot spend
    /// the compute budget of whatever this is deployed on.
    /// </summary>
    public int PermitLimit { get; set; } = 300;

    public int WindowSeconds { get; set; } = 60;
}
