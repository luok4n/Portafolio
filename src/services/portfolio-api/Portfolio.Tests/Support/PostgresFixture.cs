using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Portfolio.Application.Abstractions;
using Portfolio.Infrastructure;
using Testcontainers.PostgreSql;

namespace Portfolio.Tests.Support;

/// <summary>
/// A real PostgreSQL for the tests that are only meaningful against one.
/// </summary>
/// <remarks>
/// An in-memory provider would not exercise the migration, the check constraints, the snake_case
/// mapping or the transaction the seeder opens through the retrying execution strategy — which is
/// most of what phase 4 actually built. Testing those against a fake would prove nothing about the
/// database the site runs on.
///
/// One container is shared across the class: starting PostgreSQL per test would trade minutes for
/// isolation that these read-only tests do not need.
/// </remarks>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17.5-alpine")
        .WithDatabase("portfolio")
        .WithUsername("portfolio")
        .WithPassword("portfolio")
        .Build();

    private ServiceProvider? _services;

    public string ConnectionString => _container.GetConnectionString();

    public bool Started { get; private set; }

    public async Task InitializeAsync()
    {
        if (!DockerProbe.IsAvailable)
        {
            return;
        }

        await _container.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Portfolio:Content:Path"] = TestContent.Directory,
                ["Portfolio:Database:Enabled"] = "true",
                ["Portfolio:Database:ConnectionString"] = ConnectionString,
                ["Portfolio:Database:MigrateOnStartup"] = "true",
                ["Portfolio:Database:SeedOnStartup"] = "true",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(b => b.ClearProviders());
        services.AddPortfolioInfrastructure(configuration);
        _services = services.BuildServiceProvider();

        // Exactly what the API does at startup, so the test covers the real path rather than a
        // reimplementation of it.
        await _services.InitialisePortfolioDatabaseAsync();

        Started = true;
    }

    /// <summary>Resolves the database-backed content source, in its own scope like a request would.</summary>
    public async Task<T> WithScopeAsync<T>(Func<IServiceProvider, Task<T>> work)
    {
        ArgumentNullException.ThrowIfNull(work);
        await using var scope = Services.CreateAsyncScope();
        return await work(scope.ServiceProvider);
    }

    public Task<IPortfolioContentSource> ResolveSourceAsync() =>
        Task.FromResult(Services.CreateScope().ServiceProvider.GetRequiredService<IPortfolioContentSource>());

    public IServiceProvider Services =>
        _services ?? throw new InvalidOperationException("The fixture did not start.");

    /// <summary>Runs the startup path again, which is what a redeploy does.</summary>
    public Task ReinitialiseAsync() => Services.InitialisePortfolioDatabaseAsync();

    public async Task DisposeAsync()
    {
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }

        if (Started)
        {
            await _container.DisposeAsync();
        }
    }
}
