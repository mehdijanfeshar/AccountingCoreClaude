using Accounting.Domain.ValueObjects;

namespace Accounting.Application.PettyCash.Queries;

/// <summary>
/// One مادهٔ هزینه's slice of a settlement's approved-expense set — بخش ۳-ب
/// (<c>docs/tankhah-khazaneh-module.md</c> section 9). Becomes exactly one debit line of the
/// settlement voucher (<see cref="Accounting.Application.PettyCash.Commands.Common.IPettyCashSettlementVoucherBuilder"/>).
/// </summary>
/// <param name="ExpenseId">TB_EXPENCE.ID — the مادهٔ هزینه every grouped صورت‌هزینه shares.</param>
/// <param name="AccountCodeId">TB_EXPENCE.ACCOUNTCODE_ID — nullable; a group with no معین cannot
/// become a voucher line (see <c>PettyCashSettlementExpenseAccountMissingException</c>).</param>
/// <param name="AccountCode">TB_ACCOUNTCODE.ACCCODE — display only.</param>
/// <param name="AccountTitle">TB_ACCOUNTCODE.ACCCODENAME — display only.</param>
/// <param name="Amount">Sum of (AMOUNT_BEFORE_TAX + VAT_AMOUNT) over the group's documents.</param>
/// <param name="DocCount">Count of the group's documents.</param>
/// <param name="DocIds">The group's own صورت‌هزینه ids (subset of the movement's full <c>DocIds</c>).</param>
/// <param name="Tafsilis">TB_EXPENCE_LINK_TAFSILI rows for <paramref name="ExpenseId"/> — the
/// line's تفصیلی assignment.</param>
public sealed record PettyCashSettlementExpenseGroupDto(
    Guid ExpenseId,
    Guid? AccountCodeId,
    string? AccountCode,
    string? AccountTitle,
    decimal Amount,
    int DocCount,
    IReadOnlyList<Guid> DocIds,
    IReadOnlyList<PettyCashSettlementTafsiliDto> Tafsilis);

/// <summary>One تفصیلی assignment, resolved for display/voucher use — either a مادهٔ هزینه's own
/// (<c>TB_EXPENCE_LINK_TAFSILI</c>) or a تنخواه's own (<c>TB_PC_FUND_LINK_TAFSILI</c>).</summary>
public sealed record PettyCashSettlementTafsiliDto(
    Guid TafsiliId,
    Guid LevelId,
    string? TafsiliCode,
    string? TafsiliTitle,
    string? LevelName);

/// <summary>See <see cref="Accounting.Application.Common.Interfaces.IPettyCashSettlementReadRepository.GetPeriodMovementAsync"/>.</summary>
/// <param name="ReplenishmentsAndRefunds">Sum of Paid ترمیم TOTAL_AMOUNT + non-deleted استرداد
/// AMOUNT whose own date falls within the period (inclusive both ends).</param>
/// <param name="ExpenseGroups">The settlement's debit lines — see
/// <see cref="PettyCashSettlementExpenseGroupDto"/>.</param>
/// <param name="ApprovedExpensesTotal">Sum of every group's <c>Amount</c> — the credit line's
/// amount.</param>
/// <param name="DocIds">Every صورت‌هزینه id across all groups — what gets marked
/// <see cref="PettyCashDocState.Settled"/>.</param>
public sealed record PettyCashSettlementMovementDto(
    decimal ReplenishmentsAndRefunds,
    IReadOnlyList<PettyCashSettlementExpenseGroupDto> ExpenseGroups,
    decimal ApprovedExpensesTotal,
    IReadOnlyList<Guid> DocIds);

/// <summary><c>GET funds/{fundId}/settlement</c> — بخش ۳-ب. See
/// <c>docs/tankhah-khazaneh-module.md</c> section 9 for the full field-by-field rationale.</summary>
/// <param name="PeriodId"><see langword="null"/> when no <c>TB_PC_SETTLEMENT_PERIOD</c> row has
/// been persisted yet for the current computable period (nothing counted so far) — a pure preview
/// computed on the fly, not yet backed by a database row.</param>
/// <param name="Checks">Non-fatal readiness checks — same numbers <c>finalize</c> would enforce,
/// surfaced here so the UI can show "why can't I finalize yet" before the caller tries.</param>
public sealed record PettyCashSettlementPreviewDto(
    Guid? PeriodId,
    string PeriodStart,
    string PeriodEnd,
    PettyCashSettlementState State,
    decimal OpeningBalance,
    decimal ReplenishmentsAndRefunds,
    decimal ApprovedExpenses,
    decimal InFlightAmount,
    int InFlightCount,
    decimal ClosingCashBalance,
    decimal? CountedBalance,
    PettyCashSettlementVoucherPreviewDto VoucherPreview,
    IReadOnlyList<PettyCashSettlementCheckDto> Checks,
    IReadOnlyList<Guid> ExpenseDocIds);

/// <param name="Balanced">Total debtor == total credit — by construction always true when
/// <see cref="Lines"/> is non-empty (the credit line's amount IS the sum of every debit line), but
/// still computed explicitly rather than assumed, per §۹'s "تراز قبل از SaveChangesAsync چک شود"
/// rule (defense in depth, not decoration).</param>
public sealed record PettyCashSettlementVoucherPreviewDto(
    string Date,
    string Description,
    IReadOnlyList<PettyCashSettlementVoucherLineDto> Lines,
    decimal TotalDebtor,
    decimal TotalCredit,
    bool Balanced);

public sealed record PettyCashSettlementVoucherLineDto(
    Guid? AccountCodeId,
    string? AccountCode,
    string? AccountTitle,
    IReadOnlyList<PettyCashSettlementTafsiliDto> Tafsilis,
    decimal Debtor,
    decimal Creditor);

/// <param name="Key">Stable machine key (e.g. <c>"hasApprovedDocs"</c>) — not localized, so the
/// frontend can branch on it without string-matching a Persian message.</param>
public sealed record PettyCashSettlementCheckDto(string Key, bool Ok, string Message);

/// <summary><c>GET funds/{fundId}/settlements</c> — one finalized period.</summary>
public sealed record PettyCashSettlementHistoryItemDto(
    Guid PeriodId,
    string PeriodStart,
    string PeriodEnd,
    decimal OpeningBalance,
    decimal CountedBalance,
    Guid? VoucherHeadId,
    string? VoucherDocNum,
    string? FinalizedByUserId,
    DateTime? FinalizedDate);
