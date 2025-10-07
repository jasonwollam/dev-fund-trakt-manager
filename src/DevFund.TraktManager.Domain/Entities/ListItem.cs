using DevFund.TraktManager.Domain.ValueObjects;

namespace DevFund.TraktManager.Domain.Entities;

/// <summary>
/// Represents an item within a user-created list.
/// </summary>
public sealed class ListItem
{
    public ListItem(
        int rank,
        int listItemId,
        DateTimeOffset listedAt,
        ListItemType itemType,
        Movie? movie = null,
        Show? show = null,
        SeasonSummary? season = null,
        Episode? episode = null,
        Person? person = null,
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
        Person = person;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes;

        ValidateContent(itemType, movie, show, season, episode, person);

        Rank = rank;
        ListItemId = listItemId;
        ListedAt = listedAt;
    }

    public int Rank { get; }

    public int ListItemId { get; }

    public DateTimeOffset ListedAt { get; }

    public string? Notes { get; }

    public ListItemType ItemType { get; }

    public Movie? Movie { get; }

    public Show? Show { get; }

    public SeasonSummary? Season { get; }

    public Episode? Episode { get; }

    public Person? Person { get; }

    private static void ValidateContent(ListItemType itemType, Movie? movie, Show? show, SeasonSummary? season, Episode? episode, Person? person)
    {
        switch (itemType)
        {
            case ListItemType.Movie:
                if (movie is null)
                {
                    throw new ArgumentException("A movie list item must include movie details.", nameof(movie));
                }
                break;

            case ListItemType.Show:
                if (show is null)
                {
                    throw new ArgumentException("A show list item must include show details.", nameof(show));
                }
                break;

            case ListItemType.Season:
                if (season is null)
                {
                    throw new ArgumentException("A season list item must include season details.", nameof(season));
                }

                if (show is null)
                {
                    throw new ArgumentException("A season list item must include the parent show details.", nameof(show));
                }
                break;

            case ListItemType.Episode:
                if (episode is null)
                {
                    throw new ArgumentException("An episode list item must include episode details.", nameof(episode));
                }

                if (show is null)
                {
                    throw new ArgumentException("An episode list item must include the parent show details.", nameof(show));
                }
                break;

            case ListItemType.Person:
                if (person is null)
                {
                    throw new ArgumentException("A person list item must include person details.", nameof(person));
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(itemType), itemType, "Unknown list item type.");
        }
    }
}