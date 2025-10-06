using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Application.Contracts;

namespace DevFund.TraktManager.Application.Services;

public sealed class DeviceAuthenticationService
{
    private static readonly TimeSpan SlowDownIncrement = TimeSpan.FromSeconds(5);

    private readonly ITraktDeviceAuthClient _deviceClient;
    private readonly ITraktAccessTokenStore _tokenStore;

    public DeviceAuthenticationService(ITraktDeviceAuthClient deviceClient, ITraktAccessTokenStore tokenStore)
    {
        _deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
    }

    public Task<DeviceCodeResponse> RequestDeviceCodeAsync(CancellationToken cancellationToken = default)
    {
        return _deviceClient.CreateDeviceCodeAsync(cancellationToken);
    }

    public async Task<DeviceTokenResponse> WaitForAuthorizationAsync(DeviceCodeResponse deviceCode, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(deviceCode);

        var interval = deviceCode.Interval > TimeSpan.Zero ? deviceCode.Interval : TimeSpan.FromSeconds(5);
        var expiresAt = DateTimeOffset.UtcNow.Add(deviceCode.ExpiresIn > TimeSpan.Zero ? deviceCode.ExpiresIn : TimeSpan.FromMinutes(5));

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (DateTimeOffset.UtcNow >= expiresAt)
            {
                throw new TimeoutException("Device code expired before authorization was granted.");
            }

            await Task.Delay(interval, cancellationToken).ConfigureAwait(false);

            var result = await _deviceClient.PollForDeviceTokenAsync(deviceCode.DeviceCode, cancellationToken).ConfigureAwait(false);
            if (result.IsAuthorized && result.Token is not null)
            {
                await _tokenStore.SetTokenAsync(result.Token, cancellationToken).ConfigureAwait(false);
                return result.Token;
            }

            if (string.IsNullOrWhiteSpace(result.Error))
            {
                continue;
            }

            switch (result.Error)
            {
                case "authorization_pending":
                    // continue polling at the same interval
                    break;
                case "slow_down":
                    interval += SlowDownIncrement;
                    break;
                case "access_denied":
                    throw new InvalidOperationException("The user declined the Trakt authorization request.");
                case "expired_token":
                    throw new TimeoutException("The device code expired before authorization was granted.");
                default:
                    throw new InvalidOperationException($"Unexpected device authentication error '{result.Error}'.");
            }
        }
    }
}
