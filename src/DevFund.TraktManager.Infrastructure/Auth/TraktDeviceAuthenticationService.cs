using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Application.Contracts;

namespace DevFund.TraktManager.Infrastructure.Auth;

public sealed class TraktDeviceAuthenticationService : IDeviceAuthenticationService
{
    private static readonly TimeSpan SlowDownIncrement = TimeSpan.FromSeconds(5);

    private readonly ITraktDeviceAuthClient _deviceClient;
    private readonly ITraktAccessTokenStore _tokenStore;

    public TraktDeviceAuthenticationService(ITraktDeviceAuthClient deviceClient, ITraktAccessTokenStore tokenStore)
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

            var normalizedError = result.Error.Trim().ToLowerInvariant();

            switch (normalizedError)
            {
                case "authorization_pending":
                    break;
                case "slow_down":
                    interval += SlowDownIncrement;
                    break;
                case "access_denied":
                    throw new InvalidOperationException("The user declined the Trakt authorization request.");
                case "expired_token":
                    throw new TimeoutException("The device code expired before authorization was granted.");
                case "invalid_client":
                    throw new InvalidOperationException("The configured Trakt client credentials were rejected. Double-check your ClientId and ClientSecret.");
                case "invalid_grant":
                    throw new InvalidOperationException("The device code is no longer valid. Request a new code and try again.");
                default:
                    throw new InvalidOperationException($"Unexpected device authentication error '{result.Error}'.");
            }
        }
    }
}
