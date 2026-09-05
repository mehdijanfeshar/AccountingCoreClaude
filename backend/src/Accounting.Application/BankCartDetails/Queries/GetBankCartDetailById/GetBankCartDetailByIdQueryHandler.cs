using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.BankCartDetails.Queries.GetBankCartDetailById;

/// <summary>
/// Delegates straight to <see cref="IBankCartDetailReadRepository.GetByIdAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetBankCartDetailByIdQueryHandler : IRequestHandler<GetBankCartDetailByIdQuery, BankCartDetailDto?>
{
    private readonly IBankCartDetailReadRepository _readRepository;

    public GetBankCartDetailByIdQueryHandler(IBankCartDetailReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<BankCartDetailDto?> Handle(GetBankCartDetailByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
