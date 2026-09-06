using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.TmpVoucherHeads.Queries.GetTmpVoucherHeads;

/// <summary>
/// Delegates straight to <see cref="ITmpVoucherHeadReadRepository.GetPagedAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
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
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
}
