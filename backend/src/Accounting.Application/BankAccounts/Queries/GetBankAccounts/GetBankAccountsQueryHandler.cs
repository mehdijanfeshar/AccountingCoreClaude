using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.BankAccounts.Queries.GetBankAccounts;

/// <summary>
/// Delegates straight to <see cref="IBankAccountReadRepository.GetPagedAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
///
/// <c>request.VahedCode</c> is passed through as-is, at face value: by the time this handler
/// runs, <c>VahedScopeBehavior</c> has already overwritten it with the authenticated caller's own
/// unit code, so this handler never reads <see cref="ICurrentUser"/> directly.
/// </summary>
public sealed class GetBankAccountsQueryHandler : IRequestHandler<GetBankAccountsQuery, PagedResult<BankAccountDto>>
{
    private readonly IBankAccountReadRepository _readRepository;

    public GetBankAccountsQueryHandler(IBankAccountReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<BankAccountDto>> Handle(GetBankAccountsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, request.VahedCode, cancellationToken);
}
