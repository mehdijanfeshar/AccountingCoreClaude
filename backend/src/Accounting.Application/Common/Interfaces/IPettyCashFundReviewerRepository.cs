using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_PC_REVIEWER"/> ("بررسی‌کنندهٔ تنخواه"), the RBAC side
/// table added in بخش ۲ (<c>docs/tankhah-khazaneh-module.md</c>). Only stages changes — never
/// calls SaveChanges; the handler owns the transaction boundary via <see cref="IUnitOfWork"/>.
/// </summary>
public interface IPettyCashFundReviewerRepository
{
    Task AddAsync(TB_PC_REVIEWER reviewer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the single change-tracked reviewer row for (<paramref name="fundId"/>,
    /// <paramref name="reviewerUserId"/>) — the pair <c>UK_PC_REVIEWER</c> is unique on — or
    /// <see langword="null"/> when none exists yet. Returns soft-deleted rows too (both the
    /// upsert command, which reactivates them, and
    /// <c>Accounting.Application.PettyCash.Commands.Common.IPettyCashReviewAuthorizer</c>, which
    /// must treat them as inactive, need to see them).
    /// </summary>
    Task<TB_PC_REVIEWER?> GetByFundAndUserIdAsync(
        Guid fundId,
        string reviewerUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_PC_REVIEWER"/> by <c>ID</c> as change-tracked, verifying unit
    /// ownership via <c>VahedOwnership</c> like every other by-id repository method in this
    /// project. Returns <see langword="null"/> when no row with that <c>ID</c> exists.
    /// </summary>
    Task<TB_PC_REVIEWER?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);
}
