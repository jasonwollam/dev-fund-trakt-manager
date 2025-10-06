using System.Net.Http.Json;
using System.Text.Json;
using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Infrastructure.Dtos;
using DevFund.TraktManager.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DevFund.TraktManager.Infrastructure.Http;

internal sealed class TraktDeviceAuthClient : ITraktDeviceAuthClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IOptions<TraktOptions> _options;
    private readonly ILogger<TraktDeviceAuthClient> _logger;

    public TraktDeviceAuthClient(HttpClient httpClient, IOptions<TraktOptions> options, ILogger<TraktDeviceAuthClient>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? NullLogger<TraktDeviceAuthClient>.Instance;
    }

    public async Task<DeviceCodeResponse> CreateDeviceCodeAsync(CancellationToken cancellationToken = default)
    {
        var settings = _options.Value;
        ValidateClientCredentials(settings);

        var payload = new
        {
            client_id = settings.ClientId,
            client_secret = settings.ClientSecret,
        };

        using var response = await _httpClient.PostAsJsonAsync("oauth/device/code", payload, SerializerOptions, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<DeviceCodeResponseDto>(SerializerOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Trakt device code response was empty.");

        return new DeviceCodeResponse(
            dto.DeviceCode,
            dto.UserCode,
            new Uri(dto.VerificationUrl, UriKind.Absolute),
            TimeSpan.FromSeconds(dto.ExpiresIn <= 0 ? 300 : dto.ExpiresIn),
            TimeSpan.FromSeconds(dto.Interval <= 0 ? 5 : dto.Interval));
    }

    public async Task<DeviceTokenPollResult> PollForDeviceTokenAsync(string deviceCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceCode))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(deviceCode));
        }

        var settings = _options.Value;
        ValidateClientCredentials(settings);

        var payload = new
        {
            code = deviceCode,
            client_id = settings.ClientId,
            client_secret = settings.ClientSecret,
        };

        using var response = await _httpClient.PostAsJsonAsync("oauth/device/token", payload, SerializerOptions, cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            var dto = await response.Content.ReadFromJsonAsync<DeviceTokenResponseDto>(SerializerOptions, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Trakt device token response was empty.");

            var token = new DeviceTokenResponse(
                dto.AccessToken,
                dto.TokenType,
                TimeSpan.FromSeconds(dto.ExpiresIn <= 0 ? 0 : dto.ExpiresIn),
                dto.RefreshToken,
                dto.Scope);

            return new DeviceTokenPollResult(token, null);
        }

        DeviceTokenErrorDto? errorDto = null;
        try
        {
            errorDto = await response.Content.ReadFromJsonAsync<DeviceTokenErrorDto>(SerializerOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse device token error response (StatusCode: {StatusCode}).", response.StatusCode);
        }

        var error = errorDto?.Error ?? response.StatusCode.ToString();
        return new DeviceTokenPollResult(null, error);
    }

    private static void ValidateClientCredentials(TraktOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            throw new InvalidOperationException("Trakt:ClientId must be configured for device authentication.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            throw new InvalidOperationException("Trakt:ClientSecret must be configured for device authentication.");
        }
    }
}
