using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Portfolio.Domain;
using Portfolio.Domain.ValueObjects;
using Portfolio.Infrastructure.Json;
using Portfolio.Tests.Support;

namespace Portfolio.Tests.Infrastructure;

/// <summary>
/// The database path, against a real PostgreSQL.
/// </summary>
public sealed class PostgresContentSourceTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres = postgres;

    private static JsonFileContentSource FileSource() => new(
        Options.Create(new ContentSourceOptions { Path = TestContent.Directory }),
        NullLogger<JsonFileContentSource>.Instance);

    [RequiresDockerFact]
    public async Task The_migration_applies_and_the_content_seeds()
    {
        var source = await _postgres.ResolveSourceAsync();

        var content = await source.GetAsync(LanguageCode.English);

        Assert.NotEmpty(content.Experience);
        Assert.NotEmpty(content.Projects);
        Assert.False(string.IsNullOrWhiteSpace(content.Profile.Name));
    }

    /// <summary>
    /// The automated version of <c>tools/api/parity-check.mjs</c>.
    /// </summary>
    /// <remarks>
    /// Two independent implementations of one contract only stay honest if something compares them.
    /// The script already caught a real disagreement about the order of a role's project list —
    /// which no unit test would have found, because each implementation was individually correct.
    /// Having it as a test means it runs on every push rather than when someone remembers.
    /// </remarks>
    [RequiresDockerFact]
    public async Task The_database_returns_exactly_what_the_files_do()
    {
        var database = await _postgres.ResolveSourceAsync();
        var files = FileSource();

        foreach (var language in LanguageCode.Supported)
        {
            var fromDb = await database.GetAsync(language);
            var fromFiles = await files.GetAsync(language);

            AssertSameContent(fromFiles, fromDb, language);
        }
    }

    private static void AssertSameContent(PortfolioContent expected, PortfolioContent actual, LanguageCode language)
    {
        var where = $"[{language}]";

        Assert.Equal(expected.Profile.Name, actual.Profile.Name);
        Assert.Equal(expected.Profile.Headline, actual.Profile.Headline);
        Assert.Equal(expected.Profile.Title, actual.Profile.Title);
        Assert.Equal(expected.Profile.Email, actual.Profile.Email);
        Assert.Equal(expected.Profile.Availability, actual.Profile.Availability);
        Assert.Equal(expected.Summary, actual.Summary);
        Assert.Equal(expected.Tenure.UniqueMonths, actual.Tenure.UniqueMonths);

        Assert.Equal(
            expected.Profile.Languages.Select(l => $"{l.Language}/{l.Level}"),
            actual.Profile.Languages.Select(l => $"{l.Language}/{l.Level}"));

        // Order matters as much as membership: the ordering bug the parity script found was two
        // correct sets in different sequences.
        Assert.Equal(expected.Experience.Select(e => e.Id), actual.Experience.Select(e => e.Id));

        foreach (var (want, got) in expected.Experience.Zip(actual.Experience))
        {
            Assert.Equal(want.Company, got.Company);
            Assert.Equal(want.Role, got.Role);
            Assert.Equal(want.EmploymentType, got.EmploymentType);
            Assert.Equal(want.Period.Start, got.Period.Start);
            Assert.Equal(want.Period.End, got.Period.End);
            Assert.Equal(want.Highlights, got.Highlights);
            Assert.Equal(want.Technologies, got.Technologies);
            Assert.Equal(want.Teams, got.Teams);
            Assert.Equal(want.ProjectIds, got.ProjectIds);
            Assert.Equal(want.ParallelWith.Order(), got.ParallelWith.Order());
        }

        Assert.Equal(expected.Projects.Select(p => p.Id), actual.Projects.Select(p => p.Id));

        foreach (var (want, got) in expected.Projects.Zip(actual.Projects))
        {
            Assert.Equal(want.Name, got.Name);
            Assert.Equal(want.Client, got.Client);
            Assert.Equal(want.Sector, got.Sector);
            Assert.Equal(want.Summary, got.Summary);
            Assert.Equal(want.CvSummary, got.CvSummary);
            Assert.Equal(want.Contribution, got.Contribution);
            Assert.Equal(want.Featured, got.Featured);
            Assert.Equal(want.PubliclySourced, got.PubliclySourced);
            Assert.Equal(want.Technologies, got.Technologies);
            Assert.Equal(
                want.Sources.Select(s => $"{s.Url}@{s.Checked:yyyy-MM-dd}"),
                got.Sources.Select(s => $"{s.Url}@{s.Checked:yyyy-MM-dd}"));
        }

        Assert.Equal(expected.Skills.Select(s => s.Id), actual.Skills.Select(s => s.Id));
        foreach (var (want, got) in expected.Skills.Zip(actual.Skills))
        {
            Assert.Equal(want.Label, got.Label);
            Assert.Equal(want.Items, got.Items);
        }

        Assert.Equal(expected.Education.Select(e => e.Degree), actual.Education.Select(e => e.Degree));
        Assert.Equal(expected.Education.Select(e => e.Year), actual.Education.Select(e => e.Year));

        Assert.Equal(expected.SocialLinks.Select(l => l.Id), actual.SocialLinks.Select(l => l.Id));
        Assert.Equal(
            expected.SocialLinks.Select(l => l.IsPublic),
            actual.SocialLinks.Select(l => l.IsPublic));

        Assert.True(actual.Experience.Count > 0, $"{where} the database returned no experience");
    }

    [RequiresDockerFact]
    public async Task Reseeding_unchanged_content_changes_nothing()
    {
        // A redeploy that changed no content must not rewrite every row. The fingerprint is what
        // makes "when did this content last actually change?" an answerable question.
        var before = await ReadSeedStateAsync();

        await _postgres.ReinitialiseAsync();

        var after = await ReadSeedStateAsync();

        Assert.Equal(before.hash, after.hash);
        Assert.Equal(before.seededAt, after.seededAt);
    }

    [RequiresDockerFact]
    public async Task Reseeding_leaves_exactly_one_copy_of_everything()
    {
        await _postgres.ReinitialiseAsync();
        await _postgres.ReinitialiseAsync();

        var source = await _postgres.ResolveSourceAsync();
        var content = await source.GetAsync(LanguageCode.English);
        var files = await FileSource().GetAsync(LanguageCode.English);

        // The seed replaces rather than reconciles; a bug there would show up as duplicates.
        Assert.Equal(files.Experience.Count, content.Experience.Count);
        Assert.Equal(files.Projects.Count, content.Projects.Count);
        Assert.Equal(await CountAsync("experience_highlights") / LanguageCode.Supported.Count,
                     files.Experience.Sum(e => e.Highlights.Count));
    }

    [RequiresDockerFact]
    public async Task Translations_are_stored_resolved_for_every_language()
    {
        // One complete row per language, decided once at seed time — so a read is a straight filter
        // and the database can never serve a half-translated record.
        var roles = await CountAsync("experiences");
        var translations = await CountAsync("experience_translations");

        Assert.Equal(roles * LanguageCode.Supported.Count, translations);
    }

    [RequiresDockerFact]
    public async Task The_database_refuses_a_period_that_ends_before_it_starts()
    {
        // The check constraint is the last line of defence: a negative duration would render as
        // nonsense on a public page, so the storage refuses it even if something upstream tries.
        var error = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
            """
            INSERT INTO experiences (id, company, ordinal, start_year, start_month, end_year, end_month)
            VALUES ('broken', 'Acme', 99, 2022, 12, 2022, 1)
            """));

        Assert.Equal("23514", error.SqlState); // check_violation
    }

    [RequiresDockerFact]
    public async Task The_database_refuses_an_impossible_month()
    {
        var error = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
            """
            INSERT INTO experiences (id, company, ordinal, start_year, start_month, end_year, end_month)
            VALUES ('broken-month', 'Acme', 98, 2022, 13, 2022, 13)
            """));

        Assert.Equal("23514", error.SqlState);
    }

    [RequiresDockerFact]
    public async Task A_role_cannot_be_deleted_while_projects_still_point_at_it()
    {
        // Restricted rather than cascading on purpose: deleting a role that still has projects is a
        // mistake worth surfacing, not a silent loss of content.
        var source = await _postgres.ResolveSourceAsync();
        var content = await source.GetAsync(LanguageCode.English);
        var roleWithProjects = content.Experience.First(e => e.ProjectIds.Count > 0);

        var error = await Assert.ThrowsAsync<PostgresException>(
            () => ExecuteAsync($"DELETE FROM experiences WHERE id = '{roleWithProjects.Id}'"));

        Assert.Equal("23503", error.SqlState); // foreign_key_violation
    }

    // --- helpers ---------------------------------------------------------------------------------

    private async Task<(string hash, DateTimeOffset seededAt)> ReadSeedStateAsync()
    {
        await using var connection = new NpgsqlConnection(_postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT content_hash, seeded_at FROM content_seeds ORDER BY id DESC LIMIT 1", connection);
        await using var reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync(), "no seed state recorded");
        return (reader.GetString(0), reader.GetFieldValue<DateTimeOffset>(1));
    }

    private async Task<int> CountAsync(string table)
    {
        await using var connection = new NpgsqlConnection(_postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"SELECT count(*) FROM {table}", connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
