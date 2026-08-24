using System.Text.Json.Serialization;

namespace Portfolio.Infrastructure.Json;

/// <summary>
/// One-to-one mappings of the files in <c>content/</c>. Kept separate from the domain entities so
/// that the on-disk format can change without the domain following it, and so the reviewed content
/// files stay the readable artefact they are meant to be.
///
/// Every translatable property is nullable: a translation file carries only the fields it
/// translates, and the rest is filled from the base locale.
/// </summary>
internal sealed record ProfileFile(
    string? Name,
    string? Headline,
    string? Title,
    string? Location,
    string? Email,
    string? Availability,
    string? SummaryTemplate,
    IReadOnlyList<SpokenLanguageFile>? Languages);

internal sealed record SpokenLanguageFile(string Language, string Level);

internal sealed record ExperienceFileRoot([property: JsonPropertyName("experience")] IReadOnlyList<ExperienceFile> Experience);

internal sealed record ExperienceFile(
    string Id,
    string? Company,
    string? Role,
    string? EmploymentType,
    string? Start,
    string? End,
    IReadOnlyList<string>? Projects,
    IReadOnlyList<string>? ParallelWith,
    IReadOnlyList<string>? Teams,
    IReadOnlyList<string>? Technologies,
    IReadOnlyList<string>? Highlights);

internal sealed record ProjectsFileRoot([property: JsonPropertyName("projects")] IReadOnlyList<ProjectFile> Projects);

internal sealed record ProjectFile(
    string Id,
    string? Name,
    string? Client,
    string? ExperienceId,
    string? Sector,
    bool? Featured,
    bool? PubliclySourced,
    string? Summary,
    string? CvSummary,
    string? Contribution,
    IReadOnlyList<string>? Technologies,
    IReadOnlyList<SourceFile>? Sources);

internal sealed record SourceFile(string Url, string Checked);

internal sealed record SkillsFileRoot([property: JsonPropertyName("categories")] IReadOnlyList<SkillCategoryFile> Categories);

internal sealed record SkillCategoryFile(
    string Id,
    IReadOnlyDictionary<string, string> Label,
    IReadOnlyList<string> Items);

internal sealed record EducationFileRoot([property: JsonPropertyName("education")] IReadOnlyList<EducationFile> Education);

internal sealed record EducationFile(
    string Id,
    string? Degree,
    string? Institution,
    string? Location,
    int? Year);

internal sealed record SocialLinksFileRoot([property: JsonPropertyName("links")] IReadOnlyList<SocialLinkFile> Links);

internal sealed record SocialLinkFile(string Id, string Label, string Url, string Display, bool Public);
