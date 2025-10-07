using System.Threading;
using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Application.Services;
using DevFund.TraktManager.Domain.Entities;
using DevFund.TraktManager.Domain.ValueObjects;

namespace DevFund.TraktManager.Application.Tests;

public class ListsServiceTests
{
    [Fact]
    public async Task GetListsAsync_ForSavedKindReturnsSavedFilters()
    {
        var request = new ListsRequest(ListCollectionKind.Saved, savedFilterSection: SavedFilterSection.Shows);
        var expectedFilters = new[]
        {
            new SavedFilter(1, 42, SavedFilterSection.Shows, "My Filter", "/filters/shows", "genres=sci-fi", DateTimeOffset.UtcNow)
        };
        var pagination = new PaginationMetadata(1, 10, 1, 1);
        var client = FakeListsClient.ForSavedFilters(expectedFilters, pagination);
        var service = new ListsService(client);

        var response = await service.GetListsAsync(request, CancellationToken.None);

        Assert.Equal(expectedFilters, response.SavedFilters);
        Assert.Empty(response.Lists);
        Assert.Empty(response.ListItems);
        Assert.Equal(pagination, response.Pagination);
        Assert.Equal(request, client.LastSavedFiltersRequest);
    }

    [Fact]
    public async Task GetListsAsync_WhenListSlugMissingFetchesDetailsAndItems()
    {
        var request = new ListsRequest(ListCollectionKind.Personal, userSlug: "me", listSlug: "target-list", includeItems: false);
        var list = CreateUserList("target-list");
        var item = new ListItem(
            1,
            100,
            DateTimeOffset.UtcNow,
            ListItemType.Movie,
            movie: new Movie("Example", 2020, new TraktIds(10, "example")));
        var client = FakeListsClient.ForMissingList(list, new[] { item });
        var service = new ListsService(client);

        var response = await service.GetListsAsync(request, CancellationToken.None);

        Assert.Single(response.Lists);
        Assert.Equal("target-list", response.Lists[0].List.Ids.Slug);
        Assert.Single(response.ListItems);
        Assert.Equal("target-list", response.ListItems[0].List.List.Ids.Slug);
        Assert.Contains(item, response.ListItems[0].Items);
    Assert.True(client.GetListDetailsCalled);
    Assert.Equal(list.Owner?.Slug, client.LastItemsRequest?.UserSlug);
        Assert.Equal("target-list", client.LastItemsRequest?.ListSlug);
    }

    [Fact]
    public async Task ListsOrchestrator_PresentsResponseFromService()
    {
        var request = new ListsRequest(ListCollectionKind.Liked);
        var list = CreateUserList("liked-list");
        var response = new ListsResponse(new[] { new ListCollectionItem(list, "liked") }, Array.Empty<ListItemsGroup>(), Array.Empty<SavedFilter>(), pagination: null);
        var client = FakeListsClient.ForFixedResponse(response);
        var service = new ListsService(client);
        var presenter = new RecordingListsPresenter();
        var orchestrator = new ListsOrchestrator(service, new[] { presenter });

        await orchestrator.ExecuteAsync(request, CancellationToken.None);

        Assert.True(presenter.Invoked);
        Assert.Equal(response.Lists, presenter.Response?.Lists);
    }

    private static UserList CreateUserList(string slug)
    {
        return new UserList(
            name: "My List",
            description: "Description",
            privacy: ListPrivacy.Public,
            shareLink: null,
            type: "official",
            displayNumbers: true,
            allowComments: true,
            sortBy: "rank",
            sortOrder: ListSortOrder.Asc,
            createdAt: DateTimeOffset.UtcNow.AddDays(-10),
            updatedAt: DateTimeOffset.UtcNow.AddDays(-1),
            itemCount: 1,
            commentCount: 0,
            likes: 5,
            ids: new ListIds(1, slug),
            owner: new ListUser("owner", false, "Owner", false, false, "owner"));
    }

    private sealed class RecordingListsPresenter : IListsPresenter
    {
        public bool Invoked { get; private set; }

        public ListsResponse? Response { get; private set; }

        public Task PresentAsync(ListsResponse response, CancellationToken cancellationToken = default)
        {
            Invoked = true;
            Response = response;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeListsClient : ITraktListsClient
    {
        private readonly ListsResponse? _fixedResponse;
        private readonly SavedFiltersResult? _savedFiltersResult;
        private readonly ListCollectionResult? _listResult;
        private readonly IReadOnlyList<ListItem>? _listItems;
        private readonly UserList? _listDetails;

        private FakeListsClient(
            ListsResponse? fixedResponse,
            SavedFiltersResult? savedFiltersResult,
            ListCollectionResult? listResult,
            IReadOnlyList<ListItem>? listItems,
            UserList? listDetails)
        {
            _fixedResponse = fixedResponse;
            _savedFiltersResult = savedFiltersResult;
            _listResult = listResult;
            _listItems = listItems;
            _listDetails = listDetails;
        }

        public ListsRequest? LastListsRequest { get; private set; }

        public ListsRequest? LastSavedFiltersRequest { get; private set; }

        public ListItemsRequest? LastItemsRequest { get; private set; }

        public bool GetListDetailsCalled { get; private set; }

        public static FakeListsClient ForSavedFilters(IReadOnlyList<SavedFilter> filters, PaginationMetadata pagination)
        {
            return new FakeListsClient(
                fixedResponse: null,
                savedFiltersResult: new SavedFiltersResult(filters, pagination),
                listResult: null,
                listItems: null,
                listDetails: null);
        }

        public static FakeListsClient ForMissingList(UserList listDetails, IReadOnlyList<ListItem> listItems)
        {
            var listResult = new ListCollectionResult(Array.Empty<ListCollectionItem>(), null);
            return new FakeListsClient(
                fixedResponse: null,
                savedFiltersResult: null,
                listResult: listResult,
                listItems: listItems,
                listDetails: listDetails);
        }

        public static FakeListsClient ForFixedResponse(ListsResponse response)
        {
            return new FakeListsClient(
                fixedResponse: response,
                savedFiltersResult: null,
                listResult: new ListCollectionResult(response.Lists, response.Pagination),
                listItems: Array.Empty<ListItem>(),
                listDetails: null);
        }

        public Task<ListCollectionResult> GetListsAsync(ListsRequest request, CancellationToken cancellationToken = default)
        {
            LastListsRequest = request;

            if (_fixedResponse is not null)
            {
                return Task.FromResult(new ListCollectionResult(_fixedResponse.Lists, _fixedResponse.Pagination));
            }

            if (_listResult is not null)
            {
                return Task.FromResult(_listResult);
            }

            return Task.FromResult(new ListCollectionResult(Array.Empty<ListCollectionItem>(), null));
        }

        public Task<SavedFiltersResult> GetSavedFiltersAsync(ListsRequest request, CancellationToken cancellationToken = default)
        {
            LastSavedFiltersRequest = request;

            if (_savedFiltersResult is null)
            {
                return Task.FromResult(new SavedFiltersResult(Array.Empty<SavedFilter>(), null));
            }

            return Task.FromResult(_savedFiltersResult);
        }

        public Task<IReadOnlyList<ListItem>> GetListItemsAsync(ListItemsRequest request, CancellationToken cancellationToken = default)
        {
            LastItemsRequest = request;
            return Task.FromResult(_listItems ?? Array.Empty<ListItem>());
        }

        public Task<UserList?> GetListDetailsAsync(string userSlug, string listSlug, CancellationToken cancellationToken = default)
        {
            GetListDetailsCalled = true;
            return Task.FromResult(_listDetails);
        }
    }
}
