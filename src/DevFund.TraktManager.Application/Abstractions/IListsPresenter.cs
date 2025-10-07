using DevFund.TraktManager.Application.Contracts;

namespace DevFund.TraktManager.Application.Abstractions;

public interface IListsPresenter
{
    Task PresentAsync(ListsResponse response, CancellationToken cancellationToken = default);
}