using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portfolio.Application.Abstractions;
using Portfolio.Domain;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Infrastructure.Json;

public sealed class ContentSourceOptions
{
    public const string SectionName = "Portfolio:Content";

    /// <summary>Directory holding the reviewed content files. Absolute, or relative to the app base path.</summary>
    public string Path { get; set; } = "content";
}

/// <summary>
/// Reads the reviewed content files and turns them into domain objects.
/// </summary>
/// <remarks>
/// Phase 3 serves content straight from <c>content/</c>. Phase 4 replaces this with PostgreSQL and
/// reuses these same files as the seed, so the shape the API returns does not change when the
/// storage does — which is the point of putting <see cref="IPortfolioContentSource"/> between them.
///
/// The base locale owns every fact; a translation file carries only translated fields and is merged
/// over the base by id. Storing a date twice, once per language, is how the two stop agreeing.
/// </remarks>
public sealed partial class JsonFileContentSource : IPortfolioContentSource
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);
    private readonly string _root;
    private readonly ILogger<JsonFileContentSource> _logger;

    public JsonFileContentSource(
        IOptions<ContentSourceOptions> options,
        ILogger<JsonFileContentSource> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _logger = logger;
        _root = System.IO.Path.IsPathRooted(options.Value.Path)
            ? options.Value.Path
            : System.IO.Path.Combine(AppContext.BaseDirectory, options.Value.Path);
    }

    public Task<PortfolioContent> GetAsync(LanguageCode language, CancellationToken cancellationToken = default)
    {
        var stamp = CurrentStamp();
        if (_cache.TryGetValue(language.Value, out var cached) && cached.Stamp == stamp)
        {
            return Task.FromResult(cached.Content);
        }

        var content = Load(language);
        _cache[language.Value] = new CacheEntry(content, stamp);
        LogLoaded(_logger, language.Value, content.Experience.Count, content.Projects.Count);
        return Task.FromResult(content);
    }

    /// <summary>
    /// Newest write time across the content directory. Cheap to compute and means an edit to a
    /// content file shows up without restarting the API, without a file watcher to leak.
    /// </summary>
    private long CurrentStamp()
    {
        if (!Directory.Exists(_root))
        {
            throw new DirectoryNotFoundException($"Content directory not found: {_root}");
        }

        long newest = 0;
        foreach (var file in Directory.EnumerateFiles(_root, "*.json", SearchOption.TopDirectoryOnly))
        {
            var ticks = File.GetLastWriteTimeUtc(file).Ticks;
            if (ticks > newest)
            {
                newest = ticks;
            }
        }

        return newest;
    }

    private PortfolioContent Load(LanguageCode language)
    {
        var isBase = language == LanguageCode.Default;
        var suffix = language.Value;
        var baseSuffix = LanguageCode.Default.Value;

        var baseProfile = Read<ProfileFile>($"profile.{baseSuffix}.json");
        var profile = isBase ? baseProfile : Merge(baseProfile, ReadOptional<ProfileFile>($"profile.{suffix}.json"));

        var baseExperience = Read<ExperienceFileRoot>($"experience.{baseSuffix}.json").Experience;
        var experienceTranslations = isBase
            ? []
            : Index(ReadOptional<ExperienceFileRoot>($"experience.{suffix}.json")?.Experience, e => e.Id);

        var baseProjects = Read<ProjectsFileRoot>($"projects.{baseSuffix}.json").Projects;
        var projectTranslations = isBase
            ? []
            : Index(ReadOptional<ProjectsFileRoot>($"projects.{suffix}.json")?.Projects, p => p.Id);

        var baseEducation = Read<EducationFileRoot>($"education.{baseSuffix}.json").Education;
        var educationTranslations = isBase
            ? []
            : Index(ReadOptional<EducationFileRoot>($"education.{suffix}.json")?.Education, e => e.Id);

        var skills = Read<SkillsFileRoot>("skills.json").Categories;
        var links = Read<SocialLinksFileRoot>("social-links.json").Links;

        var projects = baseProjects.Select(p => MapProject(p, projectTranslations.GetValueOrDefault(p.Id))).ToList();

        // A role's project list is derived from the projects themselves, in the order they are
        // authored in projects.*.json — the same rule the database uses, so the two sources cannot
        // disagree about ordering. The `projects` array inside experience.*.json stays as a
        // human-readable cross-reference that the content validator checks for membership.
        var projectsByExperience = projects
            .GroupBy(p => p.ExperienceId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g.Select(p => p.Id).ToList(),
                StringComparer.OrdinalIgnoreCase);

        return new PortfolioContent(
            language,
            MapProfile(profile),
            baseExperience
                .Select(e => MapExperience(
                    e,
                    experienceTranslations.GetValueOrDefault(e.Id),
                    projectsByExperience.GetValueOrDefault(e.Id, [])))
                .ToList(),
            projects,
            skills.Select(s => MapSkills(s, language)).ToList(),
            baseEducation.Select(e => MapEducation(e, educationTranslations.GetValueOrDefault(e.Id))).ToList(),
            links.Select(l => new SocialLink(l.Id, l.Label, l.Url, l.Display, l.Public)).ToList());
    }

    private static Dictionary<string, T> Index<T>(IReadOnlyList<T>? items, Func<T, string> idOf)
        where T : class
    {
        var map = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items ?? [])
        {
            map[idOf(item)] = item;
        }

        return map;
    }

    private static ProfileFile Merge(ProfileFile @base, ProfileFile? translation) =>
        translation is null
            ? @base
            : @base with
            {
                Name = translation.Name ?? @base.Name,
                Headline = translation.Headline ?? @base.Headline,
                Title = translation.Title ?? @base.Title,
                Location = translation.Location ?? @base.Location,
                SummaryTemplate = translation.SummaryTemplate ?? @base.SummaryTemplate,
                Languages = translation.Languages ?? @base.Languages,
            };

    private static ProfileInfo MapProfile(ProfileFile f) => new(
        Required(f.Name, "profile.name"),
        Required(f.Headline, "profile.headline"),
        Required(f.Title, "profile.title"),
        Required(f.Location, "profile.location"),
        Required(f.Email, "profile.email"),
        f.Availability ?? "unknown",
        Required(f.SummaryTemplate, "profile.summaryTemplate"),
        (f.Languages ?? []).Select(l => new SpokenLanguage(l.Language, l.Level)).ToList());

    private static WorkExperience MapExperience(ExperienceFile b, ExperienceFile? t, IReadOnlyList<string> projectIds) => new(
        b.Id,
        t?.Company ?? Required(b.Company, $"experience[{b.Id}].company"),
        t?.Role ?? Required(b.Role, $"experience[{b.Id}].role"),
        t?.EmploymentType ?? b.EmploymentType ?? string.Empty,
        DateRange.Create(
            YearMonth.Parse(Required(b.Start, $"experience[{b.Id}].start")),
            YearMonth.Parse(Required(b.End, $"experience[{b.Id}].end"))),
        projectIds,
        b.ParallelWith ?? [],
        t?.Teams ?? b.Teams ?? [],
        b.Technologies ?? [],
        t?.Highlights ?? b.Highlights ?? []);

    private static Project MapProject(ProjectFile b, ProjectFile? t) => new(
        b.Id,
        t?.Name ?? Required(b.Name, $"projects[{b.Id}].name"),
        t?.Client ?? b.Client ?? string.Empty,
        Required(b.ExperienceId, $"projects[{b.Id}].experienceId"),
        t?.Sector ?? b.Sector ?? string.Empty,
        b.Featured ?? false,
        b.PubliclySourced ?? false,
        t?.Summary ?? Required(b.Summary, $"projects[{b.Id}].summary"),
        t?.CvSummary ?? b.CvSummary,
        t?.Contribution ?? b.Contribution,
        b.Technologies ?? [],
        (b.Sources ?? []).Select(s => new SourceReference(s.Url, ParseDate(s.Checked))).ToList());

    private static SkillCategory MapSkills(SkillCategoryFile f, LanguageCode language)
    {
        // Category labels are translated inline; technology names never are.
        var label = f.Label.TryGetValue(language.Value, out var localised)
            ? localised
            : f.Label.GetValueOrDefault(LanguageCode.Default.Value, f.Id);

        return new SkillCategory(f.Id, label, f.Items);
    }

    private static EducationEntry MapEducation(EducationFile b, EducationFile? t) => new(
        b.Id,
        t?.Degree ?? Required(b.Degree, $"education[{b.Id}].degree"),
        t?.Institution ?? b.Institution ?? string.Empty,
        t?.Location ?? b.Location ?? string.Empty,
        b.Year ?? throw new InvalidContentException($"education[{b.Id}].year is required."));

    private static DateOnly ParseDate(string value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : throw new InvalidContentException($"'{value}' is not a valid yyyy-MM-dd date.");

    private static string Required(string? value, string field) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidContentException($"{field} is required in the base locale.")
            : value;

    private T Read<T>(string fileName)
        where T : class
    {
        var path = System.IO.Path.Combine(_root, fileName);
        if (!File.Exists(path))
        {
            throw new InvalidContentException($"Content file not found: {path}");
        }

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<T>(stream, SerializerOptions)
            ?? throw new InvalidContentException($"{fileName} deserialised to null.");
    }

    private T? ReadOptional<T>(string fileName)
        where T : class
    {
        var path = System.IO.Path.Combine(_root, fileName);
        if (!File.Exists(path))
        {
            LogMissingTranslation(_logger, fileName);
            return null;
        }

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<T>(stream, SerializerOptions);
    }

    [LoggerMessage(EventId = 1000, Level = LogLevel.Information,
        Message = "Loaded portfolio content for '{Language}': {ExperienceCount} roles, {ProjectCount} projects.")]
    private static partial void LogLoaded(ILogger logger, string language, int experienceCount, int projectCount);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning,
        Message = "Translation file '{FileName}' not found; falling back to the base locale for those fields.")]
    private static partial void LogMissingTranslation(ILogger logger, string fileName);

    private sealed record CacheEntry(PortfolioContent Content, long Stamp);
}

/// <summary>Raised when the content files are present but wrong. Never a client's fault, so it is a 500.</summary>
public sealed class InvalidContentException(string message) : Exception(message);
