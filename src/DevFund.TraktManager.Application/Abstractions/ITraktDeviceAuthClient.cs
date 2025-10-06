using DevFund.TraktManager.Application.Contracts;

namespace DevFund.TraktManager.Application.Abstractions;

public interface ITraktDeviceAuthClient
{
    Task<DeviceCodeResponse> CreateDeviceCodeAsync(CancellationToken cancellationToken = default);

    Task<DeviceTokenPollResult> PollForDeviceTokenAsync(string deviceCode, CancellationToken cancellationToken = default);
}
