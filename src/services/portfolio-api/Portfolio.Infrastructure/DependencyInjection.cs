using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Portfolio.Application;
using Portfolio.Application.Abstractions;
using Portfolio.Infrastructure.Database;
using Portfolio.Infrastructure.Json;

namespace Portfolio.Infrastructure;

public sealed class DatabaseOptions
{
    public const string SectionName = "Portfolio:Database";

    /// <summary>
    /// When false the API serves content straight from the files in <c>content/</c>. That is not a
    /// stub: it is a working mode with no database to run, which keeps the frontend work in phase 5
    /// and the prerender build in phase 10 from depending on PostgreSQL being up.
    /// </summary>
    public bool Enabled { get; set; }

    public string ConnectionString { get; set; } = string.Empty;

    public bool MigrateOnStartup { get; set; } = true;

    public bool SeedOnStartup { get; set; } = true;
}

public static partial class DependencyInjection
{
    /// <summary>
    /// Registers the content source and the query service. Which source is used is a configuration
    /// decision; nothing above <see cref="IPortfolioContentSource"/> can tell the difference.
    /// </summary>
    public static IServiceCollection AddPortfolioInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<ContentSourceOptions>()
            .Bind(configuration.GetSection(ContentSourceOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Path), "Portfolio:Content:Path must be set.")
            .ValidateOnStart();

        var database = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .Validate(
                o => !o.Enabled || !string.IsNullOrWhiteSpace(o.ConnectionString),
                "Portfolio:Database:ConnectionString is required when the database is enabled.")
            .ValidateOnStart();

        // Always registered: it is the file source in file mode, and the seed loader in database
        // mode. Either way the merge between base locale and translation is implemented once.
        services.AddSingleton<JsonFileContentSource>();

        if (database.Enabled)
        {
            services.AddDbContext<PortfolioDbContext>(options => options
                .UseNpgsql(database.ConnectionString, npgsql => npgsql.EnableRetryOnFailure())
                .UseSnakeCaseNamingConvention());

            services.AddScoped<IPortfolioContentSource, EfPortfolioContentSource>();
            services.AddScoped<ContentSeeder>();
        }
        else
        {
            services.AddScoped<IPortfolioContentSource>(sp => sp.GetRequiredService<JsonFileContentSource>());
        }

        services.AddScoped<PortfolioQueryService>();

        return services;
    }

    /// <summary>
    /// Applies migrations and loads the content, when the database is enabled and configured to do
    /// so. Safe to call in file mode: it does nothing.
    /// </summary>
    /// <remarks>
    /// Migrating on startup is right for this project — one small service, one deployment unit, a
    /// schema only this application owns. It stops being right the day a second replica can start
    /// simultaneously against an unmigrated database — the first thing to change if this ever runs more
    /// than one replica.
    /// </remarks>
    public static async Task InitialisePortfolioDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        var options = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>().Value;
        if (!options.Enabled)
        {
            return;
        }

        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Portfolio.Infrastructure.Database");

        if (options.MigrateOnStartup)
        {
            var context = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            LogSchemaUpToDate(logger);
        }

        if (options.SeedOnStartup)
        {
            var seeder = scope.ServiceProvider.GetRequiredService<ContentSeeder>();
            await seeder.SeedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    [LoggerMessage(EventId = 2100, Level = LogLevel.Information, Message = "Database schema is up to date.")]
    private static partial void LogSchemaUpToDate(ILogger logger);
}
