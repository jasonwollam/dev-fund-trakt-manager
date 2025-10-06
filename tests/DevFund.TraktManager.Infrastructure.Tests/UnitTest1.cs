using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using DevFund.TraktManager.Infrastructure.Http;
using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Application.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace DevFund.TraktManager.Infrastructure.Tests;

public class TraktCalendarClientTests
{
    [Fact]
    public async Task GetMyShowsAsync_MapsPayloadIntoDomain()
    {
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new[]
            {
                new
                {
                    first_aired = "2024-01-01T00:00:00.000Z",
                    episode = new
                    {
                        season = 1,
                        number = 1,
                        title = "Pilot",
                        ids = new { trakt = 2, slug = "pilot", imdb = "tt123", tmdb = 4 }
                    },
                    show = new
                    {
                        title = "Test",
                        year = 2024,
                        ids = new { trakt = 1, slug = "test", imdb = (string?)null, tmdb = 5 }
                    }
                }
            })
        });

    using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };
    var tokenStore = new StubTokenStore("token-value");
    var sut = new TraktCalendarClient(client, tokenStore, NullLogger<TraktCalendarClient>.Instance);

        var entries = await sut.GetMyShowsAsync(new DateOnly(2024, 01, 01), 7);

        Assert.Single(entries);
        Assert.Equal("Test", entries[0].Show.Title);
    }

    [Fact]
    public async Task GetMyShowsAsync_Throws_WhenUnauthorized()
    {
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };
        var tokenStore = new StubTokenStore("token-value");
        var sut = new TraktCalendarClient(client, tokenStore, NullLogger<TraktCalendarClient>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetMyShowsAsync(new DateOnly(2024, 01, 01), 7));
    }

    [Fact]
    public async Task GetMyShowsAsync_Throws_WhenTokenMissing()
    {
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };
        var tokenStore = new StubTokenStore(null);
        var sut = new TraktCalendarClient(client, tokenStore, NullLogger<TraktCalendarClient>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetMyShowsAsync(new DateOnly(2024, 01, 01), 7));
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public StubHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }

    private sealed class StubTokenStore : ITraktAccessTokenStore
    {
        private readonly string? _token;

        public StubTokenStore(string? token)
        {
            _token = token;
        }

        public ValueTask<DeviceTokenResponse?> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_token))
            {
                return ValueTask.FromResult<DeviceTokenResponse?>(null);
            }

            var deviceToken = new DeviceTokenResponse(_token!, "Bearer", TimeSpan.Zero, string.Empty, null);
            return ValueTask.FromResult<DeviceTokenResponse?>(deviceToken);
        }

        public ValueTask SetTokenAsync(DeviceTokenResponse token, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}