using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Abstractions;
using Portfolio.Domain;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Infrastructure.Database;

/// <summary>
/// Reads the portfolio out of PostgreSQL and rebuilds the domain objects.
/// </summary>
/// <remarks>
/// <para>
/// The whole content for one language is loaded in one pass. That is the only shape the application
/// asks for, and for a payload this size it is cheaper than the round trips a lazier design would
/// make. Queries are <c>AsNoTracking</c> and split, because the alternative — one join across a
/// dozen collections — multiplies rows for no benefit on a read-only path.
/// </para>
/// <para>
/// The list of project ids on a role is derived from the projects' own foreign key rather than
/// stored twice. The content files carry both directions and the content validator enforces that
/// they agree; the database keeps one.
/// </para>
/// </remarks>
internal sealed class EfPortfolioContentSource(PortfolioDbContext context) : IPortfolioContentSource
{
    private readonly PortfolioDbContext _context = context;

    public async Task<PortfolioContent> GetAsync(LanguageCode language, CancellationToken cancellationToken = default)
    {
        var code = language.Value;

        var profileRow = await _context.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("The database holds no profile. Has the content been seeded?");

        var profileTranslation = await _context.ProfileTranslations
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ProfileId == profileRow.Id && t.LanguageCode == code, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"No profile translation stored for '{code}'.");

        var spoken = await _context.SpokenLanguages
            .AsNoTracking()
            .Where(s => s.ProfileId == profileRow.Id && s.LanguageCode == code)
            .OrderBy(s => s.Ordinal)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var experiences = await LoadExperienceAsync(code, cancellationToken).ConfigureAwait(false);
        var projects = await LoadProjectsAsync(code, cancellationToken).ConfigureAwait(false);
        var skills = await LoadSkillsAsync(code, cancellationToken).ConfigureAwait(false);
        var education = await LoadEducationAsync(code, cancellationToken).ConfigureAwait(false);

        var links = await _context.SocialLinks
            .AsNoTracking()
            .OrderBy(l => l.Ordinal)
            .Select(l => new SocialLink(l.Id, l.Label, l.Url, l.Display, l.IsPublic))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var profile = new ProfileInfo(
            profileRow.Name,
            profileTranslation.Headline,
            profileTranslation.Title,
            profileTranslation.Location,
            profileRow.Email,
            profileRow.Availability,
            profileTranslation.SummaryTemplate,
            spoken.Select(s => new SpokenLanguage(s.Name, s.Level)).ToList());

        return new PortfolioContent(language, profile, experiences, projects, skills, education, links);
    }

    private async Task<List<WorkExperience>> LoadExperienceAsync(string code, CancellationToken ct)
    {
        var rows = await _context.Experiences
            .AsNoTracking()
            .OrderBy(e => e.Ordinal)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var ids = rows.Select(r => r.Id).ToList();

        var translations = await _context.ExperienceTranslations.AsNoTracking()
            .Where(t => ids.Contains(t.ExperienceId) && t.LanguageCode == code)
            .ToDictionaryAsync(t => t.ExperienceId, ct).ConfigureAwait(false);

        var highlights = await Grouped(
            _context.ExperienceHighlights.AsNoTracking()
                .Where(h => ids.Contains(h.ExperienceId) && h.LanguageCode == code)
                .OrderBy(h => h.Ordinal),
            h => h.ExperienceId, h => h.Text, ct).ConfigureAwait(false);

        var technologies = await Grouped(
            _context.ExperienceTechnologies.AsNoTracking()
                .Where(t => ids.Contains(t.ExperienceId))
                .OrderBy(t => t.Ordinal),
            t => t.ExperienceId, t => t.Technology, ct).ConfigureAwait(false);

        var teams = await Grouped(
            _context.ExperienceTeams.AsNoTracking()
                .Where(t => ids.Contains(t.ExperienceId) && t.LanguageCode == code)
                .OrderBy(t => t.Ordinal),
            t => t.ExperienceId, t => t.Team, ct).ConfigureAwait(false);

        var parallel = await Grouped(
            _context.ExperienceParallelRoles.AsNoTracking().Where(p => ids.Contains(p.ExperienceId)),
            p => p.ExperienceId, p => p.ParallelExperienceId, ct).ConfigureAwait(false);

        var projectIds = await Grouped(
            _context.Projects.AsNoTracking().Where(p => ids.Contains(p.ExperienceId)).OrderBy(p => p.Ordinal),
            p => p.ExperienceId, p => p.Id, ct).ConfigureAwait(false);

        return rows.Select(r =>
        {
            var translation = translations.GetValueOrDefault(r.Id)
                ?? throw new InvalidOperationException($"No translation stored for role '{r.Id}' in '{code}'.");

            return new WorkExperience(
                r.Id,
                r.Company,
                translation.Role,
                translation.EmploymentType,
                DateRange.Create(new YearMonth(r.StartYear, r.StartMonth), new YearMonth(r.EndYear, r.EndMonth)),
                projectIds.GetValueOrDefault(r.Id, []),
                parallel.GetValueOrDefault(r.Id, []),
                teams.GetValueOrDefault(r.Id, []),
                technologies.GetValueOrDefault(r.Id, []),
                highlights.GetValueOrDefault(r.Id, []));
        }).ToList();
    }

    private async Task<List<Project>> LoadProjectsAsync(string code, CancellationToken ct)
    {
        var rows = await _context.Projects.AsNoTracking().OrderBy(p => p.Ordinal).ToListAsync(ct).ConfigureAwait(false);
        var ids = rows.Select(r => r.Id).ToList();

        var translations = await _context.ProjectTranslations.AsNoTracking()
            .Where(t => ids.Contains(t.ProjectId) && t.LanguageCode == code)
            .ToDictionaryAsync(t => t.ProjectId, ct).ConfigureAwait(false);

        var technologies = await Grouped(
            _context.ProjectTechnologies.AsNoTracking().Where(t => ids.Contains(t.ProjectId)).OrderBy(t => t.Ordinal),
            t => t.ProjectId, t => t.Technology, ct).ConfigureAwait(false);

        var sourceRows = await _context.ProjectSources.AsNoTracking()
            .Where(s => ids.Contains(s.ProjectId))
            .OrderBy(s => s.Ordinal)
            .ToListAsync(ct).ConfigureAwait(false);

        var sources = sourceRows
            .GroupBy(s => s.ProjectId, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<SourceReference>)g.Select(s => new SourceReference(s.Url, s.CheckedOn)).ToList(),
                StringComparer.Ordinal);

        return rows.Select(r =>
        {
            var translation = translations.GetValueOrDefault(r.Id)
                ?? throw new InvalidOperationException($"No translation stored for project '{r.Id}' in '{code}'.");

            return new Project(
                r.Id,
                translation.Name,
                translation.Client,
                r.ExperienceId,
                translation.Sector,
                r.Featured,
                r.Verified,
                translation.Summary,
                translation.CvSummary,
                translation.Contribution,
                technologies.GetValueOrDefault(r.Id, []),
                sources.GetValueOrDefault(r.Id, []));
        }).ToList();
    }

    private async Task<List<SkillCategory>> LoadSkillsAsync(string code, CancellationToken ct)
    {
        var rows = await _context.SkillCategories.AsNoTracking().OrderBy(c => c.Ordinal).ToListAsync(ct).ConfigureAwait(false);
        var ids = rows.Select(r => r.Id).ToList();

        var translations = await _context.SkillCategoryTranslations.AsNoTracking()
            .Where(t => ids.Contains(t.CategoryId) && t.LanguageCode == code)
            .ToDictionaryAsync(t => t.CategoryId, ct).ConfigureAwait(false);

        var items = await Grouped(
            _context.SkillItems.AsNoTracking().Where(i => ids.Contains(i.CategoryId)).OrderBy(i => i.Ordinal),
            i => i.CategoryId, i => i.Item, ct).ConfigureAwait(false);

        return rows.Select(r => new SkillCategory(
            r.Id,
            translations.GetValueOrDefault(r.Id)?.Label ?? r.Id,
            items.GetValueOrDefault(r.Id, []))).ToList();
    }

    private async Task<List<EducationEntry>> LoadEducationAsync(string code, CancellationToken ct)
    {
        var rows = await _context.Education.AsNoTracking().OrderBy(e => e.Ordinal).ToListAsync(ct).ConfigureAwait(false);
        var ids = rows.Select(r => r.Id).ToList();

        var translations = await _context.EducationTranslations.AsNoTracking()
            .Where(t => ids.Contains(t.EducationId) && t.LanguageCode == code)
            .ToDictionaryAsync(t => t.EducationId, ct).ConfigureAwait(false);

        return rows.Select(r =>
        {
            var translation = translations.GetValueOrDefault(r.Id)
                ?? throw new InvalidOperationException($"No translation stored for education '{r.Id}' in '{code}'.");

            return new EducationEntry(r.Id, translation.Degree, translation.Institution, translation.Location, r.Year);
        }).ToList();
    }

    private static async Task<Dictionary<string, IReadOnlyList<string>>> Grouped<TRow>(
        IQueryable<TRow> query,
        Func<TRow, string> keyOf,
        Func<TRow, string> valueOf,
        CancellationToken ct)
    {
        var rows = await query.ToListAsync(ct).ConfigureAwait(false);
        return rows
            .GroupBy(keyOf, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(valueOf).ToList(), StringComparer.Ordinal);
    }
}
