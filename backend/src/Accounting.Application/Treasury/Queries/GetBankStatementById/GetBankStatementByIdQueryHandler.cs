using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetBankStatementById;

/// <summary>
/// Composes <see cref="BankStatementDto"/> from three independent sources — خزانه‌داری، بخش ۴-د
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰): (۱) header + lines
/// (<see cref="ITreasuryBankStatementReadRepository"/>), (۲) موجودی دفتری تا <c>TO_DATE</c>
/// (<see cref="ITreasuryBankAccountBalanceReadRepository"/>, <c>asOfDate</c> extension), (۳)
/// «book-only» outstanding items (<see cref="IBankStatementBookCandidateReadRepository"/>, both
/// directions, excluding this statement's own matched lines). Deliberately kept in the query
/// handler rather than the read repository — the repository stays a plain projection (see its XML
/// doc).
/// </summary>
public sealed class GetBankStatementByIdQueryHandler : IRequestHandler<GetBankStatementByIdQuery, BankStatementDto?>
{
    private readonly ITreasuryBankStatementReadRepository _statementReadRepository;
    private readonly IBankAccountReadRepository _bankAccountReadRepository;
    private readonly ITreasuryBankAccountBalanceReadRepository _balanceReadRepository;
    private readonly IBankStatementBookCandidateReadRepository _candidateReadRepository;
    private readonly ITreasuryBankStatementLineRepository _lineRepository;

    public GetBankStatementByIdQueryHandler(
        ITreasuryBankStatementReadRepository statementReadRepository,
        IBankAccountReadRepository bankAccountReadRepository,
        ITreasuryBankAccountBalanceReadRepository balanceReadRepository,
        IBankStatementBookCandidateReadRepository candidateReadRepository,
        ITreasuryBankStatementLineRepository lineRepository)
    {
        _lineRepository = lineRepository;
        _statementReadRepository = statementReadRepository;
        _bankAccountReadRepository = bankAccountReadRepository;
        _balanceReadRepository = balanceReadRepository;
        _candidateReadRepository = candidateReadRepository;
    }

    public async Task<BankStatementDto?> Handle(GetBankStatementByIdQuery request, CancellationToken cancellationToken)
    {
        var header = await _statementReadRepository.GetHeaderAsync(request.Id, request.VahedCode, cancellationToken);

        if (header is null)
        {
            return null;
        }

        var lines = await _statementReadRepository.GetLinesAsync(request.Id, request.VahedCode, cancellationToken);

        var bankAccount = await _bankAccountReadRepository.GetByIdAsync(header.BankAccountId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("BankAccount", header.BankAccountId);

        var accountCodeId = bankAccount.AccountCodeId
            ?? throw new NotFoundException("BankAccountCode", header.BankAccountId);

        var tafsiliIds = bankAccount.TafsiliLinks.Select(l => l.TafsiliId).ToList();

        var year = header.ToDate.Length >= 4 ? header.ToDate[..4] : header.ToDate;

        var bookBalance = await _balanceReadRepository.GetBalanceAsync(
            accountCodeId, tafsiliIds, request.VahedCode, year, header.ToDate, cancellationToken);

        var matchedIds = lines
            .Where(l => l.MatchedVoucherDetailId.HasValue)
            .Select(l => l.MatchedVoucherDetailId!.Value)
            .Concat(await _lineRepository.GetMatchedVoucherDetailIdsAsync(request.VahedCode, cancellationToken))
            .Distinct()
            .ToList();

        // «فقط در دفتر» از ابتدای سال، نه فقط بازهٔ صورت‌حساب: چک صادرهٔ ماه‌های قبل که هنوز وصول نشده
        // (در هیچ صورت‌حسابی تطبیق نخورده) قلم باز صورت مغایرت است. ردیف‌های تطبیق‌شده در هر صورت‌حساب کنار می‌روند.
        var bookOnlyFrom = year + "0101";

        var bookOnlyDeposits = await _candidateReadRepository.GetCandidatesAsync(
            accountCodeId, tafsiliIds, debitSide: true, bookOnlyFrom, header.ToDate, request.VahedCode, matchedIds, cancellationToken);

        var bookOnlyWithdrawals = await _candidateReadRepository.GetCandidatesAsync(
            accountCodeId, tafsiliIds, debitSide: false, bookOnlyFrom, header.ToDate, request.VahedCode, matchedIds, cancellationToken);

        // ردیف بانکیِ «رفع‌شده» با سند کارمزد یا اتصال به دریافت، قلم دفتری خودش را توضیح داده است —
        // سطرهای آن سند نباید دوباره «فقط در دفتر» شمرده شوند.
        var resolvedHeadIds = (await _lineRepository.GetResolutionVoucherHeadIdsAsync(request.VahedCode, cancellationToken))
            .ToHashSet();

        var bookOnly = bookOnlyDeposits.Concat(bookOnlyWithdrawals)
            .Where(x => !resolvedHeadIds.Contains(x.VoucherHeadId))
            .OrderBy(x => x.VoucherDate)
            .ThenBy(x => x.VoucherNumber)
            .ToList();

        var summary = new BankStatementSummaryDto(
            header.ClosingBalance,
            bookBalance,
            header.ClosingBalance - bookBalance,
            lines.Count(l => l.MatchState == BankStatementLineMatchState.Unmatched),
            lines.Count(l => l.MatchState == BankStatementLineMatchState.AutoMatched),
            lines.Count(l => l.MatchState == BankStatementLineMatchState.ManualMatched),
            lines.Count(l => l.MatchState == BankStatementLineMatchState.Resolved));

        return new BankStatementDto(
            header.Id,
            header.Code,
            header.BankAccountId,
            header.FromDate,
            header.ToDate,
            header.ClosingBalance,
            header.Source,
            header.State,
            header.Description,
            header.AddUserId,
            header.CreatedDate,
            lines,
            summary,
            bookOnly);
    }
}
