using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Vouchers.Queries.GetVoucherDetails;

/// <summary>
/// Delegates straight to <see cref="IVoucherDetailReadRepository.GetPagedAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetVoucherDetailsQueryHandler : IRequestHandler<GetVoucherDetailsQuery, PagedResult<VoucherDetailDto>>
{
    private readonly IVoucherDetailReadRepository _readRepository;

    public GetVoucherDetailsQueryHandler(IVoucherDetailReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<VoucherDetailDto>> Handle(GetVoucherDetailsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.VoucherHeadId,
            request.Year,
            request.VahedCode,
            cancellationToken);
}
