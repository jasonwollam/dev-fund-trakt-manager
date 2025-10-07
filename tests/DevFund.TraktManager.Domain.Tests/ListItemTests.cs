using DevFund.TraktManager.Domain.Entities;
using DevFund.TraktManager.Domain.ValueObjects;

namespace DevFund.TraktManager.Domain.Tests;

public class ListItemTests
{
    [Fact]
    public void Constructor_CreatesMovieItem()
    {
        var ids = new TraktIds(1, "movie-slug");
        var movie = new Movie("Example", 2024, ids);

        var item = new ListItem(1, 100, DateTimeOffset.UtcNow, ListItemType.Movie, movie: movie);

        Assert.Equal(ListItemType.Movie, item.ItemType);
        Assert.Equal(movie, item.Movie);
    }

    [Fact]
    public void Constructor_Throws_WhenEpisodeMissingShow()
    {
        var episodeIds = new TraktIds(1, "ep-slug");
        var episode = new Episode(1, 1, "Pilot", episodeIds);

        Assert.Throws<ArgumentException>(() => new ListItem(1, 1, DateTimeOffset.UtcNow, ListItemType.Episode, episode: episode));
    }

    [Fact]
    public void Constructor_Throws_WhenPersonMissingDetails()
    {
        Assert.Throws<ArgumentException>(() => new ListItem(1, 1, DateTimeOffset.UtcNow, ListItemType.Person));
    }
}