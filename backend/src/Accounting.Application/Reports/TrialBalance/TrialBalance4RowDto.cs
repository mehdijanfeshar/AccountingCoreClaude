namespace Accounting.Application.Reports.TrialBalance;

/// <summary>
/// One row of the 4-column trial balance (تراز ۴ ستونی): period debtor/creditor turnover, netted
/// into a one-sided closing balance. See <see cref="Accounting.Application.Reports.TrialBalance.GetTrialBalance4.GetTrialBalance4Query"/>
/// for the query this backs.
/// </summary>
/// <param name="Code">Account code at the requested <see cref="TrialBalanceLevel"/>.</param>
/// <param name="Description">Account name/title at the same level as <paramref name="Code"/>.</param>
/// <param name="Debtor">Period debtor turnover — <see cref="TrialBalanceAggregateRow.PeriodDebtor"/>.</param>
/// <param name="Creditor">Period creditor turnover — <see cref="TrialBalanceAggregateRow.PeriodCreditor"/>.</param>
/// <param name="DebtorBalance">
/// <c>Math.Max(TotalDebtor - TotalCreditor, 0)</c> — the account's closing balance when it sits on
/// the debtor side, otherwise 0. Computed from the CUMULATIVE totals (opening + period), not from
/// <see cref="Debtor"/>/<see cref="Creditor"/> alone, so it reflects the true running balance as of
/// <c>ToDate</c> even though this DTO does not expose the opening figures separately (that is what
/// <see cref="TrialBalance6RowDto"/>/<see cref="TrialBalance8RowDto"/> are for).
/// </param>
/// <param name="CreditorBalance">
/// <c>Math.Max(TotalCreditor - TotalDebtor, 0)</c> — mirror of <paramref name="DebtorBalance"/> for
/// the creditor side. Exactly one of the two is non-zero for any given row (or both are zero when
/// the account is exactly settled) — never both positive.
/// </param>
public sealed record TrialBalance4RowDto(
    string Code,
    string? Description,
    decimal Debtor,
    decimal Creditor,
    decimal DebtorBalance,
    decimal CreditorBalance);
