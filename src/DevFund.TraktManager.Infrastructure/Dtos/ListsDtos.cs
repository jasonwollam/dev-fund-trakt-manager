using System;
using System.Text.Json.Serialization;

namespace DevFund.TraktManager.Infrastructure.Dtos;

internal sealed record TraktListIdsDto(
    [property: JsonPropertyName("trakt")] int? Trakt,
    [property: JsonPropertyName("slug")] string? Slug);

internal sealed record TraktListOwnerIdsDto(
    [property: JsonPropertyName("slug")] string Slug);

internal sealed record TraktListOwnerDto(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("private")] bool Private,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("vip")] bool Vip,
    [property: JsonPropertyName("vip_ep")] bool VipEp,
    [property: JsonPropertyName("ids")] TraktListOwnerIdsDto Ids);

internal sealed record TraktUserListDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("privacy")] string Privacy,
    [property: JsonPropertyName("share_link")] string? ShareLink,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("display_numbers")] bool DisplayNumbers,
    [property: JsonPropertyName("allow_comments")] bool AllowComments,
    [property: JsonPropertyName("sort_by")] string SortBy,
    [property: JsonPropertyName("sort_how")] string SortHow,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("updated_at")] DateTimeOffset UpdatedAt,
    [property: JsonPropertyName("item_count")] int ItemCount,
    [property: JsonPropertyName("comment_count")] int CommentCount,
    [property: JsonPropertyName("likes")] int Likes,
    [property: JsonPropertyName("ids")] TraktListIdsDto Ids,
    [property: JsonPropertyName("user")] TraktListOwnerDto? User);

internal sealed record TraktTrendingListDto(
    [property: JsonPropertyName("like_count")] int? LikeCount,
    [property: JsonPropertyName("comment_count")] int? CommentCount,
    [property: JsonPropertyName("list")] TraktUserListDto List);

internal sealed record TraktLikedListDto(
    [property: JsonPropertyName("liked_at")] DateTimeOffset LikedAt,
    [property: JsonPropertyName("list")] TraktUserListDto List);

internal sealed record TraktSavedFilterDto(
    [property: JsonPropertyName("rank")] int Rank,
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("section")] string Section,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("query")] string Query,
    [property: JsonPropertyName("updated_at")] DateTimeOffset UpdatedAt);

internal sealed record TraktListItemDto(
    [property: JsonPropertyName("rank")] int Rank,
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("listed_at")] DateTimeOffset? ListedAt,
    [property: JsonPropertyName("notes")] string? Notes,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("movie")] TraktListMovieDto? Movie,
    [property: JsonPropertyName("show")] TraktListShowDto? Show,
    [property: JsonPropertyName("season")] TraktListSeasonDto? Season,
    [property: JsonPropertyName("episode")] TraktListEpisodeDto? Episode,
    [property: JsonPropertyName("person")] TraktListPersonDto? Person);

internal sealed record TraktListMovieDto(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("year")] int? Year,
    [property: JsonPropertyName("ids")] TraktListIdsWithExtrasDto Ids);

internal sealed record TraktListShowDto(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("year")] int? Year,
    [property: JsonPropertyName("ids")] TraktListIdsWithExtrasDto Ids);

internal sealed record TraktListSeasonDto(
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("ids")] TraktListSeasonIdsDto Ids);

internal sealed record TraktListEpisodeDto(
    [property: JsonPropertyName("season")] int Season,
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("ids")] TraktListIdsWithExtrasDto Ids);

internal sealed record TraktListPersonDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("ids")] TraktListIdsWithExtrasDto Ids);

internal sealed record TraktListIdsWithExtrasDto(
    [property: JsonPropertyName("trakt")] int? Trakt,
    [property: JsonPropertyName("slug")] string? Slug,
    [property: JsonPropertyName("imdb")] string? Imdb,
    [property: JsonPropertyName("tmdb")] int? Tmdb,
    [property: JsonPropertyName("tvdb")] int? Tvdb);

internal sealed record TraktListSeasonIdsDto(
    [property: JsonPropertyName("trakt")] int? Trakt,
    [property: JsonPropertyName("tmdb")] int? Tmdb,
    [property: JsonPropertyName("tvdb")] int? Tvdb);