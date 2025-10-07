using DevFund.TraktManager.Domain.Entities;
using DevFund.TraktManager.Domain.ValueObjects;

namespace DevFund.TraktManager.Application.Contracts;

public enum ListCollectionKind
{
    Personal,
    Liked,
    Likes,
    Official,
    Saved
}

public enum ListItemsType
{
    All,
    Movies,
    Shows,
    Seasons,
    Episodes,
    People
}

public sealed class ListsRequest
{
    public ListsRequest(
        ListCollectionKind kind,
        string? userSlug = null,
        string? listSlug = null,
        ListItemsType itemType = ListItemsType.All,
        bool includeItems = false,
        int? page = null,
        int? limit = null,
        SavedFilterSection? savedFilterSection = null)
    {
        Kind = kind;
        UserSlug = string.IsNullOrWhiteSpace(userSlug) ? null : userSlug.Trim();
        ListSlug = string.IsNullOrWhiteSpace(listSlug) ? null : listSlug.Trim();
        ItemType = itemType;
        IncludeItems = includeItems;
        Page = page;
        Limit = limit;
        SavedFilterSection = savedFilterSection;
    }

    public ListCollectionKind Kind { get; }

    public string? UserSlug { get; }

    public string? ListSlug { get; }

    public ListItemsType ItemType { get; }

    public bool IncludeItems { get; }

    public int? Page { get; }

    public int? Limit { get; }

    public SavedFilterSection? SavedFilterSection { get; }

    public string ResolveUserSlugOrDefault() => string.IsNullOrWhiteSpace(UserSlug) ? "me" : UserSlug!;
}

public sealed record ListCollectionItem(UserList List, string? Origin = null, DateTimeOffset? OriginTimestamp = null);

public sealed record ListItemsGroup(ListCollectionItem List, IReadOnlyList<ListItem> Items);

public sealed class ListsResponse
{
    public ListsResponse(
        IReadOnlyList<ListCollectionItem> lists,
        IReadOnlyList<ListItemsGroup> listItems,
        IReadOnlyList<SavedFilter> savedFilters,
        PaginationMetadata? pagination)
    {
        Lists = lists ?? Array.Empty<ListCollectionItem>();
        ListItems = listItems ?? Array.Empty<ListItemsGroup>();
        SavedFilters = savedFilters ?? Array.Empty<SavedFilter>();
        Pagination = pagination;
    }

    public IReadOnlyList<ListCollectionItem> Lists { get; }

    public IReadOnlyList<ListItemsGroup> ListItems { get; }

    public IReadOnlyList<SavedFilter> SavedFilters { get; }

    public PaginationMetadata? Pagination { get; }
}

public sealed record ListCollectionResult(
    IReadOnlyList<ListCollectionItem> Lists,
    PaginationMetadata? Pagination);

public sealed record SavedFiltersResult(
    IReadOnlyList<SavedFilter> Filters,
    PaginationMetadata? Pagination);

public sealed class ListItemsRequest
{
    public ListItemsRequest(string userSlug, string listSlug, ListItemsType itemType, int? page = null, int? limit = null)
    {
        if (string.IsNullOrWhiteSpace(userSlug))
        {
            throw new ArgumentException("User slug is required.", nameof(userSlug));
        }

        if (string.IsNullOrWhiteSpace(listSlug))
        {
            throw new ArgumentException("List slug is required.", nameof(listSlug));
        }

        UserSlug = userSlug;
        ListSlug = listSlug;
        ItemType = itemType;
        Page = page;
        Limit = limit;
    }

    public string UserSlug { get; }

    public string ListSlug { get; }

    public ListItemsType ItemType { get; }

    public int? Page { get; }

    public int? Limit { get; }
}