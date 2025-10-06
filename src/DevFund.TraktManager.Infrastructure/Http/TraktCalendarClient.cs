using System.Net;
using System.Text.Json;
using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Domain.Entities;
using DevFund.TraktManager.Domain.ValueObjects;
using DevFund.TraktManager.Infrastructure.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DevFund.TraktManager.Infrastructure.Http;

internal sealed class TraktCalendarClient : ITraktCalendarClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<TraktCalendarClient> _logger;

    public TraktCalendarClient(HttpClient httpClient, ILogger<TraktCalendarClient>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? NullLogger<TraktCalendarClient>.Instance;
    }

    public async Task<IReadOnlyList<CalendarEntry>> GetMyShowsAsync(DateOnly startDate, int days, CancellationToken cancellationToken = default)
    {
        if (days <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(days), days, "Days must be positive.");
        }

        var requestUri = $"calendars/my/shows/{startDate:yyyy-MM-dd}/{days}";
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException("Trakt API returned 401. Ensure access token is configured.");
        }

        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var payload = await JsonSerializer.DeserializeAsync<List<CalendarEntryDto>>(responseStream, SerializerOptions, cancellationToken).ConfigureAwait(false);

        if (payload is null)
        {
            return Array.Empty<CalendarEntry>();
        }

        var results = new List<CalendarEntry>(payload.Count);
        foreach (var dto in payload)
        {
            if (dto.FirstAired is null)
            {
                _logger.LogDebug("Skipping calendar item without first_aired value.");
                continue;
            }

            try
            {
                var entry = Map(dto);
                results.Add(entry);
            }
            catch (Exception ex) when (ex is ArgumentException or ArgumentNullException)
            {
                _logger.LogWarning(ex, "Failed to map calendar entry for show {Title}.", dto.Show?.Title);
            }
        }

        return results;
    }

    private static CalendarEntry Map(CalendarEntryDto dto)
    {
        var firstAired = dto.FirstAired!.Value;
        if (firstAired.Kind == DateTimeKind.Unspecified)
        {
            firstAired = DateTime.SpecifyKind(firstAired, DateTimeKind.Utc);
        }

        var firstAiredDate = DateOnly.FromDateTime(firstAired.ToUniversalTime());
        var showIds = new TraktIds(dto.Show.Ids.Trakt, dto.Show.Ids.Slug, dto.Show.Ids.Imdb, dto.Show.Ids.Tmdb);
        var show = new Show(dto.Show.Title, dto.Show.Year, showIds);

        var episodeIds = new TraktIds(dto.Episode.Ids.Trakt, dto.Episode.Ids.Slug, dto.Episode.Ids.Imdb, dto.Episode.Ids.Tmdb);
        var episode = new Episode(dto.Episode.Season, dto.Episode.Number, dto.Episode.Title, episodeIds);

        return new CalendarEntry(firstAiredDate, show, episode);
    }
}
