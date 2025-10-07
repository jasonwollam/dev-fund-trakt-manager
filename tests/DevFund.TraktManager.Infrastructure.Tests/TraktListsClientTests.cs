using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Domain.Entities;
using DevFund.TraktManager.Domain.ValueObjects;
using DevFund.TraktManager.Infrastructure.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace DevFund.TraktManager.Infrastructure.Tests;

public class TraktListsClientTests
{
    [Fact]
    public async Task GetListsAsync_MapsPersonalListsAndPagination()
    {
        var response = CreateResponse(HttpStatusCode.OK, GetPersonalListsPayload(), includePagination: true);
        using var httpClient = CreateHttpClient(response, out var handler);
        var tokenStore = new StubTokenStore();
        var sut = CreateSut(httpClient, tokenStore);

        var result = await sut.GetListsAsync(new ListsRequest(ListCollectionKind.Personal, userSlug: "me"), CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("https://api.trakt.tv/users/me/lists", handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal("token", handler.LastRequest.Headers.Authorization?.Parameter);

        var list = Assert.Single(result.Lists);
        Assert.Equal("Personal", list.Origin);
        Assert.Null(list.OriginTimestamp);
        Assert.Equal("Sci-Fi Gems", list.List.Name);
        Assert.Equal(ListPrivacy.Public, list.List.Privacy);
        Assert.Equal("alice", list.List.Owner?.Slug);

        Assert.NotNull(result.Pagination);
        Assert.Equal(1, result.Pagination!.Page);
        Assert.Equal(10, result.Pagination.Limit);
        Assert.Equal(2, result.Pagination.PageCount);
        Assert.Equal(20, result.Pagination.ItemCount);
    }

    [Fact]
    public async Task GetListsAsync_MapsLikedListsWithOriginTimestamp()
    {
        var response = CreateResponse(HttpStatusCode.OK, GetLikedListsPayload(), includePagination: false);
        using var httpClient = CreateHttpClient(response, out var handler);
        var tokenStore = new StubTokenStore();
        var sut = CreateSut(httpClient, tokenStore);

        var result = await sut.GetListsAsync(new ListsRequest(ListCollectionKind.Liked, userSlug: "me"), CancellationToken.None);

        var list = Assert.Single(result.Lists);
        Assert.Equal("Liked", list.Origin);
    Assert.Equal(DateTimeOffset.Parse("2023-01-03T10:00:00Z", CultureInfo.InvariantCulture), list.OriginTimestamp);
        Assert.Equal("Liked List", list.List.Name);
        Assert.Equal("bob", list.List.Owner?.Slug);

        Assert.Equal("https://api.trakt.tv/users/me/lists/liked", handler.LastRequest?.RequestUri?.ToString());
    }

    [Fact]
    public async Task GetSavedFiltersAsync_MapsFiltersAndPagination()
    {
        var response = CreateResponse(HttpStatusCode.OK, GetSavedFiltersPayload(), includePagination: true);
        using var httpClient = CreateHttpClient(response, out var handler);
        var tokenStore = new StubTokenStore();
        var sut = CreateSut(httpClient, tokenStore);

        var result = await sut.GetSavedFiltersAsync(new ListsRequest(ListCollectionKind.Saved, savedFilterSection: SavedFilterSection.Shows), CancellationToken.None);

        Assert.Equal("https://api.trakt.tv/users/saved_filters/shows", handler.LastRequest?.RequestUri?.ToString());
        Assert.Equal("Bearer", handler.LastRequest?.Headers.Authorization?.Scheme);

        var filter = Assert.Single(result.Filters);
        Assert.Equal("Top Shows", filter.Name);
        Assert.Equal(SavedFilterSection.Shows, filter.Section);
        Assert.Equal("genres=thriller", filter.Query);
        Assert.NotNull(result.Pagination);
    }

    [Fact]
    public async Task GetListItemsAsync_MapsMixedItemTypes()
    {
        var response = CreateResponse(HttpStatusCode.OK, GetListItemsPayload(), includePagination: false);
        using var httpClient = CreateHttpClient(response, out var handler);
        var tokenStore = new StubTokenStore();
        var sut = CreateSut(httpClient, tokenStore);

        var listRequest = new ListItemsRequest("me", "favorites", ListItemsType.All);
        var result = await sut.GetListItemsAsync(listRequest, CancellationToken.None);

        Assert.Equal("https://api.trakt.tv/users/me/lists/favorites/items/all/rank/asc", handler.LastRequest?.RequestUri?.ToString());

        Assert.Equal(5, result.Count);

        var movie = result.First(i => i.ItemType == ListItemType.Movie);
        Assert.Equal("Inception (2010)", movie.Movie?.ToString());

        var show = result.First(i => i.ItemType == ListItemType.Show);
        Assert.Equal("The Expanse (2015)", show.Show?.ToString());

        var season = result.First(i => i.ItemType == ListItemType.Season);
        Assert.Equal(3, season.Season?.Number);

        var episode = result.First(i => i.ItemType == ListItemType.Episode);
        Assert.Equal("S02E05 · Home", episode.Episode?.ToString());

        var person = result.First(i => i.ItemType == ListItemType.Person);
        Assert.Equal("Carla Gugino", person.Person?.ToString());
    }

    private static HttpClient CreateHttpClient(HttpResponseMessage response, out RecordingHandler handler)
    {
        handler = new RecordingHandler(response);
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.trakt.tv/", UriKind.Absolute)
        };
    }

    private static TraktListsClient CreateSut(HttpClient httpClient, ITraktAccessTokenStore tokenStore)
    {
        return new TraktListsClient(httpClient, tokenStore, NullLogger<TraktListsClient>.Instance);
    }

    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string content, bool includePagination)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        if (includePagination)
        {
            response.Headers.Add("X-Pagination-Page", "1");
            response.Headers.Add("X-Pagination-Limit", "10");
            response.Headers.Add("X-Pagination-Page-Count", "2");
            response.Headers.Add("X-Pagination-Item-Count", "20");
        }

        return response;
    }

    private static string GetPersonalListsPayload() =>
        """
        [
            {
                "name": "Sci-Fi Gems",
                "description": "Top picks",
                "privacy": "public",
                "share_link": "https://trakt.tv/users/me/lists/sci-fi-gems",
                "type": "official",
                "display_numbers": true,
                "allow_comments": true,
                "sort_by": "rank",
                "sort_how": "asc",
                "created_at": "2023-01-01T10:00:00.000Z",
                "updated_at": "2023-01-02T10:00:00.000Z",
                "item_count": 5,
                "comment_count": 2,
                "likes": 100,
                "ids": {
                    "trakt": 100,
                    "slug": "sci-fi-gems"
                },
                "user": {
                    "username": "alice",
                    "private": false,
                    "name": "Alice Smith",
                    "vip": true,
                    "vip_ep": false,
                    "ids": {
                        "slug": "alice"
                    }
                }
            }
        ]
        """;

    private static string GetLikedListsPayload() =>
        """
        [
            {
                "liked_at": "2023-01-03T10:00:00.000Z",
                "list": {
                    "name": "Liked List",
                    "description": "A friend's list",
                    "privacy": "public",
                    "share_link": "https://trakt.tv/users/bob/lists/liked-list",
                    "type": "personal",
                    "display_numbers": false,
                    "allow_comments": true,
                    "sort_by": "rank",
                    "sort_how": "desc",
                    "created_at": "2023-01-01T09:00:00.000Z",
                    "updated_at": "2023-01-02T09:00:00.000Z",
                    "item_count": 12,
                    "comment_count": 0,
                    "likes": 50,
                    "ids": {
                        "trakt": 200,
                        "slug": "liked-list"
                    },
                    "user": {
                        "username": "bob",
                        "private": false,
                        "name": "Bob Jones",
                        "vip": false,
                        "vip_ep": false,
                        "ids": {
                            "slug": "bob"
                        }
                    }
                }
            }
        ]
        """;

    private static string GetSavedFiltersPayload() =>
        """
        [
            {
                "rank": 1,
                "id": 10,
                "section": "shows",
                "name": "Top Shows",
                "path": "/filters/shows",
                "query": "genres=thriller",
                "updated_at": "2023-01-05T00:00:00.000Z"
            }
        ]
        """;

    private static string GetListItemsPayload() =>
        """
        [
            {
                "rank": 1,
                "id": 301,
                "listed_at": "2023-01-10T00:00:00.000Z",
                "type": "movie",
                "movie": {
                    "title": "Inception",
                    "year": 2010,
                    "ids": {
                        "trakt": 1,
                        "slug": "inception-2010",
                        "imdb": "tt1375666",
                        "tmdb": 27205
                    }
                }
            },
            {
                "rank": 2,
                "id": 302,
                "listed_at": "2023-01-11T00:00:00.000Z",
                "type": "show",
                "show": {
                    "title": "The Expanse",
                    "year": 2015,
                    "ids": {
                        "trakt": 2,
                        "slug": "the-expanse",
                        "imdb": "tt3230854",
                        "tmdb": 63639
                    }
                }
            },
            {
                "rank": 3,
                "id": 303,
                "listed_at": "2023-01-12T00:00:00.000Z",
                "type": "season",
                "show": {
                    "title": "The Expanse",
                    "year": 2015,
                    "ids": {
                        "trakt": 2,
                        "slug": "the-expanse",
                        "imdb": "tt3230854",
                        "tmdb": 63639
                    }
                },
                "season": {
                    "number": 3,
                    "ids": {
                        "trakt": 30,
                        "tmdb": 70785,
                        "tvdb": 300123
                    }
                }
            },
            {
                "rank": 4,
                "id": 304,
                "listed_at": "2023-01-13T00:00:00.000Z",
                "type": "episode",
                "show": {
                    "title": "The Expanse",
                    "year": 2015,
                    "ids": {
                        "trakt": 2,
                        "slug": "the-expanse",
                        "imdb": "tt3230854",
                        "tmdb": 63639
                    }
                },
                "episode": {
                    "season": 2,
                    "number": 5,
                    "title": "Home",
                    "ids": {
                        "trakt": 400,
                        "imdb": "tt4508382",
                        "tmdb": 1185766
                    }
                }
            },
            {
                "rank": 5,
                "id": 305,
                "listed_at": "2023-01-14T00:00:00.000Z",
                "type": "person",
                "person": {
                    "name": "Carla Gugino",
                    "ids": {
                        "trakt": 500,
                        "slug": "carla-gugino",
                        "imdb": "nm0001301",
                        "tmdb": 19001
                    }
                }
            }
        ]
        """;

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public RecordingHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_response);
        }
    }

    private sealed class StubTokenStore : ITraktAccessTokenStore
    {
        private readonly DeviceTokenResponse _token = new("token", "Bearer", TimeSpan.FromHours(1), "refresh", null);

        public ValueTask<DeviceTokenResponse?> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<DeviceTokenResponse?>(_token);
        }

        public ValueTask SetTokenAsync(DeviceTokenResponse token, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
