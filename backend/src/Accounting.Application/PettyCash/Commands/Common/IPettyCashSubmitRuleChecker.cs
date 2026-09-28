namespace Accounting.Application.PettyCash.Commands.Common;

/// <summary>
/// The three §4 rules that apply only at Submit
/// (<c>docs/tankhah-khazaneh-module.md</c>): invoice date must fall in the current fiscal year,
/// total amount must not exceed the fund's <c>TB_PC_FUND.PER_DOC_LIMIT</c>, and total amount must
/// not exceed the fund's current cash balance (§2's equation). Shared by
/// <c>CreatePettyCashExpenseDocCommandHandler</c> (when <c>submit=true</c>) and
/// <c>SubmitPettyCashExpenseDocCommandHandler</c>, so the two can never drift apart — same "one
/// rule, one home" shape as <c>VoucherTafsiliLevelGuard</c>.
///
/// Since the 2026-09-28 TB_PC_FUND cutover, <c>fundCeiling</c>/<c>perDocLimit</c> are both
/// mandatory, non-nullable columns on the fund itself — callers pass them straight from the
/// already-loaded <c>TB_PC_FUND</c> row (this checker no longer loads a separate settings row
/// itself).
/// </summary>
public interface IPettyCashSubmitRuleChecker
{
    /// <summary>
    /// Throws the matching domain exception on the first rule that fails; does nothing when all
    /// three pass.
    /// </summary>
    /// <param name="expenseDocId">Carried by the thrown exception only — for a not-yet-persisted
    /// document (composite create), this may be a <see cref="Guid.NewGuid"/> generated up front by
    /// the caller, since no row needs to exist yet for the checks themselves.</param>
    /// <param name="fundId">The fund the document draws from.</param>
    /// <param name="fundCeiling">TB_PC_FUND.CEILING.</param>
    /// <param name="perDocLimit">TB_PC_FUND.PER_DOC_LIMIT.</param>
    /// <param name="invoiceDate">The document's INVOICE_DATE (YYYYMMDD), or null/empty (which always fails the year check).</param>
    /// <param name="year">The document's own fiscal year (YYYY) to compare the invoice date against.</param>
    /// <param name="totalAmount">AmountBeforeTax + VatAmount.</param>
    /// <param name="vahedCode">Caller's unit — scopes the exposure query.</param>
    /// <param name="excludeDocId">
    /// Excluded from the exposure calculation — always the document's own id for an existing
    /// document being (re-)submitted, so it never double-counts itself; <see langword="null"/>
    /// for a brand-new document that has no prior exposure to exclude.
    /// </param>
    Task EnsureSubmittableAsync(
        Guid expenseDocId,
        Guid fundId,
        decimal fundCeiling,
        decimal perDocLimit,
        string? invoiceDate,
        string year,
        decimal totalAmount,
        string vahedCode,
        Guid? excludeDocId,
        CancellationToken cancellationToken = default);
}
