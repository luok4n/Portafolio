namespace Portfolio.Domain.ValueObjects;

/// <summary>
/// A supported content language. Deliberately a closed set: the portfolio can only serve languages
/// whose content has been written and approved, so an unknown tag falls back rather than producing
/// an empty page.
/// </summary>
public readonly record struct LanguageCode
{
    private LanguageCode(string value) => Value = value;

    public static LanguageCode English { get; } = new("en");

    public static LanguageCode Spanish { get; } = new("es");

    public static LanguageCode Default => English;

    public static IReadOnlyList<LanguageCode> Supported { get; } = [English, Spanish];

    public string Value { get; }

    /// <summary>
    /// Accepts a bare tag ("es") or a regional one ("es-CO", "en-US"): the region is dropped, since
    /// the content is not regionalised.
    /// </summary>
    public static bool TryParse(string? value, out LanguageCode result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var primary = value.Trim();
        var separator = primary.IndexOf('-', StringComparison.Ordinal);
        if (separator > 0)
        {
            primary = primary[..separator];
        }

        foreach (var supported in Supported)
        {
            if (string.Equals(primary, supported.Value, StringComparison.OrdinalIgnoreCase))
            {
                result = supported;
                return true;
            }
        }

        return false;
    }

    public override string ToString() => Value;
}
