using System.Collections.Generic;
using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Application.Services;

namespace DevFund.TraktManager.Application.Tests;

public class DeviceAuthenticationServiceTests
{
    [Fact]
    public async Task RequestDeviceCodeAsync_ForwardsToClient()
    {
        var expected = new DeviceCodeResponse("device", "user", new Uri("https://example.com"), TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(1));
        var client = new StubDeviceAuthClient(expected, Array.Empty<DeviceTokenPollResult>());
        var store = new RecordingTokenStore();
        var service = new DeviceAuthenticationService(client, store);

        var result = await service.RequestDeviceCodeAsync();

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task WaitForAuthorizationAsync_PollsUntilAuthorized()
    {
        var deviceCode = new DeviceCodeResponse("device", "user", new Uri("https://example.com"), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(10));
        var token = new DeviceTokenResponse("access", "bearer", TimeSpan.FromSeconds(3600), "refresh", null);
        var polls = new List<DeviceTokenPollResult>
        {
            new(null, "authorization_pending"),
            new(token, null)
        };

        var client = new StubDeviceAuthClient(deviceCode, polls);
        var store = new RecordingTokenStore();
        var service = new DeviceAuthenticationService(client, store);

        var result = await service.WaitForAuthorizationAsync(deviceCode);

        Assert.Equal(token, result);
        Assert.Equal(token, store.SavedToken);
    }

    [Fact]
    public async Task WaitForAuthorizationAsync_ThrowsWhenAccessDenied()
    {
        var deviceCode = new DeviceCodeResponse("device", "user", new Uri("https://example.com"), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(10));
        var polls = new List<DeviceTokenPollResult>
        {
            new(null, "access_denied")
        };

        var client = new StubDeviceAuthClient(deviceCode, polls);
        var store = new RecordingTokenStore();
        var service = new DeviceAuthenticationService(client, store);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.WaitForAuthorizationAsync(deviceCode));
    }

    private sealed class StubDeviceAuthClient : ITraktDeviceAuthClient
    {
        private readonly DeviceCodeResponse _codeResponse;
        private readonly Queue<DeviceTokenPollResult> _polls;

        public StubDeviceAuthClient(DeviceCodeResponse codeResponse, IEnumerable<DeviceTokenPollResult> polls)
        {
            _codeResponse = codeResponse;
            _polls = new Queue<DeviceTokenPollResult>(polls);
        }

        public Task<DeviceCodeResponse> CreateDeviceCodeAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_codeResponse);
        }

        public Task<DeviceTokenPollResult> PollForDeviceTokenAsync(string deviceCode, CancellationToken cancellationToken = default)
        {
            if (_polls.Count == 0)
            {
                return Task.FromResult(new DeviceTokenPollResult(null, "authorization_pending"));
            }

            return Task.FromResult(_polls.Dequeue());
        }
    }

    private sealed class RecordingTokenStore : ITraktAccessTokenStore
    {
        public DeviceTokenResponse? SavedToken { get; private set; }

        public ValueTask<DeviceTokenResponse?> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<DeviceTokenResponse?>(SavedToken);
        }

        public ValueTask SetTokenAsync(DeviceTokenResponse token, CancellationToken cancellationToken = default)
        {
            SavedToken = token;
            return ValueTask.CompletedTask;
        }
    }
}
