using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using DevFund.TraktManager.Infrastructure.Http;
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
        var sut = new TraktCalendarClient(client, NullLogger<TraktCalendarClient>.Instance);

        var entries = await sut.GetMyShowsAsync(new DateOnly(2024, 01, 01), 7);

        Assert.Single(entries);
        Assert.Equal("Test", entries[0].Show.Title);
    }

    [Fact]
    public async Task GetMyShowsAsync_Throws_WhenUnauthorized()
    {
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };
        var sut = new TraktCalendarClient(client, NullLogger<TraktCalendarClient>.Instance);

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
}