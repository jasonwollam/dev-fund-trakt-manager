using DevFund.TraktManager.Application.Contracts;
using DevFund.TraktManager.Domain.Entities;

namespace DevFund.TraktManager.Application.Abstractions;

public interface ITraktListsClient
{
    Task<ListCollectionResult> GetListsAsync(ListsRequest request, CancellationToken cancellationToken = default);

    Task<SavedFiltersResult> GetSavedFiltersAsync(ListsRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ListItem>> GetListItemsAsync(ListItemsRequest request, CancellationToken cancellationToken = default);

    Task<UserList?> GetListDetailsAsync(string userSlug, string listSlug, CancellationToken cancellationToken = default);
}