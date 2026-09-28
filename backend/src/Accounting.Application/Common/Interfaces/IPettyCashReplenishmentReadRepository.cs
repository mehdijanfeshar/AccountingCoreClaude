using Accounting.Application.Common;
using Accounting.Application.PettyCash.Queries;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <see cref="Accounting.Domain.Entity.TB_PC_REPLENISHMENT"/> — بخش ۳-الف
/// (<c>docs/tankhah-khazaneh-module.md</c>، «بخش ۳ — طراحی»). Never stages changes; always
/// returns DTO projections, never the Domain entity.
/// </summary>
public interface IPettyCashReplenishmentReadRepository
{
    /// <summary>
    /// The <see cref="PettyCashReplenishmentLineDto"/>/<c>TotalAmount</c>/<c>DocIds</c> slice of
    /// <c>GET funds/{fundId}/replenishment-preview</c> — every non-deleted, currently-Approved
    /// صورت‌هزینه belonging to <paramref name="fundId"/> that has no non-deleted
    /// <c>TB_CHARGE_LINK_COST</c> row yet, grouped by expense حساب (<c>TB_EXPENCE.ACCOUNTCODE_ID</c>).
    /// <c>GetPettyCashReplenishmentPreviewQueryHandler</c> combines this with
    /// <c>IPettyCashFundReadRepository.GetByIdAsync</c>'s balance figures to build the full DTO —
    /// same read-side/write-side query duplication as
    /// <see cref="IPettyCashExpenseDocRepository.GetApprovedUnlinkedByFundAsync"/> (see that
    /// method's XML doc).
    /// </summary>
    Task<PettyCashReplenishmentPreviewLinesResult> GetUnlinkedApprovedGroupedAsync(
        Guid fundId, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>GET replenishments?fundId=&amp;state=&amp;pageNumber=&amp;pageSize=</c> — newest-created-first.
    /// </summary>
    Task<PagedResult<PettyCashReplenishmentListItemDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? fundId,
        PettyCashReplenishmentState? state,
        string vahedCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>GET replenishments/{id}</c> — with lines (per expense حساب) and the full list of linked
    /// صورت‌هزینه ids. Returns <see langword="null"/> when no row with that <c>ID</c> exists.
    /// </summary>
    Task<PettyCashReplenishmentDto?> GetByIdAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// «ترمیم پرداخت‌شدهٔ هنوز‌تسویه‌نشده» — sum of <c>TOTAL_AMOUNT</c> and count of linked
    /// صورت‌هزینه documents across every non-deleted <see cref="Accounting.Domain.ValueObjects.PettyCashReplenishmentState.Paid"/>
    /// ترمیم of <paramref name="fundId"/>. Read-side twin of <c>IPettyCashReplenishmentRepository.GetPaidTotalAsync</c>
    /// plus a doc count, used by <c>GetPettyCashFundDashboardQueryHandler</c> to derive
    /// «تأییدشده، منتظر ترمیم» = (fund's total Approved) − این مجموع (بخش ۳-الف).
    /// </summary>
    Task<PettyCashReplenishmentPaidSummaryDto> GetPaidSummaryAsync(
        Guid fundId, string vahedCode, CancellationToken cancellationToken = default);
}
