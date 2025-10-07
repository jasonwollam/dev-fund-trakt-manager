using System.Linq;
using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Domain.Entities;

namespace DevFund.TraktManager.Application.Services;

/// <summary>
/// Core application service responsible for retrieving Trakt lists, list items, and saved filters.
/// </summary>
public sealed class ListsService
{
    private readonly ITraktListsClient _listsClient;

    public ListsService(ITraktListsClient listsClient)
    {
        _listsClient = listsClient ?? throw new ArgumentNullException(nameof(listsClient));
    }

    public async Task<ListsResponse> GetListsAsync(ListsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Kind == ListCollectionKind.Saved)
        {
            var savedFiltersResult = await _listsClient.GetSavedFiltersAsync(request, cancellationToken).ConfigureAwait(false);
            return new ListsResponse(Array.Empty<ListCollectionItem>(), Array.Empty<ListItemsGroup>(), savedFiltersResult.Filters, savedFiltersResult.Pagination);
        }

        var collection = await _listsClient.GetListsAsync(request, cancellationToken).ConfigureAwait(false);
        var collections = collection.Lists;
        var listItems = new List<ListItemsGroup>();

        if (!string.IsNullOrWhiteSpace(request.ListSlug))
        {
            var targetList = collections.FirstOrDefault(item => string.Equals(item.List.Ids.Slug, request.ListSlug, StringComparison.OrdinalIgnoreCase));

            if (targetList is null)
            {
                var list = await _listsClient.GetListDetailsAsync(request.ResolveUserSlugOrDefault(), request.ListSlug!, cancellationToken).ConfigureAwait(false);
                if (list is null)
                {
                    return new ListsResponse(collections, Array.Empty<ListItemsGroup>(), Array.Empty<SavedFilter>(), collection.Pagination);
                }

                targetList = new ListCollectionItem(list);
                collections = collections.Concat(new[] { targetList }).ToArray();
            }

            var items = await FetchListItemsAsync(targetList.List, request, cancellationToken).ConfigureAwait(false);
            listItems.Add(new ListItemsGroup(targetList, items));
        }
        else if (request.IncludeItems)
        {
            foreach (var list in collections)
            {
                var items = await FetchListItemsAsync(list.List, request, cancellationToken).ConfigureAwait(false);
                if (items.Count > 0)
                {
                    listItems.Add(new ListItemsGroup(list, items));
                }
            }
        }

        return new ListsResponse(collections, listItems, Array.Empty<SavedFilter>(), collection.Pagination);
    }

    private async Task<IReadOnlyList<ListItem>> FetchListItemsAsync(UserList list, ListsRequest request, CancellationToken cancellationToken)
    {
        var ownerSlug = list.Owner?.Slug ?? request.ResolveUserSlugOrDefault();
        var listItemsRequest = new ListItemsRequest(ownerSlug, list.Ids.Slug, request.ItemType, request.Page, request.Limit);
        return await _listsClient.GetListItemsAsync(listItemsRequest, cancellationToken).ConfigureAwait(false);
    }
}