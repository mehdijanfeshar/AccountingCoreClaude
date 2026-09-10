using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.AccountCodes.Queries.GetTafsiliLevels;

/// <summary>
/// Delegates straight to <see cref="ITafsiliLookupReadRepository.GetActiveLevelsAsync"/>.
/// Read-side handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetTafsiliLevelsQueryHandler
    : IRequestHandler<GetTafsiliLevelsQuery, IReadOnlyList<TafsiliLevelDto>>
{
    private readonly ITafsiliLookupReadRepository _readRepository;

    public GetTafsiliLevelsQueryHandler(ITafsiliLookupReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<IReadOnlyList<TafsiliLevelDto>> Handle(GetTafsiliLevelsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetActiveLevelsAsync(request.AccountCodeId, cancellationToken);
}
