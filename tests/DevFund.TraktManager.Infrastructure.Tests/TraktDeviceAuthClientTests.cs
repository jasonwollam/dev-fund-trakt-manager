using System.Net;
using System.Net.Http;
using System.Text;
using DevFund.TraktManager.Infrastructure.Http;
using DevFund.TraktManager.Infrastructure.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DevFund.TraktManager.Infrastructure.Tests;

public class TraktDeviceAuthClientTests
{
    [Fact]
    public async Task PollForDeviceTokenAsync_ReturnsAuthorizationPending_WhenJsonError()
    {
        using var httpClient = CreateHttpClient(CreateResponse(HttpStatusCode.BadRequest, "{\"error\":\"authorization_pending\"}", "application/json"));
        var sut = CreateSut(httpClient);

        var result = await sut.PollForDeviceTokenAsync("device");

        Assert.False(result.IsAuthorized);
        Assert.Equal("authorization_pending", result.Error);
    }

    [Fact]
    public async Task PollForDeviceTokenAsync_DefaultsToAuthorizationPending_WhenNoContent()
    {
        using var httpClient = CreateHttpClient(CreateResponse(HttpStatusCode.BadRequest, string.Empty, "text/plain"));
        var sut = CreateSut(httpClient);

        var result = await sut.PollForDeviceTokenAsync("device");

        Assert.False(result.IsAuthorized);
        Assert.Equal("authorization_pending", result.Error);
    }

    [Fact]
    public async Task PollForDeviceTokenAsync_UsesPlainTextError_WhenProvided()
    {
        using var httpClient = CreateHttpClient(CreateResponse(HttpStatusCode.BadRequest, "invalid_client", "text/plain"));
        var sut = CreateSut(httpClient);

        var result = await sut.PollForDeviceTokenAsync("device");

        Assert.False(result.IsAuthorized);
        Assert.Equal("invalid_client", result.Error);
    }

    [Fact]
    public async Task PollForDeviceTokenAsync_ReturnsSlowDown_ForTooManyRequests()
    {
        using var httpClient = CreateHttpClient(CreateResponse(HttpStatusCode.TooManyRequests, string.Empty, "text/plain"));
        var sut = CreateSut(httpClient);

        var result = await sut.PollForDeviceTokenAsync("device");

        Assert.False(result.IsAuthorized);
        Assert.Equal("slow_down", result.Error);
    }

    private static HttpClient CreateHttpClient(HttpResponseMessage response)
    {
        var handler = new SingleResponseHandler(response);
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.trakt.tv/", UriKind.Absolute)
        };
    }

    private static TraktDeviceAuthClient CreateSut(HttpClient httpClient)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new TraktOptions
        {
            ClientId = "client-id",
            ClientSecret = "client-secret"
        });

        return new TraktDeviceAuthClient(httpClient, options, NullLogger<TraktDeviceAuthClient>.Instance);
    }

    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string content, string mediaType)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, mediaType)
        };
    }

    private sealed class SingleResponseHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public SingleResponseHandler(HttpResponseMessage response)
        {
            _response = response ?? throw new ArgumentNullException(nameof(response));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }
}
