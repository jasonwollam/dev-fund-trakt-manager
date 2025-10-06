namespace DevFund.TraktManager.Application.Contracts;

public sealed record DeviceCodeResponse(
    string DeviceCode,
    string UserCode,
    Uri VerificationUri,
    TimeSpan ExpiresIn,
    TimeSpan Interval);
