using DevFund.TraktManager.Application.Contracts;

namespace DevFund.TraktManager.Application.Abstractions;

public interface IDeviceAuthenticationService
{
    Task<DeviceCodeResponse> RequestDeviceCodeAsync(CancellationToken cancellationToken = default);

    Task<DeviceTokenResponse> WaitForAuthorizationAsync(DeviceCodeResponse deviceCode, CancellationToken cancellationToken = default);
}
