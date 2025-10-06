namespace DevFund.TraktManager.Domain.Entities;

/// <summary>
/// Represents a scheduled airing of an episode.
/// </summary>
public sealed class CalendarEntry
{
    public CalendarEntry(DateOnly firstAired, Show show, Episode episode)
    {
        if (firstAired == default)
        {
            throw new ArgumentException("First air date must be specified.", nameof(firstAired));
        }

        FirstAired = firstAired;
        Show = show ?? throw new ArgumentNullException(nameof(show));
        Episode = episode ?? throw new ArgumentNullException(nameof(episode));
    }

    public DateOnly FirstAired { get; }

    public Show Show { get; }

    public Episode Episode { get; }

    public bool OccursOn(DateOnly date) => FirstAired == date;
}
