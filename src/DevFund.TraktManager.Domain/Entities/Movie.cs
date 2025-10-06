using DevFund.TraktManager.Domain.ValueObjects;

namespace DevFund.TraktManager.Domain.Entities;

/// <summary>
/// Minimal representation of a movie used across application layers.
/// </summary>
public sealed class Movie
{
    public Movie(string title, int? year, TraktIds ids)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        if (year is < 1888)
        {
            throw new ArgumentOutOfRangeException(nameof(year), year, "Year must be null or greater than 1888.");
        }

        Title = title;
        Year = year;
        Ids = ids ?? throw new ArgumentNullException(nameof(ids));
    }

    public string Title { get; }

    public int? Year { get; }

    public TraktIds Ids { get; }

    public override string ToString() => Year.HasValue ? $"{Title} ({Year})" : Title;
}
