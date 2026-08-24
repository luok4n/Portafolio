using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain;

/// <summary>
/// How long someone has actually worked, given periods that may overlap.
/// </summary>
/// <remarks>
/// This is the one genuine business rule in the domain, and it is not "last end minus first start".
/// Two roles held at the same time — a freelance engagement alongside a full-time job — are one
/// month of experience, not two. Summing period lengths would inflate the number; measuring end to
/// end would count a career break as experience. The union of the months worked is the only
/// definition that survives both cases, and it is the number stated on the CV and on the site, so
/// the two can never disagree.
/// </remarks>
public readonly record struct ProfessionalTenure
{
    private ProfessionalTenure(int uniqueMonths) => UniqueMonths = uniqueMonths;

    public int UniqueMonths { get; }

    public int CompletedYears => UniqueMonths / 12;

    public static ProfessionalTenure Empty { get; } = new(0);

    public static ProfessionalTenure FromPeriods(IEnumerable<DateRange> periods)
    {
        ArgumentNullException.ThrowIfNull(periods);

        var months = new HashSet<int>();
        foreach (var period in periods)
        {
            for (var ordinal = period.Start.Ordinal; ordinal <= period.End.Ordinal; ordinal++)
            {
                months.Add(ordinal);
            }
        }

        return new ProfessionalTenure(months.Count);
    }
}
