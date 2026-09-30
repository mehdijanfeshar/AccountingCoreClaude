using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Queries;

/// <summary>
/// Reconciliation summary block of <c>GET api/treasury/statements/{id}</c> — خزانه‌داری، بخش ۴-د.
/// </summary>
/// <param name="ClosingBalance"><c>TB_TR_BANK_STATEMENT.CLOSING_BALANCE</c> — مانده پایانی طبق بانک.</param>
/// <param name="BookBalance">
/// موجودی دفتری حساب بانکی تا <c>TO_DATE</c> این صورت‌حساب —
/// <c>ITreasuryBankAccountBalanceReadRepository.GetBalanceAsync</c> با <c>asOfDate = TO_DATE</c>.
/// </param>
/// <param name="Difference"><see cref="ClosingBalance"/> − <see cref="BookBalance"/>. صفر یعنی
/// مغایرتی نمانده (با احتساب ردیف‌های حل‌شده).</param>
public sealed record BankStatementSummaryDto(
    decimal ClosingBalance,
    decimal BookBalance,
    decimal Difference,
    int UnmatchedCount,
    int AutoMatchedCount,
    int ManualMatchedCount,
    int ResolvedCount);

/// <summary>
/// Statement header projection only — no lines/summary/book-only. Used internally by
/// <c>GetBankStatementByIdQueryHandler</c> as the starting point it composes the full
/// <see cref="BankStatementDto"/> from (lines via <c>ITreasuryBankStatementReadRepository.GetLinesAsync</c>,
/// summary/book-only via <see cref="ITreasuryBankAccountBalanceReadRepository"/> +
/// <see cref="IBankStatementBookCandidateReadRepository"/>).
/// </summary>
public sealed record BankStatementHeaderDto(
    Guid Id,
    string Code,
    Guid BankAccountId,
    string FromDate,
    string ToDate,
    decimal ClosingBalance,
    BankStatementSource Source,
    BankStatementState State,
    string? Description,
    string AddUserId,
    DateTime CreatedDate);

/// <summary><c>GET api/treasury/statements/{id}</c> — full detail: header + lines + summary +
/// book-only (outstanding) items.</summary>
public sealed record BankStatementDto(
    Guid Id,
    string Code,
    Guid BankAccountId,
    string FromDate,
    string ToDate,
    decimal ClosingBalance,
    BankStatementSource Source,
    BankStatementState State,
    string? Description,
    string AddUserId,
    DateTime CreatedDate,
    IReadOnlyList<BankStatementLineDto> Lines,
    BankStatementSummaryDto Summary,
    IReadOnlyList<BankStatementBookLineDto> BookOnly);
