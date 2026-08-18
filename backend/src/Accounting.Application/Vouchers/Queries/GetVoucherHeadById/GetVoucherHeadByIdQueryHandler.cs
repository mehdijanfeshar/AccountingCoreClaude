using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Vouchers.Queries.GetVoucherHeadById;

/// <summary>
/// Delegates straight to <see cref="IVoucherHeadReadRepository.GetByIdAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist. Returns
/// <see langword="null"/> when the repository finds no matching row; never throws a
/// not-found exception (no such exception type exists yet in this project).
/// </summary>
public sealed class GetVoucherHeadByIdQueryHandler : IRequestHandler<GetVoucherHeadByIdQuery, VoucherHeadDto?>
{
    private readonly IVoucherHeadReadRepository _readRepository;

    public GetVoucherHeadByIdQueryHandler(IVoucherHeadReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<VoucherHeadDto?> Handle(GetVoucherHeadByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
