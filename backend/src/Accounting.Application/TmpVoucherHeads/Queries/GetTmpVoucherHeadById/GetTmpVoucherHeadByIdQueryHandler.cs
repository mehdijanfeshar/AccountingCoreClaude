using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.TmpVoucherHeads.Queries.GetTmpVoucherHeadById;

/// <summary>
/// Delegates straight to <see cref="ITmpVoucherHeadReadRepository.GetByIdAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetTmpVoucherHeadByIdQueryHandler
    : IRequestHandler<GetTmpVoucherHeadByIdQuery, TmpVoucherHeadDto?>
{
    private readonly ITmpVoucherHeadReadRepository _readRepository;

    public GetTmpVoucherHeadByIdQueryHandler(ITmpVoucherHeadReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<TmpVoucherHeadDto?> Handle(
        GetTmpVoucherHeadByIdQuery request,
        CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
