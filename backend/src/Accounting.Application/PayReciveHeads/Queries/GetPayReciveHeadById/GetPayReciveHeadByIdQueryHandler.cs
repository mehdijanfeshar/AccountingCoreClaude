using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PayReciveHeads.Queries.GetPayReciveHeadById;

/// <summary>
/// Delegates straight to <see cref="IPayReciveHeadReadRepository.GetByIdAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetPayReciveHeadByIdQueryHandler
    : IRequestHandler<GetPayReciveHeadByIdQuery, PayReciveHeadDto?>
{
    private readonly IPayReciveHeadReadRepository _readRepository;

    public GetPayReciveHeadByIdQueryHandler(IPayReciveHeadReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PayReciveHeadDto?> Handle(
        GetPayReciveHeadByIdQuery request,
        CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
