using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Domain.ValueObjects;
using DevFund.TraktManager.Infrastructure.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DevFund.TraktManager.Infrastructure.Tests;

public class TraktListsClientNegativeTests
{
    [Fact]
    public async Task GetListsAsync_Throws_WhenUnauthorized()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        using var httpClient = CreateHttpClient(response);
        var tokenStore = new StubTokenStore();
        var sut = CreateSut(httpClient, tokenStore);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.GetListsAsync(new ListsRequest(ListCollectionKind.Personal, userSlug: "me"), CancellationToken.None));
    }

    [Fact]
    public async Task GetListsAsync_Throws_WhenForbidden()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Forbidden);
        using var httpClient = CreateHttpClient(response);
        var tokenStore = new StubTokenStore();
        var sut = CreateSut(httpClient, tokenStore);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.GetListsAsync(new ListsRequest(ListCollectionKind.Personal, userSlug: "me"), CancellationToken.None));
    }

    [Fact]
    public async Task GetSavedFiltersAsync_Throws_WhenUnauthorized()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        using var httpClient = CreateHttpClient(response);
        var tokenStore = new StubTokenStore();
        var sut = CreateSut(httpClient, tokenStore);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.GetSavedFiltersAsync(new ListsRequest(ListCollectionKind.Saved, savedFilterSection: SavedFilterSection.Shows), CancellationToken.None));
    }

    [Fact]
    public async Task GetListItemsAsync_Throws_WhenUnauthorized()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        using var httpClient = CreateHttpClient(response);
        var tokenStore = new StubTokenStore();
        var sut = CreateSut(httpClient, tokenStore);

        var listRequest = new ListItemsRequest("me", "favorites", ListItemsType.All);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.GetListItemsAsync(listRequest, CancellationToken.None));
    }

    [Fact]
    public async Task GetListDetailsAsync_ReturnsNull_WhenNotFound()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        using var httpClient = CreateHttpClient(response);
        var tokenStore = new StubTokenStore();
        var sut = CreateSut(httpClient, tokenStore);

        var result = await sut.GetListDetailsAsync("me", "missing-list", CancellationToken.None);
        Assert.Null(result);
    }

    private static HttpClient CreateHttpClient(HttpResponseMessage response)
    {
        return new HttpClient(new SingleResponseHandler(response))
        {
            BaseAddress = new Uri("https://api.trakt.tv/", UriKind.Absolute)
        };
    }

    private static TraktListsClient CreateSut(HttpClient httpClient, ITraktAccessTokenStore tokenStore)
    {
        return new TraktListsClient(httpClient, tokenStore, NullLogger<TraktListsClient>.Instance);
    }

    private sealed class StubTokenStore : ITraktAccessTokenStore
    {
        private readonly DevFund.TraktManager.Application.Contracts.DeviceTokenResponse _token =
            new("token", "Bearer", TimeSpan.FromHours(1), "refresh", null);

        public ValueTask<DevFund.TraktManager.Application.Contracts.DeviceTokenResponse?> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<DevFund.TraktManager.Application.Contracts.DeviceTokenResponse?>(_token);
        }

        public ValueTask SetTokenAsync(DevFund.TraktManager.Application.Contracts.DeviceTokenResponse token, CancellationToken cancellationToken = default)
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
