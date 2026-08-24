using Portfolio.Domain;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Tests.Domain;

public sealed class ProfessionalTenureTests
{
    private static DateRange Period(string start, string end) =>
        DateRange.Create(YearMonth.Parse(start), YearMonth.Parse(end));

    [Fact]
    public void No_periods_is_no_experience()
    {
        var tenure = ProfessionalTenure.FromPeriods([]);

        Assert.Equal(0, tenure.UniqueMonths);
        Assert.Equal(0, tenure.CompletedYears);
    }

    [Fact]
    public void A_period_is_inclusive_of_both_ends()
    {
        // Feb 2018 to Jan 2019 is how a CV reads twelve months, not eleven.
        var tenure = ProfessionalTenure.FromPeriods([Period("2018-02", "2019-01")]);

        Assert.Equal(12, tenure.UniqueMonths);
        Assert.Equal(1, tenure.CompletedYears);
    }

    [Fact]
    public void Overlapping_roles_are_counted_once()
    {
        // The whole reason this rule exists: a freelance engagement running alongside a full-time
        // job is not double the experience.
        var tenure = ProfessionalTenure.FromPeriods([
            Period("2022-01", "2022-12"),
            Period("2022-03", "2022-12"),
        ]);

        Assert.Equal(12, tenure.UniqueMonths);
    }

    [Fact]
    public void Partially_overlapping_roles_count_the_union()
    {
        var tenure = ProfessionalTenure.FromPeriods([
            Period("2020-01", "2020-06"),
            Period("2020-05", "2020-09"),
        ]);

        Assert.Equal(9, tenure.UniqueMonths);
    }

    [Fact]
    public void A_gap_between_roles_is_not_counted()
    {
        // Measuring first start to last end would call this 24 months. It is 12.
        var tenure = ProfessionalTenure.FromPeriods([
            Period("2020-01", "2020-06"),
            Period("2021-07", "2021-12"),
        ]);

        Assert.Equal(12, tenure.UniqueMonths);
    }

    [Fact]
    public void A_role_fully_inside_another_adds_nothing()
    {
        var tenure = ProfessionalTenure.FromPeriods([
            Period("2020-01", "2020-12"),
            Period("2020-04", "2020-06"),
        ]);

        Assert.Equal(12, tenure.UniqueMonths);
    }

    [Fact]
    public void Years_are_completed_years_not_rounded()
    {
        // 8.5 years of experience is "8+ years" on a CV, never "9".
        var tenure = ProfessionalTenure.FromPeriods([Period("2018-01", "2026-06")]);

        Assert.Equal(102, tenure.UniqueMonths);
        Assert.Equal(8, tenure.CompletedYears);
    }

    [Fact]
    public void The_real_career_adds_up_to_the_number_on_the_cv()
    {
        // Guards the headline figure end to end. If this breaks, either the content changed or the
        // rule did — and either way the CV and the site must not disagree.
        var tenure = ProfessionalTenure.FromPeriods([
            Period("2018-02", "2019-01"), // Woldev
            Period("2019-01", "2019-10"), // Universidad Tecnológica de Pereira
            Period("2019-10", "2021-12"), // MVM
            Period("2022-01", "2022-12"), // LendingFront
            Period("2022-03", "2022-12"), // AES Chivor, concurrent with LendingFront
            Period("2022-12", "2026-07"), // Adagetech
        ]);

        Assert.Equal(102, tenure.UniqueMonths);
        Assert.Equal(8, tenure.CompletedYears);
    }
}
