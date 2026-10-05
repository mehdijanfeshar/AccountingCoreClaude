using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_TR_BANK_STATEMENT_LINE"/> — خزانه‌داری، بخش ۴-د
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Unlike a <c>*_LINK_TAFSIL*</c> table this is a
/// full aggregate root with its own CRUD (see the entity's XML doc) — same shape as
/// <c>IVoucherDetailRepository</c> under <c>TB_VOUCHERSHEAD</c>, just parent-scoped to
/// <c>TB_TR_BANK_STATEMENT</c> instead. Only stages changes — never calls SaveChanges.
/// </summary>
public interface ITreasuryBankStatementLineRepository
{
    Task AddAsync(TB_TR_BANK_STATEMENT_LINE line, CancellationToken cancellationToken = default);

    /// <summary>Loads a single line by <c>ID</c>, change-tracked. Returns <see langword="null"/>
    /// when no row with that <c>ID</c> exists, or it does not belong to
    /// <paramref name="statementId"/>.</summary>
    Task<TB_TR_BANK_STATEMENT_LINE?> GetForUpdateAsync(
        Guid id, Guid statementId, CancellationToken cancellationToken = default);

    /// <summary>Every non-deleted line of one statement, change-tracked, ordered by
    /// <c>LINE_DATE</c> then <c>ID</c>.</summary>
    Task<IReadOnlyList<TB_TR_BANK_STATEMENT_LINE>> GetActiveByStatementAsync(
        Guid statementId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every distinct non-null <c>MATCHED_VOUCHERDETAIL_ID</c> across every non-deleted line of
    /// every non-deleted statement of <paramref name="vahedCode"/> — system-wide, not scoped to
    /// one statement, so the same book line can never be claimed by two different صورت‌حساب lines.
    /// Backs the auto-match/manual-match/book-candidates exclusion set on
    /// <see cref="IBankStatementBookCandidateReadRepository.GetCandidatesAsync"/>.
    /// </summary>
    /// <summary>
    /// سرسندهای اسنادی که در هر صورت‌حساب واحد یک ردیف بانکی را «رفع» کرده‌اند — سند کارمزد، یا سند دریافتِ
    /// متصل‌شده. این‌ها در «فقط در دفتر» شمرده نمی‌شوند.
    /// </summary>
    Task<IReadOnlyCollection<Guid>> GetResolutionVoucherHeadIdsAsync(
        string vahedCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Guid>> GetMatchedVoucherDetailIdsAsync(
        string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes every non-deleted line of <paramref name="statementId"/> — cascade for
    /// <c>DeleteBankStatementCommandHandler</c>, same "delete the parent, cascade the children in
    /// the same <c>SaveChangesAsync</c>" shape as <c>IVoucherHeadRepository.SoftDeleteDetailTreeAsync</c>.
    /// Load-and-mutate (never <c>ExecuteUpdateAsync</c>) for the same change-tracker-consistency
    /// reason documented there. Returns the number of lines staged for update.
    /// </summary>
    Task<int> SoftDeleteByStatementAsync(
        Guid statementId, string? changeUserId, DateTime updatedDate, CancellationToken cancellationToken = default);
}
