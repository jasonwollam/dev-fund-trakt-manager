using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Application.Contracts;

namespace DevFund.TraktManager.Application.Services;

/// <summary>
/// Coordinates lists service and presenters.
/// </summary>
public sealed class ListsOrchestrator
{
    private readonly ListsService _service;
    private readonly IEnumerable<IListsPresenter> _presenters;

    public ListsOrchestrator(ListsService service, IEnumerable<IListsPresenter> presenters)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _presenters = presenters?.ToArray() ?? throw new ArgumentNullException(nameof(presenters));
    }

    public async Task ExecuteAsync(ListsRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _service.GetListsAsync(request, cancellationToken).ConfigureAwait(false);

        foreach (var presenter in _presenters)
        {
            await presenter.PresentAsync(response, cancellationToken).ConfigureAwait(false);
        }
    }
}