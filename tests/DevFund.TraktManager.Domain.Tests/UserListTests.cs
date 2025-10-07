using DevFund.TraktManager.Domain.Entities;
using DevFund.TraktManager.Domain.ValueObjects;

namespace DevFund.TraktManager.Domain.Tests;

public class UserListTests
{
    [Fact]
    public void Constructor_SetsOwnerAndMetadata()
    {
        var owner = new ListUser("alice", false, "Alice Example", true, false, "alice");
        var ids = new ListIds(1, "best-movies");
        var createdAt = DateTimeOffset.UtcNow.AddDays(-5);
        var updatedAt = DateTimeOffset.UtcNow;

        var list = new UserList(
            "Best Movies",
            "Hand-picked favorites",
            ListPrivacy.Public,
            new Uri("https://trakt.tv/users/alice/lists/best-movies"),
            type: "personal",
            displayNumbers: true,
            allowComments: true,
            sortBy: "rank",
            sortOrder: ListSortOrder.Asc,
            createdAt,
            updatedAt,
            itemCount: 10,
            commentCount: 2,
            likes: 5,
            ids,
            owner);

        Assert.Equal("Best Movies", list.Name);
        Assert.Equal("Hand-picked favorites", list.Description);
        Assert.Equal(ListPrivacy.Public, list.Privacy);
        Assert.Equal("rank", list.SortBy);
        Assert.Equal(ListSortOrder.Asc, list.SortOrder);
        Assert.Equal(owner, list.Owner);
        Assert.Equal(ids, list.Ids);
    }

    [Fact]
    public void Constructor_NormalizesEmptyDescription()
    {
        var list = new UserList(
            "Untitled",
            " ",
            ListPrivacy.Public,
            shareLink: null,
            type: "personal",
            displayNumbers: false,
            allowComments: false,
            sortBy: "rank",
            sortOrder: ListSortOrder.Asc,
            createdAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow,
            itemCount: 0,
            commentCount: 0,
            likes: 0,
            ids: new ListIds(2, "untitled"));

        Assert.Null(list.Description);
    }
}