namespace Accounting.Application.PettyCash.Queries;

/// <summary>
/// <c>GET funds/{fundId}/replenishment-preview</c> — بخش ۳-الف (<c>docs/tankhah-khazaneh-module.md</c>،
/// «بخش ۳ — طراحی»). Everything <c>CreatePettyCashReplenishmentCommand</c> will actually link/write
/// if the caller submits right now — same numbers, computed the same way, so the two can never
/// disagree (<see cref="Accounting.Application.PettyCash.Common.PettyCashBalanceCalculator"/>).
/// </summary>
/// <param name="FundId">TB_PC_FUND.ID.</param>
/// <param name="Ceiling">TB_PC_FUND.CEILING — سقف تنخواه.</param>
/// <param name="CashBalance">§2's extended equation, current state (before this ترمیم).</param>
/// <param name="ApprovedAmount">Sum of documents currently تأییدشده (منتظر ترمیم یا نه).</param>
/// <param name="ApprovedCount">Count of the same set.</param>
/// <param name="InFlightAmount">Sum of documents currently جدید/در انتظار بررسی/برگشتی.</param>
/// <param name="InFlightCount">Count of the same set.</param>
/// <param name="Lines">تأییدشده‌های هنوز لینک‌نشده، به تفکیک حساب هزینه.</param>
/// <param name="TotalAmount">Sum of <see cref="Lines"/>' <c>Amount</c> — the ترمیم that would be created.</param>
/// <param name="BalanceAfter"><see cref="CashBalance"/> + <see cref="TotalAmount"/> — the cash
/// balance once this ترمیم is created AND later پرداخت‌شده (creating it alone does not move the
/// balance; see <c>PettyCashBalanceCalculator</c> XML doc).</param>
/// <param name="DocIds">Every صورت‌هزینه id that would be linked — exactly what
/// <c>CreatePettyCashReplenishmentCommandHandler</c> iterates over.</param>
public sealed record PettyCashReplenishmentPreviewDto(
    Guid FundId,
    decimal Ceiling,
    decimal CashBalance,
    decimal ApprovedAmount,
    int ApprovedCount,
    decimal InFlightAmount,
    int InFlightCount,
    IReadOnlyList<PettyCashReplenishmentLineDto> Lines,
    decimal TotalAmount,
    decimal BalanceAfter,
    IReadOnlyList<Guid> DocIds);

/// <summary>One حساب هزینه's slice of a ترمیم preview/detail — <see cref="AccountCodeId"/> is
/// <see langword="null"/> when the underlying <c>TB_EXPENCE.ACCOUNTCODE_ID</c> itself is null (a
/// حساب هزینه with no معین linked); grouped under one line rather than dropped.</summary>
public sealed record PettyCashReplenishmentLineDto(
    Guid? AccountCodeId,
    string? AccountCode,
    string? AccountTitle,
    decimal Amount,
    int DocCount);

/// <summary>See <see cref="Accounting.Application.Common.Interfaces.IPettyCashReplenishmentReadRepository.GetUnlinkedApprovedGroupedAsync"/>.</summary>
public sealed record PettyCashReplenishmentPreviewLinesResult(
    IReadOnlyList<PettyCashReplenishmentLineDto> Lines,
    decimal TotalAmount,
    IReadOnlyList<Guid> DocIds);

/// <summary>See <see cref="Accounting.Application.Common.Interfaces.IPettyCashReplenishmentReadRepository.GetPaidSummaryAsync"/>.</summary>
public sealed record PettyCashReplenishmentPaidSummaryDto(decimal Amount, int DocCount);
