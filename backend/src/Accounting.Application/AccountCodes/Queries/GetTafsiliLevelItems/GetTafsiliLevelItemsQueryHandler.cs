using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.AccountCodes.Queries.GetTafsiliLevelItems;

/// <summary>
/// Delegates straight to <see cref="ITafsiliLookupReadRepository.GetSelectableItemsAsync"/>.
/// Read-side handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
///
/// <c>request.VahedCode</c> is passed through as-is, at face value: by the time this handler
/// runs, <c>VahedScopeBehavior</c> has already overwritten it with the authenticated caller's own
/// unit code, so this handler never reads <see cref="ICurrentUser"/> directly — nor does it
/// derive Rule B's caller category itself. That derivation is a further database read
/// (<c>TB_VAHED_INFO → TB_VAHED_TYPE</c>), so it belongs in the repository, not here.
/// </summary>
public sealed class GetTafsiliLevelItemsQueryHandler
    : IRequestHandler<GetTafsiliLevelItemsQuery, PagedResult<TafsiliLookupItemDto>>
{
    private readonly ITafsiliLookupReadRepository _readRepository;

    public GetTafsiliLevelItemsQueryHandler(ITafsiliLookupReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<TafsiliLookupItemDto>> Handle(GetTafsiliLevelItemsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetSelectableItemsAsync(
            request.AccountCodeId,
            request.LevelId,
            request.Search,
            request.VahedCode,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
}
