namespace Accounting.Application.Reports.TrialBalance;

/// <summary>
/// One row of the 8-column trial balance (تراز ۸ ستونی): adds the cumulative (opening + period)
/// turnover to every column of <see cref="TrialBalance6RowDto"/>.
/// </summary>
/// <param name="Code">Account code at the requested <see cref="TrialBalanceLevel"/>.</param>
/// <param name="Description">Account name/title at the same level as <paramref name="Code"/>.</param>
/// <param name="Debtor">Period debtor turnover — <see cref="TrialBalanceAggregateRow.PeriodDebtor"/>.</param>
/// <param name="Creditor">Period creditor turnover — <see cref="TrialBalanceAggregateRow.PeriodCreditor"/>.</param>
/// <param name="DebtorBalance"><c>Math.Max(TotalDebtor - TotalCreditor, 0)</c> — see <see cref="TrialBalance4RowDto.DebtorBalance"/>.</param>
/// <param name="CreditorBalance"><c>Math.Max(TotalCreditor - TotalDebtor, 0)</c> — see <see cref="TrialBalance4RowDto.CreditorBalance"/>.</param>
/// <param name="FirstDebtor">Opening balance, debtor side (netted) — see
/// <see cref="TrialBalance6RowDto.FirstDebtor"/> for the project-owner decision behind the
/// netting.</param>
/// <param name="FirstCreditor">Opening balance, creditor side (netted) — see
/// <see cref="TrialBalance6RowDto.FirstCreditor"/>.</param>
/// <param name="TotDebtor">
/// Cumulative RAW debtor turnover up to and including <c>ToDate</c> —
/// <see cref="TrialBalanceAggregateRow.TotalDebtor"/>, intentionally NOT netted (a turnover column,
/// not a balance column, so both <paramref name="TotDebtor"/> and <paramref name="TotCreditor"/>
/// can legitimately be non-zero). ⚠️ Does NOT equal <paramref name="FirstDebtor"/> +
/// <paramref name="Debtor"/> in general, because <paramref name="FirstDebtor"/> is netted while
/// this column is raw — that arithmetic identity only held before the opening-balance netting
/// decision (see <see cref="TrialBalance6RowDto.FirstDebtor"/>). Reconciles instead against the
/// raw <see cref="TrialBalanceAggregateRow.OpeningDebtor"/>/<see cref="TrialBalanceAggregateRow.PeriodDebtor"/>
/// pair, which the handler does not expose on this DTO.
/// </param>
/// <param name="TotCreditor">Mirror of <paramref name="TotDebtor"/> for the creditor side — raw,
/// not netted, and not expected to equal <paramref name="FirstCreditor"/> + <paramref name="Creditor"/>.</param>
public sealed record TrialBalance8RowDto(
    string Code,
    string? Description,
    decimal Debtor,
    decimal Creditor,
    decimal DebtorBalance,
    decimal CreditorBalance,
    decimal FirstDebtor,
    decimal FirstCreditor,
    decimal TotDebtor,
    decimal TotCreditor);
