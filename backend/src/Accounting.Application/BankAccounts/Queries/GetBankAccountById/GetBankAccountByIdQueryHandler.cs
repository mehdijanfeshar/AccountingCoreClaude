using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.BankAccounts.Queries.GetBankAccountById;

/// <summary>
/// Delegates straight to <see cref="IBankAccountReadRepository.GetByIdAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetBankAccountByIdQueryHandler : IRequestHandler<GetBankAccountByIdQuery, BankAccountDto?>
{
    private readonly IBankAccountReadRepository _readRepository;

    public GetBankAccountByIdQueryHandler(IBankAccountReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<BankAccountDto?> Handle(GetBankAccountByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
