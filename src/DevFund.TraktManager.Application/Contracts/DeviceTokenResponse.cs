namespace DevFund.TraktManager.Application.Contracts;

public sealed record DeviceTokenResponse(
    string AccessToken,
    string TokenType,
    TimeSpan ExpiresIn,
    string RefreshToken,
    string? Scope);
