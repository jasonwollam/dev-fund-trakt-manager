using DevFund.TraktManager.Domain.Entities;
using DevFund.TraktManager.Domain.ValueObjects;

namespace DevFund.TraktManager.Domain.Tests;

public class SavedFilterTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var now = DateTimeOffset.UtcNow;

        var filter = new SavedFilter(1, 42, SavedFilterSection.Movies, "Weekend", "/users/me/lists/weekend", "sort=rank", now);

        Assert.Equal(1, filter.Rank);
        Assert.Equal(42, filter.Id);
        Assert.Equal(SavedFilterSection.Movies, filter.Section);
        Assert.Equal("Weekend", filter.Name);
        Assert.Equal("/users/me/lists/weekend", filter.Path);
        Assert.Equal("sort=rank", filter.Query);
        Assert.Equal(now, filter.UpdatedAt);
    }

    [Fact]
    public void Constructor_Throws_WhenQueryMissing()
    {
        Assert.Throws<ArgumentException>(
            () => new SavedFilter(1, 42, SavedFilterSection.Shows, "Favorites", "/users/me/lists/favorites", string.Empty, DateTimeOffset.UtcNow));
    }
}