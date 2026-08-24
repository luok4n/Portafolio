using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Portfolio.Application.Contracts;
using Portfolio.Tests.Support;

namespace Portfolio.Tests.Api;

/// <summary>
/// The HTTP contract, exercised through the real pipeline against the real content.
/// </summary>
public sealed class ContentEndpointTests(PortfolioApp app) : IClassFixture<PortfolioApp>
{
    private readonly PortfolioApp _app = app;

    [Fact]
    public async Task The_bundle_returns_everything_the_site_needs()
    {
        var content = await _app.GetAsync<PortfolioContentDto>("/api/content");

        Assert.NotEmpty(content.Experience);
        Assert.NotEmpty(content.Projects);
        Assert.NotEmpty(content.Skills);
        Assert.NotEmpty(content.Education);
        Assert.NotEmpty(content.SocialLinks);
        Assert.False(string.IsNullOrWhiteSpace(content.Profile.Name));
    }

    [Theory]
    [InlineData("en", "en")]
    [InlineData("es", "es")]
    [InlineData("es-CO", "es")]
    [InlineData("EN", "en")]
    public async Task An_explicit_language_is_honoured(string requested, string expected)
    {
        var content = await _app.GetAsync<PortfolioContentDto>($"/api/content?lang={requested}");

        Assert.Equal(expected, content.Language.Resolved);
        Assert.Equal("explicit", content.Language.ResolvedFrom);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("klingon")]
    [InlineData("")]
    public async Task An_unsupported_language_falls_back_instead_of_failing(string requested)
    {
        // A page is public. A language nobody supports is not a reason to refuse to serve it.
        var content = await _app.GetAsync<PortfolioContentDto>($"/api/content?lang={requested}");

        Assert.Equal("en", content.Language.Resolved);
        Assert.Equal("fallback", content.Language.ResolvedFrom);
    }

    [Fact]
    public async Task The_accept_language_header_is_used_when_no_parameter_is_given()
    {
        using var client = _app.CreateApiClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/content");
        request.Headers.Add("Accept-Language", "es-CO,es;q=0.9,en;q=0.8");

        using var response = await client.SendAsync(request);
        var content = await response.Content.ReadFromJsonAsync<PortfolioContentDto>();

        Assert.Equal("es", content!.Language.Resolved);
        Assert.Equal("accept-header", content.Language.ResolvedFrom);
    }

    [Fact]
    public async Task An_explicit_parameter_beats_the_header()
    {
        using var client = _app.CreateApiClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/content?lang=en");
        request.Headers.Add("Accept-Language", "es");

        using var response = await client.SendAsync(request);
        var content = await response.Content.ReadFromJsonAsync<PortfolioContentDto>();

        Assert.Equal("en", content!.Language.Resolved);
    }

    [Fact]
    public async Task A_malformed_accept_language_header_still_returns_a_page()
    {
        using var client = _app.CreateApiClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/content");
        // Some client sending a broken header is not a reason to fail a request for a public page.
        request.Headers.TryAddWithoutValidation("Accept-Language", ";;;q=bogus,,,");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task The_two_languages_differ_in_prose_and_agree_on_facts()
    {
        var en = await _app.GetAsync<PortfolioContentDto>("/api/content?lang=en");
        var es = await _app.GetAsync<PortfolioContentDto>("/api/content?lang=es");

        // Same career, told twice.
        Assert.Equal(en.Experience.Count, es.Experience.Count);
        Assert.Equal(en.Profile.MonthsOfExperience, es.Profile.MonthsOfExperience);

        for (var i = 0; i < en.Experience.Count; i++)
        {
            Assert.Equal(en.Experience[i].Id, es.Experience[i].Id);
            Assert.Equal(en.Experience[i].Start, es.Experience[i].Start);
            Assert.Equal(en.Experience[i].End, es.Experience[i].End);
            Assert.Equal(en.Experience[i].Highlights.Count, es.Experience[i].Highlights.Count);
        }

        Assert.NotEqual(en.Profile.Summary, es.Profile.Summary);
    }

    [Fact]
    public async Task Years_of_experience_are_derived_from_the_periods_that_are_served()
    {
        var content = await _app.GetAsync<PortfolioContentDto>("/api/content");

        // Recomputed from the same payload the client receives. If the API ever starts reporting a
        // stored number, this catches it — the whole point of the rule is that it cannot go stale.
        var months = new HashSet<int>();
        foreach (var role in content.Experience)
        {
            var start = Ordinal(role.Start);
            var end = Ordinal(role.End);
            for (var m = start; m <= end; m++)
            {
                months.Add(m);
            }
        }

        Assert.Equal(months.Count, content.Profile.MonthsOfExperience);
        Assert.Equal(months.Count / 12, content.Profile.YearsOfExperience);
        Assert.Contains($"{content.Profile.YearsOfExperience}", content.Profile.Summary);

        static int Ordinal(string yearMonth)
        {
            var parts = yearMonth.Split('-');
            return (int.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture) * 12)
                 + int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    [Fact]
    public async Task Concurrent_roles_point_at_each_other()
    {
        var content = await _app.GetAsync<PortfolioContentDto>("/api/content");
        var concurrent = content.Experience.Where(e => e.Concurrent).ToList();

        Assert.NotEmpty(concurrent);

        // A one-sided link would render as a role claiming a partner that does not claim it back.
        foreach (var role in concurrent)
        {
            foreach (var otherId in role.ParallelWith)
            {
                var other = content.Experience.Single(e => e.Id == otherId);
                Assert.Contains(role.Id, other.ParallelWith);
            }
        }
    }

    [Fact]
    public async Task Every_project_belongs_to_a_role_that_lists_it()
    {
        var content = await _app.GetAsync<PortfolioContentDto>("/api/content");

        foreach (var project in content.Projects)
        {
            var role = content.Experience.SingleOrDefault(e => e.Id == project.ExperienceId);
            Assert.NotNull(role);
            Assert.Contains(project.Id, role.ProjectIds);
            Assert.Equal(role.Company, project.Company);
        }
    }

    [Fact]
    public async Task A_project_claiming_a_public_source_actually_cites_one()
    {
        var content = await _app.GetAsync<PortfolioContentDto>("/api/content");

        // The flag is the site's credibility claim; serving it without a citation would make the
        // "Sources" block on the page a lie of omission.
        foreach (var project in content.Projects.Where(p => p.PubliclySourced))
        {
            Assert.NotEmpty(project.Sources);
            Assert.All(project.Sources, s => Assert.StartsWith("https://", s.Url, StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task A_single_project_can_be_fetched_by_id()
    {
        var all = await _app.GetAsync<IReadOnlyList<ProjectDto>>("/api/projects");
        var expected = all[0];

        var one = await _app.GetAsync<ProjectDto>($"/api/projects/{expected.Id}");

        Assert.Equal(expected.Id, one.Id);
        Assert.Equal(expected.Name, one.Name);
    }

    [Fact]
    public async Task An_unknown_project_is_a_problem_document_not_a_crash()
    {
        using var client = _app.CreateApiClient();

        using var response = await client.GetAsync(new Uri("/api/projects/no-such-project", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(404, document.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("correlationId").GetString()));
    }

    [Fact]
    public async Task Only_public_links_are_ever_served()
    {
        var links = await _app.GetAsync<IReadOnlyList<SocialLinkDto>>("/api/social-links");

        Assert.NotEmpty(links);

        // The filter lives in the query service so a template cannot leak a private detail by
        // forgetting to check. This proves it is actually applied.
        var raw = await File.ReadAllTextAsync(Path.Combine(TestContent.Directory, "social-links.json"));
        using var document = JsonDocument.Parse(raw);
        var privateIds = document.RootElement.GetProperty("links").EnumerateArray()
            .Where(l => !l.GetProperty("public").GetBoolean())
            .Select(l => l.GetProperty("id").GetString())
            .ToList();

        foreach (var id in privateIds)
        {
            Assert.DoesNotContain(links, l => l.Id == id);
        }
    }

    [Fact]
    public async Task No_response_ever_contains_a_phone_number()
    {
        // ADR-0003 keeps the number off an indexed page. Asserting it at the edge means a future
        // content change cannot reintroduce it through a field nobody thought about.
        using var client = _app.CreateApiClient();
        var phoneish = new System.Text.RegularExpressions.Regex(
            @"\+?\s*\(?\+?57\)?[\s.\-]*3\d{2}[\s.\-]*\d{3}[\s.\-]*\d{4}");

        foreach (var url in new[] { "/api/content?lang=en", "/api/content?lang=es", "/api/social-links" })
        {
            var body = await client.GetStringAsync(new Uri(url, UriKind.Relative));
            Assert.DoesNotMatch(phoneish, body);
        }
    }
}
