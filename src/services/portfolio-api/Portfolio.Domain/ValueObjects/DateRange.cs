namespace Portfolio.Domain.ValueObjects;

/// <summary>
/// An inclusive range of calendar months. Inclusive on both ends because that is how a CV reads:
/// "Feb 2018 – Jan 2019" is twelve months worked, not eleven.
/// </summary>
public sealed record DateRange
{
    private DateRange(YearMonth start, YearMonth end)
    {
        Start = start;
        End = end;
    }

    public YearMonth Start { get; }

    public YearMonth End { get; }

    public int MonthCount => End.Ordinal - Start.Ordinal + 1;

    public static DateRange Create(YearMonth start, YearMonth end) =>
        start > end
            ? throw new ArgumentException($"A period cannot end ({end}) before it starts ({start}).", nameof(end))
            : new DateRange(start, end);

    public IEnumerable<YearMonth> Months()
    {
        for (var ordinal = Start.Ordinal; ordinal <= End.Ordinal; ordinal++)
        {
            yield return YearMonth.FromOrdinal(ordinal);
        }
    }

    public bool Overlaps(DateRange other) => Start <= other.End && other.Start <= End;

    public override string ToString() => $"{Start} – {End}";
}
