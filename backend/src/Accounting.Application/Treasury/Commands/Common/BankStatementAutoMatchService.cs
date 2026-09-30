using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Common;
using Accounting.Application.Treasury.Queries;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// See <see cref="IBankStatementAutoMatchService"/> XML doc.
///
/// <b>Candidate window.</b> For each <see cref="BankStatementLineMatchState.Unmatched"/> line,
/// candidates are every unmatched دفتری line of the bank's معین/تفصیلی(s) within
/// <c>line.LINE_DATE ± 3 days</c> (owner-specified tolerance, documented here — the only place it
/// is defined) on the opposite side (a صورت‌حساب <i>deposit</i> needs a book <i>debit</i>; a
/// <i>withdrawal</i> needs a book <i>credit</i>).
///
/// <b>Priority (owner-specified, applied in order — the first that yields exactly ONE candidate
/// wins; ANY ambiguity at a step, including zero candidates surviving a later step, leaves the
/// line <see cref="BankStatementLineMatchState.Unmatched"/> rather than falling back further once
/// that step itself found more than one candidate):</b>
/// <list type="number">
/// <item><description>Bank reference equal to <c>line.BANK_REFERENCE</c> AND exact amount.</description></item>
/// <item><description>Exact amount AND exact same date (<c>line.LINE_DATE</c>).</description></item>
/// <item><description>Exact amount within the ±3-day window (unique candidate only).</description></item>
/// </list>
///
/// A line already claimed by a live <c>MATCHED_VOUCHERDETAIL_ID</c> anywhere in this unit (this
/// run's matches included, tracked in-memory) is never offered as a candidate to a later line —
/// see <see cref="ITreasuryBankStatementLineRepository.GetMatchedVoucherDetailIdsAsync"/>.
/// </summary>
public sealed class BankStatementAutoMatchService : IBankStatementAutoMatchService
{
    private const int ToleranceDays = 3;

    private readonly ITreasuryBankStatementLineRepository _lineRepository;
    private readonly IBankAccountReadRepository _bankAccountReadRepository;
    private readonly IBankStatementBookCandidateReadRepository _candidateReadRepository;
    private readonly ICurrentUser _currentUser;

    public BankStatementAutoMatchService(
        ITreasuryBankStatementLineRepository lineRepository,
        IBankAccountReadRepository bankAccountReadRepository,
        IBankStatementBookCandidateReadRepository candidateReadRepository,
        ICurrentUser currentUser)
    {
        _lineRepository = lineRepository;
        _bankAccountReadRepository = bankAccountReadRepository;
        _candidateReadRepository = candidateReadRepository;
        _currentUser = currentUser;
    }

    public async Task<BankStatementAutoMatchResult> AutoMatchAsync(
        TB_TR_BANK_STATEMENT statement, string vahedCode, CancellationToken cancellationToken = default)
    {
        var bankAccount = await _bankAccountReadRepository.GetByIdAsync(statement.BANK_ACCOUNT_ID, vahedCode, cancellationToken)
            ?? throw new NotFoundException("BankAccount", statement.BANK_ACCOUNT_ID);

        var accountCodeId = bankAccount.AccountCodeId
            ?? throw new NotFoundException("BankAccountCode", statement.BANK_ACCOUNT_ID);

        var tafsiliIds = bankAccount.TafsiliLinks.Select(l => l.TafsiliId).ToList();

        var lines = await _lineRepository.GetActiveByStatementAsync(statement.ID, cancellationToken);
        var unmatchedLines = lines.Where(l => l.MATCH_STATE == BankStatementLineMatchState.Unmatched).ToList();

        var claimed = new HashSet<Guid>(
            await _lineRepository.GetMatchedVoucherDetailIdsAsync(vahedCode, cancellationToken));

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;
        var matchedCount = 0;

        foreach (var line in unmatchedLines)
        {
            var isDeposit = line.DEPOSIT > 0;
            var lineAmount = isDeposit ? line.DEPOSIT : line.WITHDRAWAL;

            var lineDate = PettyCashSettlementPeriodCalculator.ParseJalali(line.LINE_DATE);
            var fromDate = PettyCashSettlementPeriodCalculator.ToJalaliString(lineDate.AddDays(-ToleranceDays));
            var toDate = PettyCashSettlementPeriodCalculator.ToJalaliString(lineDate.AddDays(ToleranceDays));

            var candidates = await _candidateReadRepository.GetCandidatesAsync(
                accountCodeId, tafsiliIds, debitSide: isDeposit, fromDate, toDate, vahedCode, claimed, cancellationToken);

            var match = TryMatch(candidates, line, isDeposit, lineAmount);

            if (match is null)
            {
                continue;
            }

            line.MATCH_STATE = BankStatementLineMatchState.AutoMatched;
            line.MATCHED_VOUCHERDETAIL_ID = match.VoucherDetailId;
            line.CHANGEUSERID = userId;
            line.UPDATEDDATE = now;

            claimed.Add(match.VoucherDetailId);
            matchedCount++;
        }

        return new BankStatementAutoMatchResult(matchedCount, unmatchedLines.Count - matchedCount);
    }

    private static BankStatementBookLineDto? TryMatch(
        IReadOnlyList<BankStatementBookLineDto> candidates, TB_TR_BANK_STATEMENT_LINE line, bool isDeposit, decimal lineAmount)
    {
        decimal Amount(BankStatementBookLineDto c) => isDeposit ? c.Debit : c.Credit;

        if (!string.IsNullOrWhiteSpace(line.BANK_REFERENCE))
        {
            var byRef = candidates
                .Where(c => string.Equals(c.SourceBankReference, line.BANK_REFERENCE, StringComparison.Ordinal) && Amount(c) == lineAmount)
                .ToList();

            if (byRef.Count == 1)
            {
                return byRef[0];
            }

            if (byRef.Count > 1)
            {
                return null;
            }
        }

        var byExactDate = candidates
            .Where(c => c.VoucherDate == line.LINE_DATE && Amount(c) == lineAmount)
            .ToList();

        if (byExactDate.Count == 1)
        {
            return byExactDate[0];
        }

        if (byExactDate.Count > 1)
        {
            return null;
        }

        var byAmount = candidates.Where(c => Amount(c) == lineAmount).ToList();

        return byAmount.Count == 1 ? byAmount[0] : null;
    }
}
