namespace Portfolio.Infrastructure.Database;

/// <summary>
/// Persistence models. Deliberately separate from the domain entities.
/// </summary>
/// <remarks>
/// <para>
/// ADR-0004 promises the domain has no framework references, and mapping domain records with
/// constructor-only initialisation and <c>IReadOnlyList</c> properties straight to EF Core would
/// mean bending them into shapes that exist to satisfy a change tracker. These rows are shaped for
/// the database; <see cref="EfPortfolioContentSource"/> maps them to the domain.
/// </para>
/// <para>
/// Translations are stored <b>resolved</b>, not sparse: a row exists per language with every field
/// filled, because the fallback between a translation and the base locale is decided once, when the
/// content is seeded, rather than on every read. That keeps the query a straight filter on
/// <c>language_code</c> and means the database can never serve a half-translated record.
/// </para>
/// </remarks>
internal sealed class ProfileRow
{
    public string Id { get; set; } = "profile";

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Availability { get; set; } = string.Empty;

    public ICollection<ProfileTranslationRow> Translations { get; } = [];

    public ICollection<SpokenLanguageRow> SpokenLanguages { get; } = [];
}

internal sealed class ProfileTranslationRow
{
    public string ProfileId { get; set; } = string.Empty;

    public string LanguageCode { get; set; } = string.Empty;

    public string Headline { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string SummaryTemplate { get; set; } = string.Empty;
}

internal sealed class SpokenLanguageRow
{
    public int Id { get; set; }

    public string ProfileId { get; set; } = string.Empty;

    public string LanguageCode { get; set; } = string.Empty;

    public int Ordinal { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Level { get; set; } = string.Empty;
}

internal sealed class ExperienceRow
{
    public string Id { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public int Ordinal { get; set; }

    public int StartYear { get; set; }

    public int StartMonth { get; set; }

    public int EndYear { get; set; }

    public int EndMonth { get; set; }

    public ICollection<ExperienceTranslationRow> Translations { get; } = [];

    public ICollection<ExperienceHighlightRow> Highlights { get; } = [];

    public ICollection<ExperienceTechnologyRow> Technologies { get; } = [];

    public ICollection<ExperienceTeamRow> Teams { get; } = [];

    public ICollection<ExperienceParallelRow> ParallelRoles { get; } = [];
}

internal sealed class ExperienceTranslationRow
{
    public string ExperienceId { get; set; } = string.Empty;

    public string LanguageCode { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string EmploymentType { get; set; } = string.Empty;
}

internal sealed class ExperienceHighlightRow
{
    public int Id { get; set; }

    public string ExperienceId { get; set; } = string.Empty;

    public string LanguageCode { get; set; } = string.Empty;

    public int Ordinal { get; set; }

    public string Text { get; set; } = string.Empty;
}

internal sealed class ExperienceTechnologyRow
{
    public int Id { get; set; }

    public string ExperienceId { get; set; } = string.Empty;

    public int Ordinal { get; set; }

    public string Technology { get; set; } = string.Empty;
}

internal sealed class ExperienceTeamRow
{
    public int Id { get; set; }

    public string ExperienceId { get; set; } = string.Empty;

    public string LanguageCode { get; set; } = string.Empty;

    public int Ordinal { get; set; }

    public string Team { get; set; } = string.Empty;
}

internal sealed class ExperienceParallelRow
{
    public string ExperienceId { get; set; } = string.Empty;

    public string ParallelExperienceId { get; set; } = string.Empty;
}

internal sealed class ProjectRow
{
    public string Id { get; set; } = string.Empty;

    public string ExperienceId { get; set; } = string.Empty;

    public bool Featured { get; set; }

    public bool PubliclySourced { get; set; }

    public int Ordinal { get; set; }

    public ICollection<ProjectTranslationRow> Translations { get; } = [];

    public ICollection<ProjectTechnologyRow> Technologies { get; } = [];

    public ICollection<ProjectSourceRow> Sources { get; } = [];
}

internal sealed class ProjectTranslationRow
{
    public string ProjectId { get; set; } = string.Empty;

    public string LanguageCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Client { get; set; } = string.Empty;

    public string Sector { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string? CvSummary { get; set; }

    public string? Contribution { get; set; }
}

internal sealed class ProjectTechnologyRow
{
    public int Id { get; set; }

    public string ProjectId { get; set; } = string.Empty;

    public int Ordinal { get; set; }

    public string Technology { get; set; } = string.Empty;
}

internal sealed class ProjectSourceRow
{
    public int Id { get; set; }

    public string ProjectId { get; set; } = string.Empty;

    public int Ordinal { get; set; }

    public string Url { get; set; } = string.Empty;

    public DateOnly CheckedOn { get; set; }
}

internal sealed class SkillCategoryRow
{
    public string Id { get; set; } = string.Empty;

    public int Ordinal { get; set; }

    public ICollection<SkillCategoryTranslationRow> Translations { get; } = [];

    public ICollection<SkillItemRow> Items { get; } = [];
}

internal sealed class SkillCategoryTranslationRow
{
    public string CategoryId { get; set; } = string.Empty;

    public string LanguageCode { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
}

internal sealed class SkillItemRow
{
    public int Id { get; set; }

    public string CategoryId { get; set; } = string.Empty;

    public int Ordinal { get; set; }

    public string Item { get; set; } = string.Empty;
}

internal sealed class EducationRow
{
    public string Id { get; set; } = string.Empty;

    public int Year { get; set; }

    public int Ordinal { get; set; }

    public ICollection<EducationTranslationRow> Translations { get; } = [];
}

internal sealed class EducationTranslationRow
{
    public string EducationId { get; set; } = string.Empty;

    public string LanguageCode { get; set; } = string.Empty;

    public string Degree { get; set; } = string.Empty;

    public string Institution { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;
}

internal sealed class SocialLinkRow
{
    public string Id { get; set; } = string.Empty;

    public int Ordinal { get; set; }

    public string Label { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Display { get; set; } = string.Empty;

    public bool IsPublic { get; set; }
}

/// <summary>
/// Records which version of the content files the database currently holds, so a redeploy that
/// changed nothing does not rewrite every row.
/// </summary>
internal sealed class ContentSeedRow
{
    public int Id { get; set; }

    public string ContentHash { get; set; } = string.Empty;

    public DateTimeOffset SeededAt { get; set; }
}
