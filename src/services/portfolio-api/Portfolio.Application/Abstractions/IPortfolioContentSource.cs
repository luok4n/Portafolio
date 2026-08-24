using Portfolio.Domain;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Application.Abstractions;

/// <summary>
/// Where resolved portfolio content comes from. The application layer does not care whether that is
/// JSON on disk (phase 3) or PostgreSQL (phase 4) — swapping one for the other must not reach past
/// this interface.
/// </summary>
public interface IPortfolioContentSource
{
    Task<PortfolioContent> GetAsync(LanguageCode language, CancellationToken cancellationToken = default);
}
