using System.Text.Json.Serialization;

namespace DevFund.TraktManager.Infrastructure.Dtos;

internal sealed record WatchlistEntryDto(
    [property: JsonPropertyName("rank")] int Rank,
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("listed_at")] DateTimeOffset? ListedAt,
    [property: JsonPropertyName("notes")] string? Notes,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("movie")] WatchlistMovieDto? Movie,
    [property: JsonPropertyName("show")] WatchlistShowDto? Show,
    [property: JsonPropertyName("season")] WatchlistSeasonDto? Season,
    [property: JsonPropertyName("episode")] WatchlistEpisodeDto? Episode);

internal sealed record WatchlistMovieDto(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("year")] int? Year,
    [property: JsonPropertyName("ids")] WatchlistIdsDto Ids);

internal sealed record WatchlistShowDto(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("year")] int? Year,
    [property: JsonPropertyName("ids")] WatchlistIdsDto Ids);

internal sealed record WatchlistSeasonDto(
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("ids")] WatchlistIdsDto Ids);

internal sealed record WatchlistEpisodeDto(
    [property: JsonPropertyName("season")] int Season,
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("ids")] WatchlistIdsDto Ids);

internal sealed record WatchlistIdsDto(
    [property: JsonPropertyName("trakt")] int? Trakt,
    [property: JsonPropertyName("slug")] string? Slug,
    [property: JsonPropertyName("imdb")] string? Imdb,
    [property: JsonPropertyName("tmdb")] int? Tmdb,
    [property: JsonPropertyName("tvdb")] int? Tvdb);
