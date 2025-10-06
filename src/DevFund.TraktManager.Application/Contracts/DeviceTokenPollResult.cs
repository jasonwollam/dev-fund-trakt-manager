namespace DevFund.TraktManager.Application.Contracts;

public sealed record DeviceTokenPollResult(DeviceTokenResponse? Token, string? Error)
{
    public bool IsAuthorized => Token is not null;
}
