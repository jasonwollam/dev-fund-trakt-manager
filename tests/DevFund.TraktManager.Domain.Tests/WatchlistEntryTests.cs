using DevFund.TraktManager.Domain.Entities;
using DevFund.TraktManager.Domain.ValueObjects;

namespace DevFund.TraktManager.Domain.Tests;

public class WatchlistEntryTests
{
    [Fact]
    public void Constructor_CreatesMovieEntry()
    {
        var ids = new TraktIds(1, "movie-slug");
        var movie = new Movie("Inception", 2010, ids);

        var entry = new WatchlistEntry(1, 42, DateTimeOffset.UtcNow, WatchlistItemType.Movie, movie: movie);

        Assert.Equal(WatchlistItemType.Movie, entry.ItemType);
        Assert.Equal(movie, entry.Movie);
        Assert.Null(entry.Show);
    }

    [Fact]
    public void Constructor_Throws_WhenSeasonShowMissing()
    {
        var seasonIds = new SeasonIds(1, null, null);
        var season = new SeasonSummary(1, seasonIds);

        Assert.Throws<ArgumentException>(() =>
            new WatchlistEntry(1, 42, DateTimeOffset.UtcNow, WatchlistItemType.Season, season: season));
    }
}
