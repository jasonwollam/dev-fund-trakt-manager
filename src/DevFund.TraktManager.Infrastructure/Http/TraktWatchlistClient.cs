using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Domain.Entities;
using DevFund.TraktManager.Domain.ValueObjects;
using DevFund.TraktManager.Infrastructure.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DevFund.TraktManager.Infrastructure.Http;

internal sealed class TraktWatchlistClient : ITraktWatchlistClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ITraktAccessTokenStore _tokenStore;
    private readonly ILogger<TraktWatchlistClient> _logger;

    public TraktWatchlistClient(HttpClient httpClient, ITraktAccessTokenStore tokenStore, ILogger<TraktWatchlistClient>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        _logger = logger ?? NullLogger<TraktWatchlistClient>.Instance;
    }

    public async Task<IReadOnlyList<WatchlistEntry>> GetWatchlistAsync(WatchlistRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestUri = BuildRequestUri(request);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);

        var token = await _tokenStore.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new InvalidOperationException("No Trakt access token is available. Run device authentication before requesting the watchlist.");
        }

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException("Trakt API returned 401. Ensure access token is configured and has watchlist scope.");
        }

        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var payload = await JsonSerializer.DeserializeAsync<List<WatchlistEntryDto>>(responseStream, SerializerOptions, cancellationToken).ConfigureAwait(false);

        if (payload is null || payload.Count == 0)
        {
            return Array.Empty<WatchlistEntry>();
        }

        var results = new List<WatchlistEntry>(payload.Count);
        foreach (var dto in payload)
        {
            try
            {
                var entry = Map(dto);
                results.Add(entry);
            }
            catch (Exception ex) when (ex is ArgumentException or ArgumentNullException or InvalidOperationException)
            {
                _logger.LogWarning(ex, "Failed to map watchlist entry with list item id {ListItemId}.", dto.Id);
            }
        }

        return results;
    }

    private static string BuildRequestUri(WatchlistRequest request)
    {
        var typeSegment = request.ItemFilter switch
        {
            WatchlistItemFilter.All => "all",
            WatchlistItemFilter.Movies => "movies",
            WatchlistItemFilter.Shows => "shows",
            WatchlistItemFilter.Seasons => "seasons",
            WatchlistItemFilter.Episodes => "episodes",
            _ => "all"
        };

        var sortBySegment = request.SortField switch
        {
            WatchlistSortField.Rank => "rank",
            WatchlistSortField.Added => "added",
            WatchlistSortField.Title => "title",
            WatchlistSortField.Released => "released",
            WatchlistSortField.Runtime => "runtime",
            WatchlistSortField.Popularity => "popularity",
            WatchlistSortField.Random => "random",
            WatchlistSortField.Percentage => "percentage",
            WatchlistSortField.ImdbRating => "imdb_rating",
            WatchlistSortField.TmdbRating => "tmdb_rating",
            WatchlistSortField.RtTomatometer => "rt_tomatometer",
            WatchlistSortField.RtAudience => "rt_audience",
            WatchlistSortField.Metascore => "metascore",
            WatchlistSortField.Votes => "votes",
            WatchlistSortField.ImdbVotes => "imdb_votes",
            WatchlistSortField.TmdbVotes => "tmdb_votes",
            WatchlistSortField.MyRating => "my_rating",
            WatchlistSortField.Watched => "watched",
            WatchlistSortField.Collected => "collected",
            _ => "rank"
        };

        var sortHowSegment = request.SortOrder == WatchlistSortOrder.Desc ? "desc" : "asc";

        return $"sync/watchlist/{typeSegment}/{sortBySegment}/{sortHowSegment}";
    }

    private WatchlistEntry Map(WatchlistEntryDto dto)
    {
        if (dto.ListedAt is null)
        {
            throw new ArgumentException("Watchlist entry is missing listed_at timestamp.", nameof(dto));
        }

        var itemType = ParseItemType(dto.Type);
        return itemType switch
        {
            WatchlistItemType.Movie => MapMovieEntry(dto, itemType),
            WatchlistItemType.Show => MapShowEntry(dto, itemType),
            WatchlistItemType.Season => MapSeasonEntry(dto, itemType),
            WatchlistItemType.Episode => MapEpisodeEntry(dto, itemType),
            _ => throw new ArgumentOutOfRangeException(nameof(dto.Type), dto.Type, "Unsupported watchlist item type.")
        };
    }

    private WatchlistEntry MapMovieEntry(WatchlistEntryDto dto, WatchlistItemType type)
    {
        if (dto.Movie is null)
        {
            throw new ArgumentException("Movie watchlist entry is missing movie payload.", nameof(dto));
        }

        var ids = CreateTraktIds(dto.Movie.Ids, "movie");
        var movie = new Movie(dto.Movie.Title, dto.Movie.Year, ids);

        return new WatchlistEntry(dto.Rank, dto.Id, dto.ListedAt!.Value, type, movie: movie, notes: dto.Notes);
    }

    private WatchlistEntry MapShowEntry(WatchlistEntryDto dto, WatchlistItemType type)
    {
        if (dto.Show is null)
        {
            throw new ArgumentException("Show watchlist entry is missing show payload.", nameof(dto));
        }

        var showIds = CreateTraktIds(dto.Show.Ids, "show");
        var show = new Show(dto.Show.Title, dto.Show.Year, showIds);

        return new WatchlistEntry(dto.Rank, dto.Id, dto.ListedAt!.Value, type, show: show, notes: dto.Notes);
    }

    private WatchlistEntry MapSeasonEntry(WatchlistEntryDto dto, WatchlistItemType type)
    {
        if (dto.Show is null || dto.Season is null)
        {
            throw new ArgumentException("Season watchlist entry must include show and season payloads.", nameof(dto));
        }

        var showIds = CreateTraktIds(dto.Show.Ids, "show");
        var show = new Show(dto.Show.Title, dto.Show.Year, showIds);

        var seasonIds = CreateSeasonIds(dto.Season.Ids);
        var season = new SeasonSummary(dto.Season.Number, seasonIds);

        return new WatchlistEntry(dto.Rank, dto.Id, dto.ListedAt!.Value, type, show: show, season: season, notes: dto.Notes);
    }

    private WatchlistEntry MapEpisodeEntry(WatchlistEntryDto dto, WatchlistItemType type)
    {
        if (dto.Show is null || dto.Episode is null)
        {
            throw new ArgumentException("Episode watchlist entry must include show and episode payloads.", nameof(dto));
        }

        var showIds = CreateTraktIds(dto.Show.Ids, "show");
        var show = new Show(dto.Show.Title, dto.Show.Year, showIds);

        var episodeIds = CreateTraktIds(dto.Episode.Ids, "episode");
        var episode = new Episode(dto.Episode.Season, dto.Episode.Number, dto.Episode.Title, episodeIds);

        return new WatchlistEntry(dto.Rank, dto.Id, dto.ListedAt!.Value, type, show: show, episode: episode, notes: dto.Notes);
    }

    private static WatchlistItemType ParseItemType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Watchlist entry type was not provided.", nameof(type));
        }

        return type.Trim().ToLowerInvariant() switch
        {
            "movie" => WatchlistItemType.Movie,
            "show" => WatchlistItemType.Show,
            "season" => WatchlistItemType.Season,
            "episode" => WatchlistItemType.Episode,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported watchlist item type.")
        };
    }

    private static TraktIds CreateTraktIds(WatchlistIdsDto ids, string prefix)
    {
        if (ids.Trakt is null or <= 0)
        {
            throw new ArgumentException($"{prefix} ids must include a positive Trakt id.", nameof(ids));
        }

        var slug = !string.IsNullOrWhiteSpace(ids.Slug) ? ids.Slug! : $"{prefix}-{ids.Trakt.Value}";

        return new TraktIds(ids.Trakt.Value, slug, ids.Imdb, ids.Tmdb);
    }

    private static SeasonIds CreateSeasonIds(WatchlistIdsDto ids)
    {
        return new SeasonIds(ids.Trakt, ids.Tvdb, ids.Tmdb);
    }
}
