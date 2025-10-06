using DevFund.TraktManager.Domain.ValueObjects;

namespace DevFund.TraktManager.Domain.Entities;

/// <summary>
/// Represents an item within the user's Trakt watchlist.
/// </summary>
public sealed class WatchlistEntry
{
    public WatchlistEntry(
        int rank,
        int listItemId,
        DateTimeOffset listedAt,
        WatchlistItemType itemType,
        Movie? movie = null,
        Show? show = null,
        SeasonSummary? season = null,
        Episode? episode = null,
        string? notes = null)
    {
        if (rank <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rank), rank, "Rank must be positive.");
        }

        if (listItemId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(listItemId), listItemId, "List item id must be positive.");
        }

        if (listedAt == default)
        {
            throw new ArgumentException("Listed at timestamp must be specified.", nameof(listedAt));
        }

        ItemType = itemType;
        Movie = movie;
        Show = show;
        Season = season;
        Episode = episode;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes;

        ValidateContent(itemType, movie, show, season, episode);

        Rank = rank;
        ListItemId = listItemId;
        ListedAt = listedAt;
    }

    public int Rank { get; }

    public int ListItemId { get; }

    public DateTimeOffset ListedAt { get; }

    public string? Notes { get; }

    public WatchlistItemType ItemType { get; }

    public Movie? Movie { get; }

    public Show? Show { get; }

    public SeasonSummary? Season { get; }

    public Episode? Episode { get; }

    private static void ValidateContent(WatchlistItemType itemType, Movie? movie, Show? show, SeasonSummary? season, Episode? episode)
    {
        switch (itemType)
        {
            case WatchlistItemType.Movie:
                if (movie is null)
                {
                    throw new ArgumentException("A movie entry must include movie details.", nameof(movie));
                }
                break;
            case WatchlistItemType.Show:
                if (show is null)
                {
                    throw new ArgumentException("A show entry must include show details.", nameof(show));
                }
                break;
            case WatchlistItemType.Season:
                if (season is null)
                {
                    throw new ArgumentException("A season entry must include season details.", nameof(season));
                }

                if (show is null)
                {
                    throw new ArgumentException("A season entry must include the parent show details.", nameof(show));
                }
                break;
            case WatchlistItemType.Episode:
                if (episode is null)
                {
                    throw new ArgumentException("An episode entry must include episode details.", nameof(episode));
                }

                if (show is null)
                {
                    throw new ArgumentException("An episode entry must include the parent show details.", nameof(show));
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(itemType), itemType, "Unknown watchlist item type.");
        }
    }
}
