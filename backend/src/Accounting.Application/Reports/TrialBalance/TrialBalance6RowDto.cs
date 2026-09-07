namespace Accounting.Application.Reports.TrialBalance;

/// <summary>
/// One row of the 6-column trial balance (تراز ۶ ستونی): adds the opening (carry-forward) turnover
/// to every column of <see cref="TrialBalance4RowDto"/>.
/// </summary>
/// <param name="Code">Account code at the requested <see cref="TrialBalanceLevel"/>.</param>
/// <param name="Description">Account name/title at the same level as <paramref name="Code"/>.</param>
/// <param name="Debtor">Period debtor turnover — <see cref="TrialBalanceAggregateRow.PeriodDebtor"/>.</param>
/// <param name="Creditor">Period creditor turnover — <see cref="TrialBalanceAggregateRow.PeriodCreditor"/>.</param>
/// <param name="DebtorBalance"><c>Math.Max(TotalDebtor - TotalCreditor, 0)</c> — see <see cref="TrialBalance4RowDto.DebtorBalance"/>.</param>
/// <param name="CreditorBalance"><c>Math.Max(TotalCreditor - TotalDebtor, 0)</c> — see <see cref="TrialBalance4RowDto.CreditorBalance"/>.</param>
/// <param name="FirstDebtor">
/// Opening balance ("مانده اول دوره"), debtor side — <c>Math.Max(OpeningDebtor - OpeningCreditor, 0)</c>.
///
/// ⚠️ <b>Project-owner decision (see <c>docs/open-decisions.md</c>):</b> the reference project's own
/// formula is plain subtraction (<c>FirstDebtor = TotDebtor - CurDebtor</c>, never
/// <c>GREATEST</c>/<c>Math.Max</c>), which can leave both <paramref name="FirstDebtor"/> and
/// <paramref name="FirstCreditor"/> non-zero simultaneously — contradicting standard trial-balance
/// convention, where a balance (unlike a turnover) is always one-sided. The project owner
/// explicitly confirmed the one-sided convention takes precedence over verbatim fidelity to the
/// reference formula, so this column is netted like <paramref name="DebtorBalance"/>/
/// <paramref name="CreditorBalance"/> — NOT threaded straight through from
/// <see cref="TrialBalanceAggregateRow.OpeningDebtor"/>.
/// </param>
/// <param name="FirstCreditor">Mirror of <paramref name="FirstDebtor"/> for the creditor side —
/// <c>Math.Max(OpeningCreditor - OpeningDebtor, 0)</c>.</param>
public sealed record TrialBalance6RowDto(
    string Code,
    string? Description,
    decimal Debtor,
    decimal Creditor,
    decimal DebtorBalance,
    decimal CreditorBalance,
    decimal FirstDebtor,
    decimal FirstCreditor);
