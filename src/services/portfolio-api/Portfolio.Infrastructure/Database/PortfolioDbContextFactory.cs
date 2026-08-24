using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Portfolio.Infrastructure.Database;

/// <summary>
/// Lets <c>dotnet ef</c> build the model without booting the API.
/// </summary>
/// <remarks>
/// Migrations are generated from the model, not from a live database, so the connection string here
/// only has to parse — it is never opened during <c>migrations add</c>. Override it with
/// <c>PORTFOLIO_DB</c> when running a command that does touch a database, such as
/// <c>dotnet ef database update</c>.
/// </remarks>
internal sealed class PortfolioDbContextFactory : IDesignTimeDbContextFactory<PortfolioDbContext>
{
    private const string DefaultConnection =
        "Host=localhost;Port=5432;Database=portfolio;Username=portfolio;Password=portfolio";

    public PortfolioDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("PORTFOLIO_DB") ?? DefaultConnection;

        var options = new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseNpgsql(connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new PortfolioDbContext(options);
    }
}
