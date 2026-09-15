using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.ChequesIncorrents.Queries.GetChequesIncorrentById;

/// <summary>
/// Delegates straight to <see cref="IChequesIncorrentReadRepository.GetByIdAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetChequesIncorrentByIdQueryHandler : IRequestHandler<GetChequesIncorrentByIdQuery, ChequesIncorrentDto?>
{
    private readonly IChequesIncorrentReadRepository _readRepository;

    public GetChequesIncorrentByIdQueryHandler(IChequesIncorrentReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<ChequesIncorrentDto?> Handle(GetChequesIncorrentByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
