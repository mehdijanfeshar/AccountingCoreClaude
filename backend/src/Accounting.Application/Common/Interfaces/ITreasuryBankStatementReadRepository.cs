using Accounting.Application.Treasury.Queries;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <see cref="Accounting.Domain.Entity.TB_TR_BANK_STATEMENT"/> +
/// <see cref="Accounting.Domain.Entity.TB_TR_BANK_STATEMENT_LINE"/> — خزانه‌داری، بخش ۴-د
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Never stages changes, always returns DTO
/// projections. Deliberately does NOT assemble the full <see cref="BankStatementDto"/> (summary +
/// book-only require <see cref="ITreasuryBankAccountBalanceReadRepository"/> and
/// <see cref="IBankStatementBookCandidateReadRepository"/> too) — that composition lives in
/// <c>GetBankStatementByIdQueryHandler</c>, keeping this repository a plain projection.
/// </summary>
public interface ITreasuryBankStatementReadRepository
{
    /// <summary>
    /// <c>GET statements?bankAccountId=&amp;state=&amp;pageNumber=&amp;pageSize=</c> —
    /// newest-created-first, plus per-state row counts (unaffected by the filters).
    /// </summary>
    Task<BankStatementListResult> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? bankAccountId,
        BankStatementState? state,
        string vahedCode,
        CancellationToken cancellationToken = default);

    /// <summary>Statement header only (no lines) — <see langword="null"/> when no row with that
    /// <c>ID</c> exists.</summary>
    Task<BankStatementHeaderDto?> GetHeaderAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>Every non-deleted line of one statement, ordered by <c>LINE_DATE</c> then
    /// <c>ID</c>, projected with display labels (matched voucher number, resolution voucher/receipt
    /// labels).</summary>
    Task<IReadOnlyList<BankStatementLineDto>> GetLinesAsync(
        Guid statementId, string vahedCode, CancellationToken cancellationToken = default);
}
