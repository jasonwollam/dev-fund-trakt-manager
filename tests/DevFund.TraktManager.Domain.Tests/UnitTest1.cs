using DevFund.TraktManager.Domain.Entities;
using DevFund.TraktManager.Domain.ValueObjects;

namespace DevFund.TraktManager.Domain.Tests;

public class CalendarEntryTests
{
    [Fact]
    public void OccursOn_ReturnsTrue_WhenDatesMatch()
    {
        var ids = new TraktIds(1, "slug");
        var entry = new CalendarEntry(new DateOnly(2024, 01, 01), new Show("Title", 2024, ids), new Episode(1, 1, "Pilot", ids));

    Assert.True(entry.OccursOn(new DateOnly(2024, 01, 01)));
    }

    [Fact]
    public void Constructor_Throws_WhenDateIsDefault()
    {
        var ids = new TraktIds(1, "slug");

        Action act = () => new CalendarEntry(default, new Show("Title", 2024, ids), new Episode(1, 1, "Pilot", ids));

    Assert.Throws<ArgumentException>(act);
    }
}

public class TraktIdsTests
{
    [Fact]
    public void Constructor_Throws_WhenTraktIdIsZero()
    {
        Action act = () => new TraktIds(0, "slug");
    Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void HasImdb_ReturnsTrue_WhenImdbProvided()
    {
        var ids = new TraktIds(1, "slug", imdb: "tt1234567");
    Assert.True(ids.HasImdb);
    }
}