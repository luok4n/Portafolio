using Portfolio.Application.Abstractions;
using Portfolio.Application.Contracts;
using Portfolio.Domain;

namespace Portfolio.Application;

/// <summary>Raised when a caller asks for something that does not exist. Mapped to 404 at the edge.</summary>
public sealed class ContentNotFoundException(string what, string id)
    : Exception($"No {what} with id '{id}'.")
{
    public string What { get; } = what;

    public string Id { get; } = id;
}

/// <summary>
/// The read side of the portfolio. Every method resolves the language first and reports back which
/// language was actually served and why, so a caller never has to assume.
/// </summary>
public sealed class PortfolioQueryService(IPortfolioContentSource source)
{
    private readonly IPortfolioContentSource _source = source;

    public async Task<PortfolioContentDto> GetContentAsync(
        string? requestedLanguage,
        string? acceptLanguage,
        CancellationToken cancellationToken = default)
    {
        var (content, language) = await ResolveAsync(requestedLanguage, acceptLanguage, cancellationToken)
            .ConfigureAwait(false);

        return new PortfolioContentDto(
            language,
            MapProfile(content),
            content.Experience.Select(MapExperience).ToList(),
            content.Projects.Select(p => MapProject(p, content)).ToList(),
            content.Skills.Select(s => new SkillCategoryDto(s.Id, s.Label, s.Items)).ToList(),
            content.Education.Select(e => new EducationDto(e.Id, e.Degree, e.Institution, e.Location, e.Year)).ToList(),
            MapSocialLinks(content));
    }

    public async Task<ProfileDto> GetProfileAsync(string? requestedLanguage, string? acceptLanguage, CancellationToken ct = default)
    {
        var (content, _) = await ResolveAsync(requestedLanguage, acceptLanguage, ct).ConfigureAwait(false);
        return MapProfile(content);
    }

    public async Task<IReadOnlyList<ExperienceDto>> GetExperienceAsync(string? requestedLanguage, string? acceptLanguage, CancellationToken ct = default)
    {
        var (content, _) = await ResolveAsync(requestedLanguage, acceptLanguage, ct).ConfigureAwait(false);
        return content.Experience.Select(MapExperience).ToList();
    }

    public async Task<IReadOnlyList<SkillCategoryDto>> GetSkillsAsync(string? requestedLanguage, string? acceptLanguage, CancellationToken ct = default)
    {
        var (content, _) = await ResolveAsync(requestedLanguage, acceptLanguage, ct).ConfigureAwait(false);
        return content.Skills.Select(s => new SkillCategoryDto(s.Id, s.Label, s.Items)).ToList();
    }

    public async Task<IReadOnlyList<ProjectDto>> GetProjectsAsync(string? requestedLanguage, string? acceptLanguage, CancellationToken ct = default)
    {
        var (content, _) = await ResolveAsync(requestedLanguage, acceptLanguage, ct).ConfigureAwait(false);
        return content.Projects.Select(p => MapProject(p, content)).ToList();
    }

    public async Task<ProjectDto> GetProjectAsync(string id, string? requestedLanguage, string? acceptLanguage, CancellationToken ct = default)
    {
        var (content, _) = await ResolveAsync(requestedLanguage, acceptLanguage, ct).ConfigureAwait(false);
        var project = content.Projects.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase))
            ?? throw new ContentNotFoundException("project", id);

        return MapProject(project, content);
    }

    public async Task<IReadOnlyList<EducationDto>> GetEducationAsync(string? requestedLanguage, string? acceptLanguage, CancellationToken ct = default)
    {
        var (content, _) = await ResolveAsync(requestedLanguage, acceptLanguage, ct).ConfigureAwait(false);
        return content.Education.Select(e => new EducationDto(e.Id, e.Degree, e.Institution, e.Location, e.Year)).ToList();
    }

    public async Task<IReadOnlyList<SocialLinkDto>> GetSocialLinksAsync(CancellationToken ct = default)
    {
        var (content, _) = await ResolveAsync(null, null, ct).ConfigureAwait(false);
        return MapSocialLinks(content);
    }

    private async Task<(PortfolioContent Content, LanguageInfo Language)> ResolveAsync(
        string? requestedLanguage,
        string? acceptLanguage,
        CancellationToken cancellationToken)
    {
        var negotiated = LanguageNegotiator.Negotiate(requestedLanguage, acceptLanguage);
        var content = await _source.GetAsync(negotiated.Language, cancellationToken).ConfigureAwait(false);

        var info = new LanguageInfo(
            requestedLanguage ?? acceptLanguage ?? string.Empty,
            negotiated.Language.Value,
            Describe(negotiated.Source));

        return (content, info);
    }

    /// <summary>
    /// Spelled out rather than derived from the enum name: this string is part of the API contract,
    /// so renaming <see cref="LanguageSource"/> must not silently change what clients receive.
    /// </summary>
    private static string Describe(LanguageSource source) => source switch
    {
        LanguageSource.Explicit => "explicit",
        LanguageSource.AcceptHeader => "accept-header",
        LanguageSource.Fallback => "fallback",
        _ => "unknown",
    };

    private static ProfileDto MapProfile(PortfolioContent content)
    {
        var tenure = content.Tenure;
        return new ProfileDto(
            content.Profile.Name,
            content.Profile.Headline,
            content.Profile.Title,
            content.Profile.Location,
            content.Profile.Email,
            content.Profile.IsOpenToWork,
            content.Summary,
            tenure.CompletedYears,
            tenure.UniqueMonths,
            content.Profile.Languages.Select(l => new SpokenLanguageDto(l.Language, l.Level)).ToList());
    }

    private static ExperienceDto MapExperience(WorkExperience e) => new(
        e.Id,
        e.Company,
        e.Role,
        e.EmploymentType,
        e.Period.Start.ToString(),
        e.Period.End.ToString(),
        e.Period.MonthCount,
        e.IsConcurrent,
        e.ParallelWith,
        e.Teams,
        e.Technologies,
        e.Highlights,
        e.ProjectIds);

    private static ProjectDto MapProject(Project p, PortfolioContent content)
    {
        var company = content.Experience
            .FirstOrDefault(e => string.Equals(e.Id, p.ExperienceId, StringComparison.OrdinalIgnoreCase))
            ?.Company ?? string.Empty;

        return new ProjectDto(
            p.Id,
            p.Name,
            p.Client,
            p.ExperienceId,
            company,
            p.Sector,
            p.Featured,
            p.Verified,
            p.Summary,
            p.Contribution,
            p.Technologies,
            p.Sources.Select(s => new SourceDto(s.Url, s.Checked.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture))).ToList());
    }

    /// <summary>
    /// Only links marked public are ever served. The filter lives here rather than in the frontend
    /// so a private contact detail cannot leak by way of a template that forgot to check.
    /// </summary>
    private static List<SocialLinkDto> MapSocialLinks(PortfolioContent content) =>
        content.SocialLinks
            .Where(l => l.IsPublic)
            .Select(l => new SocialLinkDto(l.Id, l.Label, l.Url, l.Display))
            .ToList();
}
