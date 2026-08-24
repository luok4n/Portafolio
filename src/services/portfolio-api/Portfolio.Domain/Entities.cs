using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain;

/// <summary>
/// The portfolio's entities. They are small and read-only by design: this domain publishes a
/// professional history, it does not mutate one. Editing happens in <c>content/</c>, under review.
/// </summary>
public sealed record SpokenLanguage(string Language, string Level);

public sealed record ProfileInfo(
    string Name,
    string Headline,
    string Title,
    string Location,
    string Email,
    string Availability,
    string SummaryTemplate,
    IReadOnlyList<SpokenLanguage> Languages)
{
    public bool IsOpenToWork => string.Equals(Availability, "open-to-work", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The summary with the tenure substituted. The years are never stored, so the text cannot go
    /// stale while the dates move on.
    /// </summary>
    public string RenderSummary(ProfessionalTenure tenure) =>
        SummaryTemplate.Replace(
            "{years}",
            tenure.CompletedYears.ToString(System.Globalization.CultureInfo.InvariantCulture),
            StringComparison.Ordinal);
}

public sealed record WorkExperience(
    string Id,
    string Company,
    string Role,
    string EmploymentType,
    DateRange Period,
    IReadOnlyList<string> ProjectIds,
    IReadOnlyList<string> ParallelWith,
    IReadOnlyList<string> Teams,
    IReadOnlyList<string> Technologies,
    IReadOnlyList<string> Highlights)
{
    /// <summary>True when this role was held alongside another — shown explicitly so the timeline
    /// does not read as a data error.</summary>
    public bool IsConcurrent => ParallelWith.Count > 0;
}

public sealed record SourceReference(string Url, DateOnly Checked);

public sealed record Project(
    string Id,
    string Name,
    string Client,
    string ExperienceId,
    string Sector,
    bool Featured,
    bool Verified,
    string Summary,
    string? CvSummary,
    string? Contribution,
    IReadOnlyList<string> Technologies,
    IReadOnlyList<SourceReference> Sources);

public sealed record SkillCategory(string Id, string Label, IReadOnlyList<string> Items);

public sealed record EducationEntry(string Id, string Degree, string Institution, string Location, int Year);

public sealed record SocialLink(string Id, string Label, string Url, string Display, bool IsPublic);

/// <summary>
/// Everything the site needs for one language, resolved and consistent.
/// </summary>
public sealed record PortfolioContent(
    LanguageCode Language,
    ProfileInfo Profile,
    IReadOnlyList<WorkExperience> Experience,
    IReadOnlyList<Project> Projects,
    IReadOnlyList<SkillCategory> Skills,
    IReadOnlyList<EducationEntry> Education,
    IReadOnlyList<SocialLink> SocialLinks)
{
    public ProfessionalTenure Tenure => ProfessionalTenure.FromPeriods(Experience.Select(e => e.Period));

    public string Summary => Profile.RenderSummary(Tenure);
}
