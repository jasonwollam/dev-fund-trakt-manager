using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Domain.Entities;
using DevFund.TraktManager.Domain.ValueObjects;
using DevFund.TraktManager.Presentation.Cli.Presentation;
using Spectre.Console.Testing;

namespace DevFund.TraktManager.Presentation.Cli.Tests;

public class ConsoleListsPresenterTests
{
    [Fact]
    public async Task PresentAsync_WritesFallbackMessage_WhenNoData()
    {
        var console = new TestConsole();
        var presenter = new ConsoleListsPresenter(console);
        var response = new ListsResponse(Array.Empty<ListCollectionItem>(), Array.Empty<ListItemsGroup>(), Array.Empty<SavedFilter>(), pagination: null);

        await presenter.PresentAsync(response);

        Assert.Contains("No list data returned", console.Output);
    }

    [Fact]
    public async Task PresentAsync_RendersFiltersListsAndItems()
    {
        var console = new TestConsole();
        var presenter = new ConsoleListsPresenter(console);
        var list = CreateUserList("space-picks");
        var collectionItem = new ListCollectionItem(list, "Liked", DateTimeOffset.UtcNow);
        var movie = new Movie("Inception", 2010, new TraktIds(1, "inception"));
        var listItem = new ListItem(1, 10, DateTimeOffset.UtcNow, ListItemType.Movie, movie: movie);
        var filter = new SavedFilter(1, 100, SavedFilterSection.Movies, "Favorites", "/filters/movies", "genres=scifi", DateTimeOffset.UtcNow);
        var pagination = new PaginationMetadata(1, 10, 1, 10);
        var response = new ListsResponse(new[] { collectionItem }, new[] { new ListItemsGroup(collectionItem, new[] { listItem }) }, new[] { filter }, pagination);

        await presenter.PresentAsync(response);

        Assert.Contains("Saved Filters", console.Output);
        Assert.Contains("Lists", console.Output);
        Assert.Contains("Inception", console.Output);
        Assert.Contains("Space Picks", console.Output);
        Assert.Contains("Page 1 of 1", console.Output);
    }

    private static UserList CreateUserList(string slug)
    {
        return new UserList(
            name: "Space Picks",
            description: "Best sci-fi movies",
            privacy: ListPrivacy.Public,
            shareLink: null,
            type: "official",
            displayNumbers: true,
            allowComments: true,
            sortBy: "rank",
            sortOrder: ListSortOrder.Asc,
            createdAt: DateTimeOffset.UtcNow.AddDays(-7),
            updatedAt: DateTimeOffset.UtcNow,
            itemCount: 1,
            commentCount: 0,
            likes: 10,
            ids: new ListIds(1, slug),
            owner: new ListUser("owner", false, "Owner", false, false, "owner"));
    }
}
