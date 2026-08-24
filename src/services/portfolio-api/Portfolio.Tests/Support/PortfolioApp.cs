using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Portfolio.Tests.Support;

/// <summary>
/// Boots the real application in memory, reading the real content files.
/// </summary>
/// <remarks>
/// These tests go through the actual HTTP pipeline rather than calling the query service directly,
/// because most of what can break at this layer only exists there: parameter binding, header
/// binding, the problem-details shape, the status code, the correlation id. A test that calls the
/// service would pass while the endpoint returned 500.
///
/// Content comes from the repository's own <c>content/</c>, so these also act as a check that the
/// published content still satisfies the contract — not just that some fixture does.
/// </remarks>
public sealed class PortfolioApp : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment(Environments.Production);

        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                // The database stays off: this fixture exercises the HTTP surface, and pulling
                // PostgreSQL into every endpoint test would make the fast tests slow for no gain.
                // The database path has its own tests.
                ["Portfolio:Database:Enabled"] = "false",
                ["Portfolio:Content:Path"] = TestContent.Directory,
            }));

        builder.ConfigureLogging(logging => logging.ClearProviders());
    }

    public HttpClient CreateApiClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
    });

    public async Task<T> GetAsync<T>(string url)
    {
        using var client = CreateApiClient();
        var response = await client.GetAsync(new Uri(url, UriKind.Relative)).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>().ConfigureAwait(false)
            ?? throw new InvalidOperationException($"{url} returned null.");
    }
}

/// <summary>
/// Locates the repository's content directory from the test binary.
/// </summary>
/// <remarks>
/// The API project links the content files into its own output, so they are next to the test
/// assembly. Walking up to find the repository root instead would tie the tests to a checkout
/// layout and break the moment they run from anywhere else.
/// </remarks>
public static class TestContent
{
    public static string Directory { get; } = Path.Combine(AppContext.BaseDirectory, "content");

    public static void EnsureAvailable()
    {
        if (!System.IO.Directory.Exists(Directory) ||
            !File.Exists(Path.Combine(Directory, "profile.en.json")))
        {
            throw new InvalidOperationException(
                $"Content not found at {Directory}. The test project references Portfolio.Api, " +
                "which copies content/ to its output; check that link if this fails.");
        }
    }
}
