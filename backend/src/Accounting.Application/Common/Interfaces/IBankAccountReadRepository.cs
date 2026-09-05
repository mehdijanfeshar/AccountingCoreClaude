using Accounting.Application.BankAccounts.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_ACCOUNT</c>. Deliberately separate from
/// <see cref="IBankAccountRepository"/> (the write-side repository) — this repository never
/// stages changes and always returns <see cref="BankAccounts.Queries.BankAccountDto"/>
/// projections, never the Domain entity.
/// </summary>
public interface IBankAccountReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted bank account rows ordered by <c>ACCOUNTNUMBER</c>, then
    /// <c>ID</c> as a tie-breaker for stable paging.
    /// </summary>
    Task<PagedResult<BankAccountDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the bank account row with the given <paramref name="id"/> regardless of its
    /// logical-delete state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<BankAccountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
