using System.Globalization;

namespace Portfolio.Domain.ValueObjects;

/// <summary>
/// A calendar month. Employment is recorded to month precision, not day precision — the CV never
/// knew the days, and pretending otherwise would invent data.
/// </summary>
public readonly record struct YearMonth : IComparable<YearMonth>
{
    public YearMonth(int year, int month)
    {
        if (year is < 1900 or > 2999)
        {
            throw new ArgumentOutOfRangeException(nameof(year), year, "Year is outside the supported range.");
        }

        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), month, "Month must be between 1 and 12.");
        }

        Year = year;
        Month = month;
    }

    public int Year { get; }

    public int Month { get; }

    /// <summary>Months since year zero. Makes ordering and arithmetic a single integer operation.</summary>
    public int Ordinal => (Year * 12) + (Month - 1);

    public static YearMonth FromOrdinal(int ordinal) => new(ordinal / 12, (ordinal % 12) + 1);

    public static YearMonth Parse(string value) =>
        TryParse(value, out var result)
            ? result
            : throw new FormatException($"'{value}' is not a valid YYYY-MM value.");

    public static bool TryParse(string? value, out YearMonth result)
    {
        result = default;
        if (value is null || value.Length != 7 || value[4] != '-')
        {
            return false;
        }

        if (!int.TryParse(value.AsSpan(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out var year) ||
            !int.TryParse(value.AsSpan(5, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var month) ||
            month is < 1 or > 12 ||
            year is < 1900 or > 2999)
        {
            return false;
        }

        result = new YearMonth(year, month);
        return true;
    }

    public int CompareTo(YearMonth other) => Ordinal.CompareTo(other.Ordinal);

    public static bool operator <(YearMonth left, YearMonth right) => left.Ordinal < right.Ordinal;

    public static bool operator >(YearMonth left, YearMonth right) => left.Ordinal > right.Ordinal;

    public static bool operator <=(YearMonth left, YearMonth right) => left.Ordinal <= right.Ordinal;

    public static bool operator >=(YearMonth left, YearMonth right) => left.Ordinal >= right.Ordinal;

    public override string ToString() => $"{Year:D4}-{Month:D2}";
}
