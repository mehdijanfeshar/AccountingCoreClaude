using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Vouchers.Queries.GetVoucherDetailById;

/// <summary>
/// Delegates straight to <see cref="IVoucherDetailReadRepository.GetByIdAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist. Returns
/// <see langword="null"/> when the repository finds no matching row; never throws a
/// not-found exception (matches <c>GetVoucherHeadByIdQueryHandler</c>).
/// </summary>
public sealed class GetVoucherDetailByIdQueryHandler : IRequestHandler<GetVoucherDetailByIdQuery, VoucherDetailDto?>
{
    private readonly IVoucherDetailReadRepository _readRepository;

    public GetVoucherDetailByIdQueryHandler(IVoucherDetailReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<VoucherDetailDto?> Handle(GetVoucherDetailByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
