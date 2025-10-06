using System.Text.Json.Serialization;

namespace DevFund.TraktManager.Infrastructure.Dtos;

internal sealed record DeviceCodeResponseDto(
    [property: JsonPropertyName("device_code")] string DeviceCode,
    [property: JsonPropertyName("user_code")] string UserCode,
    [property: JsonPropertyName("verification_url")] string VerificationUrl,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("interval")] int Interval);

internal sealed record DeviceTokenResponseDto(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("scope")] string? Scope);

internal sealed record DeviceTokenErrorDto(
    [property: JsonPropertyName("error")] string Error);
