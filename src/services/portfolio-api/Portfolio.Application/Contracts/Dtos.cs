namespace Portfolio.Application.Contracts;

/// <summary>
/// The shapes the API returns. Kept separate from the domain entities so a rename inside the domain
/// is not automatically a breaking change for the frontend.
/// </summary>
public sealed record LanguageInfo(string Requested, string Resolved, string ResolvedFrom);

public sealed record SpokenLanguageDto(string Language, string Level);

public sealed record ProfileDto(
    string Name,
    string Headline,
    string Title,
    string Location,
    string Email,
    bool OpenToWork,
    string Summary,
    int YearsOfExperience,
    int MonthsOfExperience,
    IReadOnlyList<SpokenLanguageDto> Languages);

public sealed record ExperienceDto(
    string Id,
    string Company,
    string Role,
    string EmploymentType,
    string Start,
    string End,
    int DurationMonths,
    bool Concurrent,
    IReadOnlyList<string> ParallelWith,
    IReadOnlyList<string> Teams,
    IReadOnlyList<string> Technologies,
    IReadOnlyList<string> Highlights,
    IReadOnlyList<string> ProjectIds);

public sealed record SourceDto(string Url, string Checked);

public sealed record ProjectSummaryDto(
    string Id,
    string Name,
    string Client,
    string Sector,
    bool Featured,
    IReadOnlyList<string> Technologies);

public sealed record ProjectDto(
    string Id,
    string Name,
    string Client,
    string ExperienceId,
    string Company,
    string Sector,
    bool Featured,
    bool PubliclySourced,
    string Summary,
    string? Contribution,
    IReadOnlyList<string> Technologies,
    IReadOnlyList<SourceDto> Sources);

public sealed record SkillCategoryDto(string Id, string Label, IReadOnlyList<string> Items);

public sealed record EducationDto(string Id, string Degree, string Institution, string Location, int Year);

public sealed record SocialLinkDto(string Id, string Label, string Url, string Display);

/// <summary>
/// The whole bundle in one response. The site always needs all of it, so seven round trips would be
/// six too many — and the build-time snapshot wants exactly this shape.
/// </summary>
public sealed record PortfolioContentDto(
    LanguageInfo Language,
    ProfileDto Profile,
    IReadOnlyList<ExperienceDto> Experience,
    IReadOnlyList<ProjectDto> Projects,
    IReadOnlyList<SkillCategoryDto> Skills,
    IReadOnlyList<EducationDto> Education,
    IReadOnlyList<SocialLinkDto> SocialLinks);
