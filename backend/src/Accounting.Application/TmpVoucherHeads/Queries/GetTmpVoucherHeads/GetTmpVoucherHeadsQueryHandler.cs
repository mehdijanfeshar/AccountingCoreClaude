using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.TmpVoucherHeads.Queries.GetTmpVoucherHeads;

/// <summary>
/// Delegates straight to <see cref="ITmpVoucherHeadReadRepository.GetPagedAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
///
/// <c>request.VahedCode</c> is passed through as-is, at face value: by the time this handler
/// runs, <c>VahedScopeBehavior</c> has already overwritten it with the authenticated caller's own
/// unit code, so this handler never reads <see cref="ICurrentUser"/> directly.
/// </summary>
public sealed class GetTmpVoucherHeadsQueryHandler
    : IRequestHandler<GetTmpVoucherHeadsQuery, PagedResult<TmpVoucherHeadDto>>
{
    private readonly ITmpVoucherHeadReadRepository _readRepository;

    public GetTmpVoucherHeadsQueryHandler(ITmpVoucherHeadReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<TmpVoucherHeadDto>> Handle(
        GetTmpVoucherHeadsQuery request,
        CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, request.VahedCode, cancellationToken);
}
