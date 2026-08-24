using Microsoft.AspNetCore.Mvc;
using Portfolio.Application;
using Portfolio.Application.Contracts;

namespace Portfolio.Api.Endpoints;

/// <summary>
/// The HTTP surface. Endpoints do routing, binding and status codes and nothing else — every
/// decision about what to return lives in <see cref="PortfolioQueryService"/>.
/// </summary>
public static class PortfolioEndpoints
{
    public static IEndpointRouteBuilder MapPortfolioEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var api = app.MapGroup("/api")
            .WithTags("Portfolio")
            .CacheOutput();

        // One call returns everything. The site always renders the whole portfolio and the
        // build-time snapshot wants exactly this shape, so seven round trips would be six too many.
        api.MapGet("/content", async (
                [FromQuery] string? lang,
                [FromHeader(Name = "Accept-Language")] string? acceptLanguage,
                PortfolioQueryService queries,
                CancellationToken ct) =>
            Results.Ok(await queries.GetContentAsync(lang, acceptLanguage, ct).ConfigureAwait(false)))
            .WithName("GetContent")
            .WithSummary("The whole portfolio for one language.")
            .Produces<PortfolioContentDto>();

        api.MapGet("/profile", async (
                [FromQuery] string? lang,
                [FromHeader(Name = "Accept-Language")] string? acceptLanguage,
                PortfolioQueryService queries,
                CancellationToken ct) =>
            Results.Ok(await queries.GetProfileAsync(lang, acceptLanguage, ct).ConfigureAwait(false)))
            .WithName("GetProfile")
            .WithSummary("Profile, summary and computed years of experience.")
            .Produces<ProfileDto>();

        api.MapGet("/experience", async (
                [FromQuery] string? lang,
                [FromHeader(Name = "Accept-Language")] string? acceptLanguage,
                PortfolioQueryService queries,
                CancellationToken ct) =>
            Results.Ok(await queries.GetExperienceAsync(lang, acceptLanguage, ct).ConfigureAwait(false)))
            .WithName("GetExperience")
            .WithSummary("Work history, newest first, with concurrent roles flagged.")
            .Produces<IReadOnlyList<ExperienceDto>>();

        api.MapGet("/skills", async (
                [FromQuery] string? lang,
                [FromHeader(Name = "Accept-Language")] string? acceptLanguage,
                PortfolioQueryService queries,
                CancellationToken ct) =>
            Results.Ok(await queries.GetSkillsAsync(lang, acceptLanguage, ct).ConfigureAwait(false)))
            .WithName("GetSkills")
            .WithSummary("Skill categories with localised labels.")
            .Produces<IReadOnlyList<SkillCategoryDto>>();

        api.MapGet("/projects", async (
                [FromQuery] string? lang,
                [FromHeader(Name = "Accept-Language")] string? acceptLanguage,
                PortfolioQueryService queries,
                CancellationToken ct) =>
            Results.Ok(await queries.GetProjectsAsync(lang, acceptLanguage, ct).ConfigureAwait(false)))
            .WithName("GetProjects")
            .WithSummary("All projects, with their public sources.")
            .Produces<IReadOnlyList<ProjectDto>>();

        api.MapGet("/projects/{id}", async (
                string id,
                [FromQuery] string? lang,
                [FromHeader(Name = "Accept-Language")] string? acceptLanguage,
                PortfolioQueryService queries,
                CancellationToken ct) =>
            Results.Ok(await queries.GetProjectAsync(id, lang, acceptLanguage, ct).ConfigureAwait(false)))
            .WithName("GetProject")
            .WithSummary("One project by id.")
            .Produces<ProjectDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        api.MapGet("/education", async (
                [FromQuery] string? lang,
                [FromHeader(Name = "Accept-Language")] string? acceptLanguage,
                PortfolioQueryService queries,
                CancellationToken ct) =>
            Results.Ok(await queries.GetEducationAsync(lang, acceptLanguage, ct).ConfigureAwait(false)))
            .WithName("GetEducation")
            .WithSummary("Education history.")
            .Produces<IReadOnlyList<EducationDto>>();

        api.MapGet("/social-links", async (PortfolioQueryService queries, CancellationToken ct) =>
            Results.Ok(await queries.GetSocialLinksAsync(ct).ConfigureAwait(false)))
            .WithName("GetSocialLinks")
            .WithSummary("Public links only. Anything not marked public is never served.")
            .Produces<IReadOnlyList<SocialLinkDto>>();

        return app;
    }
}
