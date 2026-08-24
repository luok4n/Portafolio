using Portfolio.Domain.ValueObjects;

namespace Portfolio.Application;

/// <summary>How the served language was decided. Returned to the caller so the choice is never a guess.</summary>
public enum LanguageSource
{
    /// <summary>An explicit <c>?lang=</c> parameter.</summary>
    Explicit,

    /// <summary>The <c>Accept-Language</c> header.</summary>
    AcceptHeader,

    /// <summary>Nothing usable was supplied, or what was supplied is not a supported language.</summary>
    Fallback,
}

public readonly record struct NegotiatedLanguage(LanguageCode Language, LanguageSource Source);

/// <summary>
/// Decides which language to serve: explicit parameter, then <c>Accept-Language</c>, then the
/// default. Pure and dependency-free so the fallback chain can be tested exhaustively, including
/// the cases that actually occur in the wild — regional tags, quality values, unsupported
/// languages, and headers that are simply malformed.
/// </summary>
public static class LanguageNegotiator
{
    public static NegotiatedLanguage Negotiate(string? explicitLanguage, string? acceptLanguageHeader)
    {
        if (LanguageCode.TryParse(explicitLanguage, out var explicitCode))
        {
            return new NegotiatedLanguage(explicitCode, LanguageSource.Explicit);
        }

        foreach (var candidate in ParseAcceptLanguage(acceptLanguageHeader))
        {
            if (LanguageCode.TryParse(candidate, out var headerCode))
            {
                return new NegotiatedLanguage(headerCode, LanguageSource.AcceptHeader);
            }
        }

        return new NegotiatedLanguage(LanguageCode.Default, LanguageSource.Fallback);
    }

    /// <summary>
    /// Yields the header's language tags in descending quality order. A malformed entry is skipped
    /// rather than throwing: a broken header from some client is not a reason to fail a request for
    /// a public page.
    /// </summary>
    private static IEnumerable<string> ParseAcceptLanguage(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return [];
        }

        return header
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseEntry)
            .Where(entry => entry.Quality > 0 && entry.Tag.Length > 0)
            .OrderByDescending(entry => entry.Quality)
            .Select(entry => entry.Tag);
    }

    private static (string Tag, double Quality) ParseEntry(string entry)
    {
        var parts = entry.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var tag = parts.Length > 0 ? parts[0] : string.Empty;

        var quality = 1.0d;
        for (var i = 1; i < parts.Length; i++)
        {
            if (!parts[i].StartsWith("q=", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (double.TryParse(
                    parts[i].AsSpan(2),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var parsed))
            {
                quality = parsed;
            }
            else
            {
                // An unparseable q-value means the entry cannot be ranked; drop it rather than
                // silently promoting it to the top with the default quality of 1.0.
                quality = 0;
            }
        }

        return (tag, quality);
    }
}
