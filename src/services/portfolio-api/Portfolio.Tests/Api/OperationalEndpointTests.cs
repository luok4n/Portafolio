using System.Net;
using System.Text.Json;
using Portfolio.Tests.Support;

namespace Portfolio.Tests.Api;

/// <summary>
/// Health, correlation ids and error shape — the parts an operator relies on and nobody notices
/// until something is already wrong.
/// </summary>
public sealed class OperationalEndpointTests(PortfolioApp app) : IClassFixture<PortfolioApp>
{
    private readonly PortfolioApp _app = app;

    [Fact]
    public async Task Liveness_answers_without_touching_anything_else()
    {
        using var client = _app.CreateApiClient();

        using var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Readiness_reports_every_supported_language()
    {
        using var client = _app.CreateApiClient();

        using var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // The check exists to catch the case where English loads and Spanish does not — a failure
        // that would otherwise only ever be seen by Spanish-speaking visitors.
        using var document = JsonDocument.Parse(body);
        var data = document.RootElement.GetProperty("checks")[0].GetProperty("data");
        Assert.True(data.TryGetProperty("en", out _));
        Assert.True(data.TryGetProperty("es", out _));
    }

    [Fact]
    public async Task A_supplied_correlation_id_comes_back()
    {
        using var client = _app.CreateApiClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/profile");
        request.Headers.Add("X-Correlation-Id", "trace-from-the-frontend-1");

        using var response = await client.SendAsync(request);

        Assert.Equal("trace-from-the-frontend-1", response.Headers.GetValues("X-Correlation-Id").Single());
    }

    [Fact]
    public async Task A_request_without_one_still_gets_an_id()
    {
        using var client = _app.CreateApiClient();

        using var response = await client.GetAsync(new Uri("/api/profile", UriKind.Relative));

        var id = response.Headers.GetValues("X-Correlation-Id").Single();
        Assert.False(string.IsNullOrWhiteSpace(id));
    }

    [Theory]
    [InlineData("id with spaces")]
    [InlineData("id\nwith-newline")]
    [InlineData("<script>alert(1)</script>")]
    public async Task A_hostile_correlation_id_is_replaced_not_echoed(string hostile)
    {
        // The value reaches log files and a response header, so it is length-capped and restricted
        // to characters that cannot forge either.
        using var client = _app.CreateApiClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/profile");
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", hostile);

        using var response = await client.SendAsync(request);

        var id = response.Headers.GetValues("X-Correlation-Id").Single();
        Assert.NotEqual(hostile, id);
        Assert.DoesNotContain(' ', id);
    }

    [Fact]
    public async Task An_absurdly_long_correlation_id_is_replaced()
    {
        using var client = _app.CreateApiClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/profile");
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", new string('a', 4096));

        using var response = await client.SendAsync(request);

        Assert.True(response.Headers.GetValues("X-Correlation-Id").Single().Length < 200);
    }

    [Fact]
    public async Task The_api_reference_is_not_exposed_outside_development()
    {
        // The fixture runs in Production. Shipping an interactive API explorer to the public is a
        // decision, not a default, and this pins it.
        using var client = _app.CreateApiClient();

        foreach (var url in new[] { "/docs", "/openapi/v1.json" })
        {
            using var response = await client.GetAsync(new Uri(url, UriKind.Relative));
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
