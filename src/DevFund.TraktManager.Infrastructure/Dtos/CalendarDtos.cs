using System.Text.Json.Serialization;

namespace DevFund.TraktManager.Infrastructure.Dtos;

internal sealed record CalendarEntryDto(
    [property: JsonPropertyName("first_aired")] DateTime? FirstAired,
    [property: JsonPropertyName("episode")] EpisodeDto Episode,
    [property: JsonPropertyName("show")] ShowDto Show);

internal sealed record EpisodeDto(
    [property: JsonPropertyName("season")] int Season,
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("ids")] IdsDto Ids);

internal sealed record ShowDto(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("year")] int? Year,
    [property: JsonPropertyName("ids")] IdsDto Ids);

internal sealed record IdsDto(
    [property: JsonPropertyName("trakt")] int Trakt,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("imdb")] string? Imdb,
    [property: JsonPropertyName("tmdb")] int? Tmdb);
