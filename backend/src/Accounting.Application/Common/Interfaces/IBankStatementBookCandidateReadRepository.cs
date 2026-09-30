using Accounting.Application.Treasury.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Finds candidate/leftover دفتری (book) <c>TB_VOUCHERSDETAIL</c> lines of one bank account's
/// معین for مغایرت‌گیری بانکی — خزانه‌داری، بخش ۴-د (<c>docs/tankhah-khazaneh-module.md</c> §۱۰).
/// One shared query backs three call sites: the auto-match algorithm's per-line candidate search
/// (±3 days tolerance), <c>GET statements/{id}/book-candidates</c> (the manual-match picker), and
/// the statement-detail «book-only» (outstanding) list (whole statement range, both directions) —
/// see <see cref="BankStatementBookLineDto"/> XML doc.
/// </summary>
public interface IBankStatementBookCandidateReadRepository
{
    /// <summary>
    /// Same تفصیلی-scoping rule as <see cref="ITreasuryBankAccountBalanceReadRepository"/> (every
    /// تفصیلی of the bank account must be on the line; no links configured ⇒ whole معین).
    /// Non-deleted lines only, within [<paramref name="fromDate"/>, <paramref name="toDate"/>]
    /// inclusive (شمسی <c>YYYYMMDD</c> string comparison), on the <paramref name="debitSide"/>
    /// (<see langword="true"/> = <c>DEBTOR &gt; 0</c>, matching a صورت‌حساب <i>deposit</i>;
    /// <see langword="false"/> = <c>CREDITOR &gt; 0</c>, matching a <i>withdrawal</i>), excluding
    /// every id in <paramref name="excludeVoucherDetailIds"/> (already claimed by another live
    /// <c>MATCHED_VOUCHERDETAIL_ID</c>). Ordered by voucher date, then <c>DOC_NUM</c>.
    /// </summary>
    Task<IReadOnlyList<BankStatementBookLineDto>> GetCandidatesAsync(
        Guid accountCodeId,
        IReadOnlyCollection<Guid> bankTafsiliIds,
        bool debitSide,
        string fromDate,
        string toDate,
        string vahedCode,
        IReadOnlyCollection<Guid> excludeVoucherDetailIds,
        CancellationToken cancellationToken = default);
}
