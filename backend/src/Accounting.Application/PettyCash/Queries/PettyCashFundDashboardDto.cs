using Accounting.Domain.ValueObjects;

namespace Accounting.Application.PettyCash.Queries;

/// <summary>
/// <c>GET api/petty-cash/funds/{fundId}/dashboard</c> — بخش ۳-الف (<c>docs/tankhah-khazaneh-module.md</c>،
/// صفحهٔ ۴ پاورپوینت).
/// </summary>
public sealed record PettyCashFundDashboardDto(
    PettyCashFundSummaryDto Fund,
    decimal CashBalance,
    decimal CashPercentOfCeiling,
    bool BelowAlertThreshold,
    PettyCashDashboardBucketDto AwaitingReplenishment,
    PettyCashDashboardInFlightDto InFlight,
    PettyCashDashboardReturnedDto Returned,
    PettyCashDashboardBalanceCheckDto BalanceCheck,
    IReadOnlyList<PettyCashDashboardActionItemDto> TodayActions,
    IReadOnlyList<PettyCashDashboardAlertDto> Alerts);

public sealed record PettyCashFundSummaryDto(Guid Id, string Code, string Name, decimal Ceiling, int? AlertThresholdPercent);

public sealed record PettyCashDashboardBucketDto(decimal Amount, int Count);

public sealed record PettyCashDashboardInFlightDto(decimal Amount, int Count, int OlderThan5DaysCount);

public sealed record PettyCashDashboardReturnedDto(int Count, int? OldestAgeDays);

/// <summary>
/// The equation صفحهٔ ۴ shows («سقف = نقد + تأییدشده + در جریان») pre-dates ترمیم/استرداد (بخش
/// ۳-الف). Extended and algebraically verified here — see
/// <see cref="Accounting.Application.PettyCash.Common.PettyCashBalanceCalculator"/> XML doc for the
/// derivation: <c>cash = CEILING − approvedAll − inFlight + paidReplenishmentTotal + refundTotal</c>,
/// where <c>approvedAll = awaitingReplenishment + replenishedNotSettled</c> and
/// <c>replenishedNotSettled ≡ paidReplenishmentTotal</c> by construction (a ترمیم's
/// <c>TOTAL_AMOUNT</c> is exactly the sum of the صورت‌هزینه rows it links). Substituting and
/// simplifying (the <c>replenishedNotSettled</c>/<c>paidReplenishmentTotal</c> terms cancel
/// exactly) gives the identity that always holds:
/// <code>CEILING = Cash + AwaitingReplenishment + InFlight − RefundTotal</code>
/// <see cref="ReplenishedNotSettled"/> is still surfaced (it equals <see cref="PaidReplenishmentTotal"/>
/// wording-wise — informational, for the UI to display "چقدر ترمیم شده ولی هنوز تسویه نشده") but
/// deliberately plays no separate role in <see cref="Balanced"/> beyond what already cancelled out.
/// <see cref="Balanced"/> is computed from the full, unsimplified form (<c>Cash + ApprovedAll +
/// InFlight − PaidReplenishmentTotal − RefundTotal == Ceiling</c>) so it also verifies
/// <see cref="ReplenishedNotSettled"/> truly equals the fund's own paid-ترمیم total — a genuine
/// data-integrity check, not just re-deriving a tautology.
/// </summary>
public sealed record PettyCashDashboardBalanceCheckDto(
    decimal Ceiling,
    decimal Cash,
    decimal AwaitingReplenishment,
    decimal InFlight,
    decimal ReplenishedNotSettled,
    bool Balanced);

public sealed record PettyCashDashboardActionItemDto(
    Guid Id,
    string DocNumber,
    string? CustodianName,
    string? Description,
    decimal Amount,
    PettyCashDocState State,
    int AgeDays);

public sealed record PettyCashDashboardAlertDto(string Severity, string Message);
