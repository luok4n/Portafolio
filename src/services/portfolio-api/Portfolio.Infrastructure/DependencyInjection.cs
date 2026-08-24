using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Application;
using Portfolio.Application.Abstractions;
using Portfolio.Infrastructure.Json;

namespace Portfolio.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the content source and the query service. The API composes the application this
    /// way rather than reaching for concrete infrastructure types, so phase 4 swaps JSON for
    /// PostgreSQL by changing one registration.
    /// </summary>
    public static IServiceCollection AddPortfolioInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ContentSourceOptions>()
            .Bind(configuration.GetSection(ContentSourceOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Path), "Portfolio:Content:Path must be set.")
            .ValidateOnStart();

        services.AddSingleton<IPortfolioContentSource, JsonFileContentSource>();
        services.AddScoped<PortfolioQueryService>();

        return services;
    }
}
