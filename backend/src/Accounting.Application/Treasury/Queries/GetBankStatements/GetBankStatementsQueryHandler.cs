using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetBankStatements;

public sealed class GetBankStatementsQueryHandler : IRequestHandler<GetBankStatementsQuery, BankStatementListResult>
{
    private readonly ITreasuryBankStatementReadRepository _statementReadRepository;

    public GetBankStatementsQueryHandler(ITreasuryBankStatementReadRepository statementReadRepository)
    {
        _statementReadRepository = statementReadRepository;
    }

    public Task<BankStatementListResult> Handle(GetBankStatementsQuery request, CancellationToken cancellationToken)
        => _statementReadRepository.GetPagedAsync(
            request.PageNumber, request.PageSize, request.BankAccountId, request.State, request.VahedCode, cancellationToken);
}
