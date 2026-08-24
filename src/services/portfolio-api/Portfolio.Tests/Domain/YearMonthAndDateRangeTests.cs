using Portfolio.Domain.ValueObjects;

namespace Portfolio.Tests.Domain;

public sealed class YearMonthTests
{
    [Theory]
    [InlineData("2019-01", 2019, 1)]
    [InlineData("2026-12", 2026, 12)]
    public void Parses_valid_values(string value, int year, int month)
    {
        var parsed = YearMonth.Parse(value);

        Assert.Equal(year, parsed.Year);
        Assert.Equal(month, parsed.Month);
        Assert.Equal(value, parsed.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2019-1")]
    [InlineData("2019/01")]
    [InlineData("2019-13")]
    [InlineData("2019-00")]
    [InlineData("19-01")]
    [InlineData("not-a-date")]
    [InlineData("1899-12")]
    public void Rejects_invalid_values(string? value)
    {
        Assert.False(YearMonth.TryParse(value, out _));
    }

    [Fact]
    public void Orders_chronologically_across_a_year_boundary()
    {
        Assert.True(YearMonth.Parse("2018-12") < YearMonth.Parse("2019-01"));
        Assert.True(YearMonth.Parse("2019-02") > YearMonth.Parse("2019-01"));
    }

    [Fact]
    public void Round_trips_through_its_ordinal()
    {
        var original = YearMonth.Parse("2023-07");

        Assert.Equal(original, YearMonth.FromOrdinal(original.Ordinal));
    }
}

public sealed class DateRangeTests
{
    [Fact]
    public void Counts_both_end_months()
    {
        var range = DateRange.Create(YearMonth.Parse("2022-01"), YearMonth.Parse("2022-12"));

        Assert.Equal(12, range.MonthCount);
    }

    [Fact]
    public void A_single_month_is_one_month()
    {
        var range = DateRange.Create(YearMonth.Parse("2022-05"), YearMonth.Parse("2022-05"));

        Assert.Equal(1, range.MonthCount);
        Assert.Single(range.Months());
    }

    [Fact]
    public void Refuses_to_end_before_it_starts()
    {
        // Bad content should fail loudly at load time, not produce a negative duration on a page.
        Assert.Throws<ArgumentException>(() =>
            DateRange.Create(YearMonth.Parse("2022-12"), YearMonth.Parse("2022-01")));
    }

    [Theory]
    [InlineData("2022-01", "2022-06", "2022-05", "2022-09", true)]
    [InlineData("2022-01", "2022-06", "2022-06", "2022-09", true)]
    [InlineData("2022-01", "2022-06", "2022-07", "2022-09", false)]
    public void Detects_overlap(string aStart, string aEnd, string bStart, string bEnd, bool expected)
    {
        var a = DateRange.Create(YearMonth.Parse(aStart), YearMonth.Parse(aEnd));
        var b = DateRange.Create(YearMonth.Parse(bStart), YearMonth.Parse(bEnd));

        Assert.Equal(expected, a.Overlaps(b));
        Assert.Equal(expected, b.Overlaps(a));
    }
}
