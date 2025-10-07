using System.Net;
using System.Net.Http;
using System.Text;
using System.Linq;
using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Domain.Entities;
using DevFund.TraktManager.Domain.ValueObjects;
using DevFund.TraktManager.Infrastructure.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace DevFund.TraktManager.Infrastructure.Tests;

public class TraktWatchlistClientTests
{
    [Fact]
    public async Task GetWatchlistAsync_MapsMovieEntries()
    {
        var response = CreateResponse(HttpStatusCode.OK, GetMovieWatchlistPayload());
        using var httpClient = CreateHttpClient(response, out _);
        var tokenStore = new StubTokenStore();
        var sut = CreateSut(httpClient, tokenStore);

        var result = await sut.GetWatchlistAsync(new WatchlistRequest());

        var entry = Assert.Single(result);
        Assert.Equal(1, entry.Rank);
        Assert.Equal(101, entry.ListItemId);
        Assert.Equal("TRON: Legacy (2010)", entry.Movie?.ToString());
    }

    [Fact]
    public async Task GetWatchlistAsync_MapsShowSeasonAndEpisodeEntries()
    {
        var response = CreateResponse(HttpStatusCode.OK, GetMixedWatchlistPayload());
        using var httpClient = CreateHttpClient(response, out _);
        var tokenStore = new StubTokenStore();
        var sut = CreateSut(httpClient, tokenStore);

        var result = await sut.GetWatchlistAsync(new WatchlistRequest(WatchlistItemFilter.All, WatchlistSortField.Added, WatchlistSortOrder.Desc));

        Assert.Equal(3, result.Count);

        var showEntry = Assert.Single(result.Where(e => e.ItemType == WatchlistItemType.Show));
        Assert.Equal("Better Call Saul (2015)", showEntry.Show?.ToString());

        var seasonEntry = Assert.Single(result.Where(e => e.ItemType == WatchlistItemType.Season));
        Assert.Equal("Better Call Saul (2015)", seasonEntry.Show?.ToString());
        Assert.Equal(2, seasonEntry.Season?.Number);

        var episodeEntry = Assert.Single(result.Where(e => e.ItemType == WatchlistItemType.Episode));
        Assert.Equal("Better Call Saul (2015)", episodeEntry.Show?.ToString());
        Assert.Equal("S01E03 · Nacho", episodeEntry.Episode?.ToString());
    }

    [Fact]
    public async Task GetWatchlistAsync_UsesRequestParametersInUri()
    {
        var response = CreateResponse(HttpStatusCode.OK, GetMovieWatchlistPayload());
        using var httpClient = CreateHttpClient(response, out var handler);
        var tokenStore = new StubTokenStore();
        var sut = CreateSut(httpClient, tokenStore);

        await sut.GetWatchlistAsync(new WatchlistRequest(WatchlistItemFilter.Shows, WatchlistSortField.Added, WatchlistSortOrder.Desc));

        Assert.NotNull(handler.LastRequestUri);
        Assert.Equal("https://api.trakt.tv/sync/watchlist/shows/added/desc", handler.LastRequestUri!.ToString());
    }

    [Fact]
    public async Task GetWatchlistAsync_Throws_WhenUnauthorized()
    {
        var response = CreateResponse(HttpStatusCode.Unauthorized, string.Empty);
        using var httpClient = CreateHttpClient(response, out _);
        var tokenStore = new StubTokenStore();
        var sut = CreateSut(httpClient, tokenStore);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetWatchlistAsync(new WatchlistRequest()));
    }

    private static HttpClient CreateHttpClient(HttpResponseMessage response, out SingleResponseHandler handler)
    {
        handler = new SingleResponseHandler(response);
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.trakt.tv/", UriKind.Absolute)
        };
    }

    private static TraktWatchlistClient CreateSut(HttpClient httpClient, ITraktAccessTokenStore tokenStore)
    {
        return new TraktWatchlistClient(httpClient, tokenStore, NullLogger<TraktWatchlistClient>.Instance);
    }

    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string content)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };
    }

    private static string GetMovieWatchlistPayload() =>
        """
        [
            {
                "rank": 1,
                "id": 101,
                "listed_at": "2014-09-01T09:10:11.000Z",
                "notes": "Need to catch up before TRON 3 is out.",
                "type": "movie",
                "movie": {
                    "title": "TRON: Legacy",
                    "year": 2010,
                    "ids": {
                        "trakt": 1,
                        "slug": "tron-legacy-2010",
                        "imdb": "tt1104001",
                        "tmdb": 20526
                    }
                }
            }
        ]
        """;

    private static string GetMixedWatchlistPayload() =>
        """
        [
            {
                "rank": 1,
                "id": 120,
                "listed_at": "2015-01-01T00:00:00.000Z",
                "type": "show",
                "show": {
                    "title": "Better Call Saul",
                    "year": 2015,
                    "ids": {
                        "trakt": 12,
                        "slug": "better-call-saul",
                        "imdb": "tt3032476",
                        "tmdb": 60059
                    }
                }
            },
            {
                "rank": 2,
                "id": 121,
                "listed_at": "2015-01-02T00:00:00.000Z",
                "type": "season",
                "show": {
                    "title": "Better Call Saul",
                    "year": 2015,
                    "ids": {
                        "trakt": 12,
                        "slug": "better-call-saul",
                        "imdb": "tt3032476",
                        "tmdb": 60059
                    }
                },
                "season": {
                    "number": 2,
                    "ids": {
                        "trakt": 302,
                        "tvdb": 575074,
                        "tmdb": 64114
                    }
                }
            },
            {
                "rank": 3,
                "id": 122,
                "listed_at": "2015-01-03T00:00:00.000Z",
                "type": "episode",
                "show": {
                    "title": "Better Call Saul",
                    "year": 2015,
                    "ids": {
                        "trakt": 12,
                        "slug": "better-call-saul",
                        "imdb": "tt3032476",
                        "tmdb": 60059
                    }
                },
                "episode": {
                    "season": 1,
                    "number": 3,
                    "title": "Nacho",
                    "ids": {
                        "trakt": 501,
                        "tvdb": 5096921,
                        "imdb": "tt3032476",
                        "tmdb": 103168
                    }
                }
            }
        ]
        """;

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

    private sealed class SingleResponseHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public SingleResponseHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return Task.FromResult(_response);
        }
    }
}
