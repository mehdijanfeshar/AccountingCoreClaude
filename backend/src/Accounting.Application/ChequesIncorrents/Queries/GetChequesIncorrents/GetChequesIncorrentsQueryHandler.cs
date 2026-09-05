using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.ChequesIncorrents.Queries.GetChequesIncorrents;

/// <summary>
/// Delegates straight to <see cref="IChequesIncorrentReadRepository.GetPagedAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetChequesIncorrentsQueryHandler : IRequestHandler<GetChequesIncorrentsQuery, PagedResult<ChequesIncorrentDto>>
{
    private readonly IChequesIncorrentReadRepository _readRepository;

    public GetChequesIncorrentsQueryHandler(IChequesIncorrentReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<ChequesIncorrentDto>> Handle(GetChequesIncorrentsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
}
