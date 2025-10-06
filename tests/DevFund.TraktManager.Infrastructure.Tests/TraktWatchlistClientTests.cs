using System.Net;
using System.Net.Http;
using System.Text;
using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Domain.Entities;
using DevFund.TraktManager.Infrastructure.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace DevFund.TraktManager.Infrastructure.Tests;

public class TraktWatchlistClientTests
{
    [Fact]
    public async Task GetWatchlistAsync_MapsMovieEntries()
    {
        var response = CreateResponse(HttpStatusCode.OK, GetMovieWatchlistPayload());
        using var httpClient = CreateHttpClient(response);
        var tokenStore = new StubTokenStore();
        var sut = CreateSut(httpClient, tokenStore);

        var result = await sut.GetWatchlistAsync(new WatchlistRequest());

        var entry = Assert.Single(result);
        Assert.Equal(1, entry.Rank);
        Assert.Equal(101, entry.ListItemId);
        Assert.Equal("TRON: Legacy (2010)", entry.Movie?.ToString());
    }

    [Fact]
    public async Task GetWatchlistAsync_Throws_WhenUnauthorized()
    {
        var response = CreateResponse(HttpStatusCode.Unauthorized, string.Empty);
        using var httpClient = CreateHttpClient(response);
        var tokenStore = new StubTokenStore();
        var sut = CreateSut(httpClient, tokenStore);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetWatchlistAsync(new WatchlistRequest()));
    }

    private static HttpClient CreateHttpClient(HttpResponseMessage response)
    {
        var handler = new SingleResponseHandler(response);
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

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }
}
