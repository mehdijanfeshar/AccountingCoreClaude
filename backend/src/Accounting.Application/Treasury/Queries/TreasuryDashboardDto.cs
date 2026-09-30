namespace Accounting.Application.Treasury.Queries;

/// <summary>Row of <see cref="TreasuryDashboardDto.BankAccounts"/> — one bank account's current
/// GL balance (see <c>ITreasuryBankAccountBalanceReadRepository</c>).</summary>
public sealed record TreasuryDashboardBankAccountDto(Guid BankAccountId, string Label, decimal Balance);

/// <param name="Count">Payment requests <c>ReadyForExecution</c>/<c>Suspended</c> with
/// <c>DUE_DATE</c> within the next 7 days (inclusive of today).</param>
/// <param name="Amount">Sum of their <c>NET_PAYABLE_AMOUNT</c>.</param>
/// <param name="CoverageRatio"><c>TotalBankBalance / Amount</c>, or <see langword="null"/> when
/// <see cref="Amount"/> is zero (division-by-zero guard, not "no data").</param>
public sealed record TreasuryDashboardCommitmentsDto(int Count, decimal Amount, decimal? CoverageRatio);

/// <summary>Same three sources as <c>GET approval-cartable</c> (درخواست پرداخت Pending*, ترمیم
/// تنخواه PendingTreasurer, انتقال وجه PendingTreasurer), merged and summed — not "pending for me",
/// the whole unit's backlog.</summary>
public sealed record TreasuryDashboardPendingApprovalDto(int Count, decimal Amount);

/// <summary>Registered دریافت‌ها + executed پرداخت‌ها whose own شمسی business date (not the
/// registration/execution timestamp) is today.</summary>
public sealed record TreasuryDashboardTodayDto(decimal Receipts, decimal Payments, decimal Net);

/// <param name="Type">One of <c>payment</c>/<c>replenishment</c>/<c>receipt</c>/<c>transfer</c>.</param>
/// <param name="DueDate">Only populated for <c>payment</c> (<c>DUE_DATE</c>); <see langword="null"/>
/// for the other three types, which have no due-date concept.</param>
public sealed record TreasuryDashboardOpenItemDto(
    string Type,
    Guid Id,
    string Code,
    string? Counterparty,
    decimal Amount,
    string? DueDate,
    string StateLabel);

/// <summary><c>GET api/treasury/dashboard</c> — read-only overview, mirrors prototype slide ۱۴
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰، بخش ۴-د).</summary>
public sealed record TreasuryDashboardDto(
    decimal TotalBankBalance,
    IReadOnlyList<TreasuryDashboardBankAccountDto> BankAccounts,
    TreasuryDashboardCommitmentsDto CommitmentsNext7Days,
    TreasuryDashboardPendingApprovalDto PendingApproval,
    TreasuryDashboardTodayDto Today,
    IReadOnlyList<TreasuryDashboardOpenItemDto> OpenItems,
    IReadOnlyList<string> Alerts);
