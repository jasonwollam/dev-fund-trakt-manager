using DevFund.TraktManager.Domain.ValueObjects;

namespace DevFund.TraktManager.Domain.Entities;

/// <summary>
/// Represents a single episode of a show.
/// </summary>
public sealed class Episode
{
    public Episode(int season, int number, string title, TraktIds ids)
    {
        if (season < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(season), season, "Season must be zero or positive.");
        }

        if (number <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(number), number, "Episode number must be positive.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        Season = season;
        Number = number;
        Title = title;
        Ids = ids ?? throw new ArgumentNullException(nameof(ids));
    }

    public int Season { get; }

    public int Number { get; }

    public string Title { get; }

    public TraktIds Ids { get; }

    public override string ToString() => $"S{Season:00}E{Number:00} · {Title}";
}
