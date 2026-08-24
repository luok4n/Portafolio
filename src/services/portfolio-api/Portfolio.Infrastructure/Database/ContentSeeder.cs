using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portfolio.Domain;
using Portfolio.Domain.ValueObjects;
using Portfolio.Infrastructure.Json;

namespace Portfolio.Infrastructure.Database;

/// <summary>
/// Loads the reviewed content files into PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// The seed reuses <see cref="JsonFileContentSource"/> as its loader, so the merge between the base
/// locale and a translation is implemented exactly once. What lands in the database is already
/// resolved: one complete row per language, no partial records, no fallback logic at read time.
/// </para>
/// <para>
/// It is reproducible rather than incremental. The content is authored and reviewed, never edited
/// by users, so there is nothing in the database worth preserving that is not in <c>content/</c>.
/// Replacing everything inside a transaction is simpler than reconciling, and it cannot leave the
/// database in a state that no version of the content files describes.
/// </para>
/// </remarks>
internal sealed partial class ContentSeeder(
    PortfolioDbContext context,
    JsonFileContentSource loader,
    IOptions<ContentSourceOptions> contentOptions,
    ILogger<ContentSeeder> logger)
{
    private readonly PortfolioDbContext _context = context;
    private readonly JsonFileContentSource _loader = loader;
    private readonly ILogger<ContentSeeder> _logger = logger;
    private readonly string _contentPath = ResolvePath(contentOptions.Value.Path);

    private static string ResolvePath(string path) =>
        Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);

    public async Task<bool> SeedAsync(CancellationToken cancellationToken = default)
    {
        var fingerprint = ContentFingerprint.Compute(_contentPath);

        var current = await _context.ContentSeeds
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (current is not null && string.Equals(current.ContentHash, fingerprint, StringComparison.Ordinal))
        {
            LogUpToDate(_logger, fingerprint[..12]);
            return false;
        }

        var byLanguage = new Dictionary<LanguageCode, PortfolioContent>();
        foreach (var language in LanguageCode.Supported)
        {
            byLanguage[language] = await _loader.GetAsync(language, cancellationToken).ConfigureAwait(false);
        }

        var baseContent = byLanguage[LanguageCode.Default];

        // The Npgsql retrying execution strategy refuses a transaction it did not open, because a
        // retry has to replay the whole unit rather than half of it. The strategy therefore owns the
        // transaction, and each attempt starts from a clean change tracker so a retry does not
        // re-add the entities the previous attempt already staged.
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async ct =>
        {
            _context.ChangeTracker.Clear();

            await using var transaction = await _context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

            await ClearAsync(ct).ConfigureAwait(false);
            Populate(baseContent, byLanguage);

            _context.ContentSeeds.Add(new ContentSeedRow
            {
                ContentHash = fingerprint,
                SeededAt = DateTimeOffset.UtcNow,
            });

            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
            await transaction.CommitAsync(ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);

        LogSeeded(_logger, fingerprint[..12], baseContent.Experience.Count, baseContent.Projects.Count, LanguageCode.Supported.Count);
        return true;
    }

    /// <summary>
    /// Deletes parents in dependency order; children go with them through their cascades. Projects
    /// go before experiences because that foreign key is restricted on purpose.
    /// </summary>
    private async Task ClearAsync(CancellationToken cancellationToken)
    {
        await _context.Projects.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await _context.Experiences.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await _context.Profiles.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await _context.SkillCategories.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await _context.Education.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await _context.SocialLinks.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await _context.ContentSeeds.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private void Populate(PortfolioContent baseContent, Dictionary<LanguageCode, PortfolioContent> byLanguage)
    {
        const string ProfileId = "profile";

        var profile = new ProfileRow
        {
            Id = ProfileId,
            Name = baseContent.Profile.Name,
            Email = baseContent.Profile.Email,
            Availability = baseContent.Profile.Availability,
        };

        foreach (var (language, content) in byLanguage)
        {
            profile.Translations.Add(new ProfileTranslationRow
            {
                ProfileId = ProfileId,
                LanguageCode = language.Value,
                Headline = content.Profile.Headline,
                Title = content.Profile.Title,
                Location = content.Profile.Location,
                SummaryTemplate = content.Profile.SummaryTemplate,
            });

            for (var i = 0; i < content.Profile.Languages.Count; i++)
            {
                profile.SpokenLanguages.Add(new SpokenLanguageRow
                {
                    ProfileId = ProfileId,
                    LanguageCode = language.Value,
                    Ordinal = i,
                    Name = content.Profile.Languages[i].Language,
                    Level = content.Profile.Languages[i].Level,
                });
            }
        }

        _context.Profiles.Add(profile);

        for (var index = 0; index < baseContent.Experience.Count; index++)
        {
            var source = baseContent.Experience[index];
            var row = new ExperienceRow
            {
                Id = source.Id,
                Company = source.Company,
                Ordinal = index,
                StartYear = source.Period.Start.Year,
                StartMonth = source.Period.Start.Month,
                EndYear = source.Period.End.Year,
                EndMonth = source.Period.End.Month,
            };

            for (var i = 0; i < source.Technologies.Count; i++)
            {
                row.Technologies.Add(new ExperienceTechnologyRow
                {
                    ExperienceId = source.Id,
                    Ordinal = i,
                    Technology = source.Technologies[i],
                });
            }

            foreach (var parallel in source.ParallelWith)
            {
                row.ParallelRoles.Add(new ExperienceParallelRow
                {
                    ExperienceId = source.Id,
                    ParallelExperienceId = parallel,
                });
            }

            foreach (var (language, content) in byLanguage)
            {
                var localised = content.Experience.First(e => e.Id == source.Id);

                row.Translations.Add(new ExperienceTranslationRow
                {
                    ExperienceId = source.Id,
                    LanguageCode = language.Value,
                    Role = localised.Role,
                    EmploymentType = localised.EmploymentType,
                });

                for (var i = 0; i < localised.Highlights.Count; i++)
                {
                    row.Highlights.Add(new ExperienceHighlightRow
                    {
                        ExperienceId = source.Id,
                        LanguageCode = language.Value,
                        Ordinal = i,
                        Text = localised.Highlights[i],
                    });
                }

                for (var i = 0; i < localised.Teams.Count; i++)
                {
                    row.Teams.Add(new ExperienceTeamRow
                    {
                        ExperienceId = source.Id,
                        LanguageCode = language.Value,
                        Ordinal = i,
                        Team = localised.Teams[i],
                    });
                }
            }

            _context.Experiences.Add(row);
        }

        for (var index = 0; index < baseContent.Projects.Count; index++)
        {
            var source = baseContent.Projects[index];
            var row = new ProjectRow
            {
                Id = source.Id,
                ExperienceId = source.ExperienceId,
                Featured = source.Featured,
                PubliclySourced = source.PubliclySourced,
                Ordinal = index,
            };

            for (var i = 0; i < source.Technologies.Count; i++)
            {
                row.Technologies.Add(new ProjectTechnologyRow
                {
                    ProjectId = source.Id,
                    Ordinal = i,
                    Technology = source.Technologies[i],
                });
            }

            for (var i = 0; i < source.Sources.Count; i++)
            {
                row.Sources.Add(new ProjectSourceRow
                {
                    ProjectId = source.Id,
                    Ordinal = i,
                    Url = source.Sources[i].Url,
                    CheckedOn = source.Sources[i].Checked,
                });
            }

            foreach (var (language, content) in byLanguage)
            {
                var localised = content.Projects.First(p => p.Id == source.Id);
                row.Translations.Add(new ProjectTranslationRow
                {
                    ProjectId = source.Id,
                    LanguageCode = language.Value,
                    Name = localised.Name,
                    Client = localised.Client,
                    Sector = localised.Sector,
                    Summary = localised.Summary,
                    CvSummary = localised.CvSummary,
                    Contribution = localised.Contribution,
                });
            }

            _context.Projects.Add(row);
        }

        for (var index = 0; index < baseContent.Skills.Count; index++)
        {
            var source = baseContent.Skills[index];
            var row = new SkillCategoryRow { Id = source.Id, Ordinal = index };

            for (var i = 0; i < source.Items.Count; i++)
            {
                row.Items.Add(new SkillItemRow { CategoryId = source.Id, Ordinal = i, Item = source.Items[i] });
            }

            foreach (var (language, content) in byLanguage)
            {
                row.Translations.Add(new SkillCategoryTranslationRow
                {
                    CategoryId = source.Id,
                    LanguageCode = language.Value,
                    Label = content.Skills.First(s => s.Id == source.Id).Label,
                });
            }

            _context.SkillCategories.Add(row);
        }

        for (var index = 0; index < baseContent.Education.Count; index++)
        {
            var source = baseContent.Education[index];
            var row = new EducationRow { Id = source.Id, Year = source.Year, Ordinal = index };

            foreach (var (language, content) in byLanguage)
            {
                var localised = content.Education.First(e => e.Id == source.Id);
                row.Translations.Add(new EducationTranslationRow
                {
                    EducationId = source.Id,
                    LanguageCode = language.Value,
                    Degree = localised.Degree,
                    Institution = localised.Institution,
                    Location = localised.Location,
                });
            }

            _context.Education.Add(row);
        }

        for (var index = 0; index < baseContent.SocialLinks.Count; index++)
        {
            var source = baseContent.SocialLinks[index];
            _context.SocialLinks.Add(new SocialLinkRow
            {
                Id = source.Id,
                Ordinal = index,
                Label = source.Label,
                Url = source.Url,
                Display = source.Display,
                IsPublic = source.IsPublic,
            });
        }
    }

    [LoggerMessage(EventId = 2000, Level = LogLevel.Information,
        Message = "Content already at fingerprint {Fingerprint}; nothing to seed.")]
    private static partial void LogUpToDate(ILogger logger, string fingerprint);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information,
        Message = "Seeded content {Fingerprint}: {ExperienceCount} roles, {ProjectCount} projects, {LanguageCount} languages.")]
    private static partial void LogSeeded(ILogger logger, string fingerprint, int experienceCount, int projectCount, int languageCount);
}
