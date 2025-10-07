using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Domain.Entities;
using DevFund.TraktManager.Domain.ValueObjects;
using DevFund.TraktManager.Infrastructure.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DevFund.TraktManager.Infrastructure.Http;

internal sealed class TraktListsClient : ITraktListsClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ITraktAccessTokenStore _tokenStore;
    private readonly ILogger<TraktListsClient> _logger;

    public TraktListsClient(HttpClient httpClient, ITraktAccessTokenStore tokenStore, ILogger<TraktListsClient>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        _logger = logger ?? NullLogger<TraktListsClient>.Instance;
    }

    public async Task<ListCollectionResult> GetListsAsync(ListsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (path, requiresAuth) = BuildCollectionPath(request);
        var requestUri = AppendPagination(path, request.Page, request.Limit);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
        await AttachAuthorizationAsync(httpRequest, requiresAuth, cancellationToken).ConfigureAwait(false);

    using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
    EnsureSuccess(response, requiresAuth);

        var pagination = ParsePaginationMetadata(response);
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return new ListCollectionResult(Array.Empty<ListCollectionItem>(), pagination);
        }

        var origin = DetermineOriginLabel(request.Kind);
        var lists = new List<ListCollectionItem>();

        foreach (var element in document.RootElement.EnumerateArray())
        {
            try
            {
                var (listDto, originTimestamp) = ExtractListDto(element);
                if (listDto is null)
                {
                    continue;
                }

                var list = MapList(listDto);
                lists.Add(new ListCollectionItem(list, origin, originTimestamp));
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or FormatException)
            {
                _logger.LogWarning(ex, "Failed to map Trakt list payload.");
            }
        }

        return new ListCollectionResult(lists, pagination);
    }

    public async Task<SavedFiltersResult> GetSavedFiltersAsync(ListsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var section = request.SavedFilterSection ?? SavedFilterSection.Movies;
        var path = AppendPagination($"users/saved_filters/{GetSavedFilterSectionValue(section)}", request.Page, request.Limit);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, path);
        await AttachAuthorizationAsync(httpRequest, required: true, cancellationToken).ConfigureAwait(false);

    using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
    EnsureSuccess(response, requiresAuth: true);

        var pagination = ParsePaginationMetadata(response);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var dto = await JsonSerializer.DeserializeAsync<List<TraktSavedFilterDto>>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);

        if (dto is null || dto.Count == 0)
        {
            return new SavedFiltersResult(Array.Empty<SavedFilter>(), pagination);
        }

        var filters = new List<SavedFilter>(dto.Count);
        foreach (var filterDto in dto)
        {
            try
            {
                filters.Add(MapSavedFilter(filterDto));
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                _logger.LogWarning(ex, "Failed to map saved filter {FilterId}.", filterDto.Id);
            }
        }

        return new SavedFiltersResult(filters, pagination);
    }

    public async Task<IReadOnlyList<ListItem>> GetListItemsAsync(ListItemsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var typeSegment = MapListItemsType(request.ItemType);
        var pathBuilder = new StringBuilder()
            .Append("users/")
            .Append(request.UserSlug)
            .Append("/lists/")
            .Append(request.ListSlug)
            .Append("/items/")
            .Append(typeSegment)
            .Append("/rank/asc");

        var requestUri = AppendPagination(pathBuilder.ToString(), request.Page, request.Limit);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
        await AttachAuthorizationAsync(httpRequest, required: false, cancellationToken).ConfigureAwait(false);

    using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
    EnsureSuccess(response, requiresAuth: false);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var dto = await JsonSerializer.DeserializeAsync<List<TraktListItemDto>>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);

        if (dto is null || dto.Count == 0)
        {
            return Array.Empty<ListItem>();
        }

        var items = new List<ListItem>(dto.Count);
        foreach (var itemDto in dto)
        {
            try
            {
                var item = MapListItem(itemDto);
                items.Add(item);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                _logger.LogWarning(ex, "Failed to map list item {ListItemId}.", itemDto.Id);
            }
        }

        return items;
    }

    public async Task<UserList?> GetListDetailsAsync(string userSlug, string listSlug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userSlug))
        {
            throw new ArgumentException("User slug is required.", nameof(userSlug));
        }

        if (string.IsNullOrWhiteSpace(listSlug))
        {
            throw new ArgumentException("List slug is required.", nameof(listSlug));
        }

        var path = $"users/{userSlug}/lists/{listSlug}";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, path);
        await AttachAuthorizationAsync(httpRequest, required: false, cancellationToken).ConfigureAwait(false);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        EnsureSuccess(response, requiresAuth: false);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var dto = await JsonSerializer.DeserializeAsync<TraktUserListDto>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);

        return dto is null ? null : MapList(dto);
    }

    private static string AppendPagination(string path, int? page, int? limit)
    {
        if (page is null && limit is null)
        {
            return path;
        }

        var separator = path.Contains('?') ? '&' : '?';
        var builder = new StringBuilder(path).Append(separator);

        if (page is not null)
        {
            builder.Append("page=").Append(page.Value);
        }

        if (limit is not null)
        {
            if (page is not null)
            {
                builder.Append('&');
            }

            builder.Append("limit=").Append(limit.Value);
        }

        return builder.ToString();
    }

    private static string DetermineOriginLabel(ListCollectionKind kind) => kind switch
    {
        ListCollectionKind.Personal => "Personal",
        ListCollectionKind.Liked => "Liked",
        ListCollectionKind.Likes => "Likes",
        ListCollectionKind.Official => "Official",
        _ => null
    } ?? string.Empty;

    private static (TraktUserListDto? List, DateTimeOffset? OriginTimestamp) ExtractListDto(JsonElement element)
    {
        JsonElement listElement;
        DateTimeOffset? timestamp = null;

        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("list", out var nested))
        {
            listElement = nested;

            if (element.TryGetProperty("liked_at", out var likedAtElement) && likedAtElement.ValueKind == JsonValueKind.String)
            {
                if (likedAtElement.TryGetDateTimeOffset(out var likedAt))
                {
                    timestamp = likedAt;
                }
            }
        }
        else
        {
            listElement = element;
        }

        if (listElement.ValueKind is not JsonValueKind.Object)
        {
            return (null, null);
        }

        var listDto = listElement.Deserialize<TraktUserListDto>(SerializerOptions);
        return (listDto, timestamp);
    }

    private static ListItem MapListItem(TraktListItemDto dto)
    {
        if (dto.ListedAt is null)
        {
            throw new ArgumentException("List item is missing listed_at value.", nameof(dto));
        }

        var itemType = ParseListItemType(dto.Type);

        return itemType switch
        {
            ListItemType.Movie => MapMovieItem(dto, itemType),
            ListItemType.Show => MapShowItem(dto, itemType),
            ListItemType.Season => MapSeasonItem(dto, itemType),
            ListItemType.Episode => MapEpisodeItem(dto, itemType),
            ListItemType.Person => MapPersonItem(dto, itemType),
            _ => throw new ArgumentOutOfRangeException(nameof(dto.Type), dto.Type, "Unsupported list item type.")
        };
    }

    private static ListItem MapMovieItem(TraktListItemDto dto, ListItemType itemType)
    {
        if (dto.Movie is null)
        {
            throw new ArgumentException("Movie list item must include movie payload.", nameof(dto));
        }

        var ids = CreateIds(dto.Movie.Ids, "movie");
        var movie = new Movie(dto.Movie.Title, dto.Movie.Year, ids);
        return new ListItem(dto.Rank, dto.Id, dto.ListedAt!.Value, itemType, movie: movie, notes: dto.Notes);
    }

    private static ListItem MapShowItem(TraktListItemDto dto, ListItemType itemType)
    {
        if (dto.Show is null)
        {
            throw new ArgumentException("Show list item must include show payload.", nameof(dto));
        }

        var ids = CreateIds(dto.Show.Ids, "show");
        var show = new Show(dto.Show.Title, dto.Show.Year, ids);
        return new ListItem(dto.Rank, dto.Id, dto.ListedAt!.Value, itemType, show: show, notes: dto.Notes);
    }

    private static ListItem MapSeasonItem(TraktListItemDto dto, ListItemType itemType)
    {
        if (dto.Show is null || dto.Season is null)
        {
            throw new ArgumentException("Season list item must include show and season payloads.", nameof(dto));
        }

        var showIds = CreateIds(dto.Show.Ids, "show");
        var show = new Show(dto.Show.Title, dto.Show.Year, showIds);

        var seasonIds = new SeasonIds(dto.Season.Ids.Trakt, dto.Season.Ids.Tvdb, dto.Season.Ids.Tmdb);
        var season = new SeasonSummary(dto.Season.Number, seasonIds);

        return new ListItem(dto.Rank, dto.Id, dto.ListedAt!.Value, itemType, show: show, season: season, notes: dto.Notes);
    }

    private static ListItem MapEpisodeItem(TraktListItemDto dto, ListItemType itemType)
    {
        if (dto.Show is null || dto.Episode is null)
        {
            throw new ArgumentException("Episode list item must include show and episode payloads.", nameof(dto));
        }

        var showIds = CreateIds(dto.Show.Ids, "show");
        var show = new Show(dto.Show.Title, dto.Show.Year, showIds);

        var episodeIds = CreateIds(dto.Episode.Ids, "episode");
        var episode = new Episode(dto.Episode.Season, dto.Episode.Number, dto.Episode.Title, episodeIds);

        return new ListItem(dto.Rank, dto.Id, dto.ListedAt!.Value, itemType, show: show, episode: episode, notes: dto.Notes);
    }

    private static ListItem MapPersonItem(TraktListItemDto dto, ListItemType itemType)
    {
        if (dto.Person is null)
        {
            throw new ArgumentException("Person list item must include person payload.", nameof(dto));
        }

        var ids = CreateIds(dto.Person.Ids, "person");
        var person = new Person(dto.Person.Name, ids);
        return new ListItem(dto.Rank, dto.Id, dto.ListedAt!.Value, itemType, person: person, notes: dto.Notes);
    }

    private static SavedFilter MapSavedFilter(TraktSavedFilterDto dto)
    {
        var section = ParseSavedFilterSection(dto.Section);
        return new SavedFilter(dto.Rank, dto.Id, section, dto.Name, dto.Path, dto.Query, dto.UpdatedAt);
    }

    private static SavedFilterSection ParseSavedFilterSection(string section) => section?.Trim().ToLowerInvariant() switch
    {
        "movies" => SavedFilterSection.Movies,
        "shows" => SavedFilterSection.Shows,
        "calendars" => SavedFilterSection.Calendars,
        "search" => SavedFilterSection.Search,
        _ => throw new ArgumentOutOfRangeException(nameof(section), section, "Unsupported saved filter section.")
    };

    private static string GetSavedFilterSectionValue(SavedFilterSection section) => section switch
    {
        SavedFilterSection.Movies => "movies",
        SavedFilterSection.Shows => "shows",
        SavedFilterSection.Calendars => "calendars",
        SavedFilterSection.Search => "search",
        _ => "movies"
    };

    private static string MapListItemsType(ListItemsType type) => type switch
    {
        ListItemsType.All => "all",
        ListItemsType.Movies => "movies",
        ListItemsType.Shows => "shows",
        ListItemsType.Seasons => "seasons",
        ListItemsType.Episodes => "episodes",
        ListItemsType.People => "people",
        _ => "all"
    };

    private static ListItemType ParseListItemType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("List item type is required.", nameof(type));
        }

        return type.Trim().ToLowerInvariant() switch
        {
            "movie" or "movies" => ListItemType.Movie,
            "show" or "shows" => ListItemType.Show,
            "season" or "seasons" => ListItemType.Season,
            "episode" or "episodes" => ListItemType.Episode,
            "person" or "people" => ListItemType.Person,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported list item type.")
        };
    }

    private static ListPrivacy ParsePrivacy(string value) => value?.Trim().ToLowerInvariant() switch
    {
        "private" => ListPrivacy.Private,
        "link" => ListPrivacy.Link,
        "friends" => ListPrivacy.Friends,
        "public" => ListPrivacy.Public,
        _ => ListPrivacy.Public
    };

    private static ListSortOrder ParseSortOrder(string value) => string.Equals(value, "desc", StringComparison.OrdinalIgnoreCase)
        ? ListSortOrder.Desc
        : ListSortOrder.Asc;

    private static ListIds MapIds(TraktListIdsDto ids)
    {
        if (ids.Trakt is null or <= 0)
        {
            throw new ArgumentException("List ids must include a positive trakt id.", nameof(ids));
        }

        var slug = !string.IsNullOrWhiteSpace(ids.Slug)
            ? ids.Slug!
            : ids.Trakt.Value.ToString(CultureInfo.InvariantCulture);

        return new ListIds(ids.Trakt.Value, slug);
    }

    private static TraktIds CreateIds(TraktListIdsWithExtrasDto ids, string prefix)
    {
        if (ids.Trakt is null or <= 0)
        {
            throw new ArgumentException($"{prefix} ids must include a positive trakt id.", nameof(ids));
        }

        var slug = !string.IsNullOrWhiteSpace(ids.Slug) ? ids.Slug! : $"{prefix}-{ids.Trakt.Value}";
        return new TraktIds(ids.Trakt.Value, slug, ids.Imdb, ids.Tmdb);
    }

    private static UserList MapList(TraktUserListDto dto)
    {
        var privacy = ParsePrivacy(dto.Privacy);
        var sortOrder = ParseSortOrder(dto.SortHow);
        var ids = MapIds(dto.Ids);
        ListUser? owner = null;

        if (dto.User is not null)
        {
            var slug = dto.User.Ids?.Slug ?? dto.User.Username;
            owner = new ListUser(dto.User.Username, dto.User.Private, dto.User.Name, dto.User.Vip, dto.User.VipEp, slug);
        }

        Uri? shareLink = null;
        if (!string.IsNullOrWhiteSpace(dto.ShareLink) && Uri.TryCreate(dto.ShareLink, UriKind.Absolute, out var uri))
        {
            shareLink = uri;
        }

        return new UserList(
            dto.Name,
            dto.Description,
            privacy,
            shareLink,
            dto.Type,
            dto.DisplayNumbers,
            dto.AllowComments,
            dto.SortBy,
            sortOrder,
            dto.CreatedAt,
            dto.UpdatedAt,
            dto.ItemCount,
            dto.CommentCount,
            dto.Likes,
            ids,
            owner);
    }

    private (string Path, bool RequiresAuth) BuildCollectionPath(ListsRequest request)
    {
        var user = request.ResolveUserSlugOrDefault();

        return request.Kind switch
        {
            ListCollectionKind.Personal => ($"users/{user}/lists", true),
            ListCollectionKind.Liked => ($"users/{user}/lists/liked", true),
            ListCollectionKind.Likes => ($"users/{user}/likes/lists", true),
            ListCollectionKind.Official => ("lists/trending/official", false),
            _ => ($"users/{user}/lists", true)
        };
    }

    private async Task AttachAuthorizationAsync(HttpRequestMessage request, bool required, CancellationToken cancellationToken)
    {
        var token = await _tokenStore.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            if (required)
            {
                throw new InvalidOperationException("No Trakt access token is available. Authorize the device before making this request.");
            }

            return;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);
    }

    private static void EnsureSuccess(HttpResponseMessage response, bool requiresAuth)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException("Trakt API returned 401. Ensure the access token is valid and has the required scopes.");
        }

        if (requiresAuth && response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException("Trakt API returned 403. The authenticated user may not have access to this resource.");
        }

        response.EnsureSuccessStatusCode();
    }

    private static PaginationMetadata? ParsePaginationMetadata(HttpResponseMessage response)
    {
        bool TryGetHeader(string name, out int value)
        {
            value = 0;
            if (!response.Headers.TryGetValues(name, out var values))
            {
                return false;
            }

            var raw = values.FirstOrDefault();
            return raw is not null && int.TryParse(raw, out value);
        }

        if (TryGetHeader("X-Pagination-Page", out var page) &&
            TryGetHeader("X-Pagination-Limit", out var limit) &&
            TryGetHeader("X-Pagination-Page-Count", out var pageCount) &&
            TryGetHeader("X-Pagination-Item-Count", out var itemCount))
        {
            return new PaginationMetadata(page, limit, pageCount, itemCount);
        }

        return null;
    }
}

internal static class JsonElementExtensions
{
    public static bool TryGetDateTimeOffset(this JsonElement element, out DateTimeOffset value)
    {
        try
        {
            value = element.GetDateTimeOffset();
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }
}