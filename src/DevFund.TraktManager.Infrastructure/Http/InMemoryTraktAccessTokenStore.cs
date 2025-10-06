using System.Threading;
using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace DevFund.TraktManager.Infrastructure.Http;

internal sealed class InMemoryTraktAccessTokenStore : ITraktAccessTokenStore
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DeviceTokenResponse? _token;

    public InMemoryTraktAccessTokenStore(IOptions<TraktOptions> options)
    {
        var value = options.Value;
        if (!string.IsNullOrWhiteSpace(value.AccessToken))
        {
            var expiresIn = TimeSpan.Zero;
            _token = new DeviceTokenResponse(
                value.AccessToken!,
                TokenType: "bearer",
                ExpiresIn: expiresIn,
                RefreshToken: value.RefreshToken ?? string.Empty,
                Scope: null);
        }
    }

    public async ValueTask<DeviceTokenResponse?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _token;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask SetTokenAsync(DeviceTokenResponse token, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _token = token;
        }
        finally
        {
            _gate.Release();
        }
    }
}
