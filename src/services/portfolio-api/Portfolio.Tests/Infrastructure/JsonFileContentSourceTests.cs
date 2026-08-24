using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Portfolio.Domain.ValueObjects;
using Portfolio.Infrastructure.Json;

namespace Portfolio.Tests.Infrastructure;

/// <summary>
/// Covers the merge between the base locale and a translation, which is where a bilingual content
/// model actually goes wrong: a field missing from the translation must fall back rather than
/// render empty, and a fact that only exists in the base locale must survive the merge.
/// </summary>
public sealed class JsonFileContentSourceTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("portfolio-content-").FullName;

    public JsonFileContentSourceTests()
    {
        Write("profile.en.json", """
        {
          "name": "Test Person",
          "headline": "Base headline",
          "title": "Base title",
          "location": "Somewhere",
          "email": "test@example.com",
          "availability": "open-to-work",
          "summaryTemplate": "Base summary with {years} years."
        }
        """);

        // Deliberately incomplete: no title, no location. Those must fall back to the base locale.
        Write("profile.es.json", """
        {
          "headline": "Titular traducido",
          "summaryTemplate": "Resumen con {years} años."
        }
        """);

        Write("experience.en.json", """
        {
          "experience": [
            {
              "id": "one",
              "company": "Acme",
              "role": "Engineer",
              "employmentType": "full-time",
              "start": "2020-01",
              "end": "2020-12",
              "projects": [],
              "technologies": ["C#"],
              "highlights": ["Did a thing.", "Did another thing."]
            }
          ]
        }
        """);

        Write("experience.es.json", """
        {
          "experience": [
            { "id": "one", "role": "Ingeniero", "highlights": ["Hice algo.", "Hice otra cosa."] }
          ]
        }
        """);

        Write("projects.en.json", """
        { "projects": [ { "id": "p1", "name": "P1", "experienceId": "one", "summary": "Base summary",
          "publiclySourced": true, "sources": [ { "url": "https://example.com", "checked": "2026-08-24" } ] } ] }
        """);
        Write("projects.es.json", """{ "projects": [ { "id": "p1", "summary": "Resumen base" } ] }""");

        Write("education.en.json", """
        { "education": [ { "id": "e1", "degree": "B.Sc.", "institution": "University", "location": "City", "year": 2018 } ] }
        """);
        Write("education.es.json", """{ "education": [ { "id": "e1", "degree": "Ingeniería" } ] }""");

        Write("skills.json", """
        { "categories": [ { "id": "languages", "label": { "en": "Languages", "es": "Lenguajes" }, "items": ["C#"] } ] }
        """);

        Write("social-links.json", """
        { "links": [
          { "id": "linkedin", "label": "LinkedIn", "url": "https://x", "display": "x", "public": true },
          { "id": "secret", "label": "Secret", "url": "https://y", "display": "y", "public": false }
        ] }
        """);
    }

    private void Write(string name, string json) => File.WriteAllText(Path.Combine(_dir, name), json);

    private JsonFileContentSource CreateSource() =>
        new(Options.Create(new ContentSourceOptions { Path = _dir }), NullLogger<JsonFileContentSource>.Instance);

    [Fact]
    public async Task Serves_the_base_locale_unchanged()
    {
        var content = await CreateSource().GetAsync(LanguageCode.English);

        Assert.Equal("Base headline", content.Profile.Headline);
        Assert.Equal("Engineer", content.Experience[0].Role);
        Assert.Equal("Base summary with 1 years.", content.Summary);
    }

    [Fact]
    public async Task Applies_the_translation_where_one_exists()
    {
        var content = await CreateSource().GetAsync(LanguageCode.Spanish);

        Assert.Equal("Titular traducido", content.Profile.Headline);
        Assert.Equal("Ingeniero", content.Experience[0].Role);
        Assert.Equal("Ingeniería", content.Education[0].Degree);
        Assert.Equal("Lenguajes", content.Skills[0].Label);
    }

    [Fact]
    public async Task Falls_back_field_by_field_when_a_translation_is_incomplete()
    {
        var content = await CreateSource().GetAsync(LanguageCode.Spanish);

        Assert.Equal("Base title", content.Profile.Title);
        Assert.Equal("Somewhere", content.Profile.Location);
        Assert.Equal("Acme", content.Experience[0].Company);
        Assert.Equal("University", content.Education[0].Institution);
    }

    [Fact]
    public async Task Facts_live_only_in_the_base_locale_and_survive_the_merge()
    {
        var spanish = await CreateSource().GetAsync(LanguageCode.Spanish);
        var english = await CreateSource().GetAsync(LanguageCode.English);

        Assert.Equal(english.Experience[0].Period, spanish.Experience[0].Period);
        Assert.Equal(english.Education[0].Year, spanish.Education[0].Year);
        Assert.Equal(english.Projects[0].Sources[0].Url, spanish.Projects[0].Sources[0].Url);
        Assert.Equal(english.Projects[0].PubliclySourced, spanish.Projects[0].PubliclySourced);
    }

    [Fact]
    public async Task Tenure_is_computed_from_the_periods_not_stored()
    {
        var content = await CreateSource().GetAsync(LanguageCode.English);

        Assert.Equal(12, content.Tenure.UniqueMonths);
        Assert.Equal(1, content.Tenure.CompletedYears);
    }

    [Fact]
    public async Task Non_public_links_are_still_present_in_the_domain_and_filtered_later()
    {
        // The source reports what the file says; hiding a link is the query service's job, and the
        // two responsibilities are kept apart on purpose.
        var content = await CreateSource().GetAsync(LanguageCode.English);

        Assert.Equal(2, content.SocialLinks.Count);
        Assert.Contains(content.SocialLinks, l => !l.IsPublic);
    }

    [Fact]
    public async Task A_missing_required_field_in_the_base_locale_fails_loudly()
    {
        Write("profile.en.json", """{ "name": "Test Person" }""");

        await Assert.ThrowsAsync<InvalidContentException>(() => CreateSource().GetAsync(LanguageCode.English));
    }

    [Fact]
    public async Task A_missing_translation_file_falls_back_to_the_base_locale()
    {
        File.Delete(Path.Combine(_dir, "profile.es.json"));

        var content = await CreateSource().GetAsync(LanguageCode.Spanish);

        Assert.Equal("Base headline", content.Profile.Headline);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }
}
