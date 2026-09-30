using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Common;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetBankStatementBookCandidates;

public sealed class GetBankStatementBookCandidatesQueryHandler
    : IRequestHandler<GetBankStatementBookCandidatesQuery, IReadOnlyList<BankStatementBookLineDto>>
{
    /// <summary>Same ±3-day window <c>BankStatementAutoMatchService</c> uses — the manual-match
    /// picker offers exactly the same candidate pool auto-match would have considered.</summary>
    private const int ToleranceDays = 3;

    private readonly ITreasuryBankStatementReadRepository _statementReadRepository;
    private readonly IBankAccountReadRepository _bankAccountReadRepository;
    private readonly IBankStatementBookCandidateReadRepository _candidateReadRepository;
    private readonly ITreasuryBankStatementLineRepository _lineRepository;

    public GetBankStatementBookCandidatesQueryHandler(
        ITreasuryBankStatementReadRepository statementReadRepository,
        IBankAccountReadRepository bankAccountReadRepository,
        IBankStatementBookCandidateReadRepository candidateReadRepository,
        ITreasuryBankStatementLineRepository lineRepository)
    {
        _statementReadRepository = statementReadRepository;
        _bankAccountReadRepository = bankAccountReadRepository;
        _candidateReadRepository = candidateReadRepository;
        _lineRepository = lineRepository;
    }

    public async Task<IReadOnlyList<BankStatementBookLineDto>> Handle(
        GetBankStatementBookCandidatesQuery request, CancellationToken cancellationToken)
    {
        var header = await _statementReadRepository.GetHeaderAsync(request.StatementId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("BankStatement", request.StatementId);

        var lines = await _statementReadRepository.GetLinesAsync(request.StatementId, request.VahedCode, cancellationToken);
        var line = lines.FirstOrDefault(l => l.Id == request.LineId)
            ?? throw new NotFoundException("BankStatementLine", request.LineId);

        var bankAccount = await _bankAccountReadRepository.GetByIdAsync(header.BankAccountId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("BankAccount", header.BankAccountId);

        var accountCodeId = bankAccount.AccountCodeId
            ?? throw new NotFoundException("BankAccountCode", header.BankAccountId);

        var tafsiliIds = bankAccount.TafsiliLinks.Select(l => l.TafsiliId).ToList();

        var isDeposit = line.Deposit > 0;

        var lineDate = PettyCashSettlementPeriodCalculator.ParseJalali(line.LineDate);
        var fromDate = PettyCashSettlementPeriodCalculator.ToJalaliString(lineDate.AddDays(-ToleranceDays));
        var toDate = PettyCashSettlementPeriodCalculator.ToJalaliString(lineDate.AddDays(ToleranceDays));

        var excludeIds = await _lineRepository.GetMatchedVoucherDetailIdsAsync(request.VahedCode, cancellationToken);

        return await _candidateReadRepository.GetCandidatesAsync(
            accountCodeId, tafsiliIds, isDeposit, fromDate, toDate, request.VahedCode, excludeIds, cancellationToken);
    }
}
