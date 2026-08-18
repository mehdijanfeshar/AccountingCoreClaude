using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Vouchers.Queries.GetVoucherHeads;

/// <summary>
/// Delegates straight to <see cref="IVoucherHeadReadRepository.GetPagedAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetVoucherHeadsQueryHandler : IRequestHandler<GetVoucherHeadsQuery, PagedResult<VoucherHeadDto>>
{
    private readonly IVoucherHeadReadRepository _readRepository;

    public GetVoucherHeadsQueryHandler(IVoucherHeadReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<VoucherHeadDto>> Handle(GetVoucherHeadsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, request.Year, request.VahedCode, cancellationToken);
}
