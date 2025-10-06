using DevFund.TraktManager.Application.Contracts;

namespace DevFund.TraktManager.Application.Abstractions;

public interface ITraktAccessTokenStore
{
    ValueTask<DeviceTokenResponse?> GetTokenAsync(CancellationToken cancellationToken = default);

    ValueTask SetTokenAsync(DeviceTokenResponse token, CancellationToken cancellationToken = default);
}
