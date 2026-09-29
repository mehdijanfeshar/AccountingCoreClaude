using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_TR_ROLE"/> ("نقش خزانه‌داری") — خزانه‌داری، بخش ۴-الف
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Deliberately its own, fresh per-واحد role list,
/// independent of <c>TB_PC_REVIEWER</c> (صاحب پروژه، ۲۰۲۶-۰۹-۲۸). Only stages changes — never
/// calls SaveChanges.
/// </summary>
public interface ITreasuryRoleRepository
{
    Task AddAsync(TB_TR_ROLE role, CancellationToken cancellationToken = default);

    /// <summary>Loads a single role row by <c>ID</c>, change-tracked, scoped to
    /// <paramref name="vahedCode"/> via <c>VahedOwnership</c>. Returns <see langword="null"/> when
    /// no row with that <c>ID</c> exists.</summary>
    Task<TB_TR_ROLE?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the single row matching (<paramref name="vahedCode"/>, <paramref name="userId"/>,
    /// <paramref name="role"/>) regardless of its <c>ISDELETED</c> state, change-tracked — backs
    /// the create-or-reactivate upsert semantics of <c>CreateTreasuryRoleCommand</c> (same shape as
    /// <c>IPettyCashFundReviewerRepository.GetByFundUserAndRoleAsync</c>). Returns
    /// <see langword="null"/> when no such row exists yet.
    /// </summary>
    Task<TB_TR_ROLE?> GetByVahedUserAndRoleAsync(
        string vahedCode, string userId, Accounting.Domain.ValueObjects.TreasuryRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every active (<c>ISDELETED == false</c>) role held by <paramref name="userId"/> in
    /// <paramref name="vahedCode"/> — used by <c>ITreasuryRoleAuthorizer</c> and the bootstrap
    /// check on <c>CreateTreasuryRoleCommand</c>.
    /// </summary>
    Task<IReadOnlyCollection<Accounting.Domain.ValueObjects.TreasuryRole>> GetActiveRolesAsync(
        string vahedCode, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when <paramref name="vahedCode"/> has at least one active (<c>ISDELETED == false</c>)
    /// <see cref="Accounting.Domain.ValueObjects.TreasuryRole.FinanceManager"/> role row — the
    /// bootstrap gate on <c>CreateTreasuryRoleCommand</c> («اگر واحد هیچ FinanceManager فعالی
    /// ندارد، هر کاربر احراز‌شدهٔ واحد می‌تواند نقش ثبت کند»). Uses <c>CountAsync(...) &gt; 0</c>,
    /// never <c>AnyAsync</c> (project-wide Oracle/EF translation rule).
    /// </summary>
    Task<bool> HasActiveFinanceManagerAsync(string vahedCode, CancellationToken cancellationToken = default);
}
