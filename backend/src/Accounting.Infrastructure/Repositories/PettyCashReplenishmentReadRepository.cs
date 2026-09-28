using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Queries;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IPettyCashReplenishmentReadRepository"/>. Reads
/// directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> — same deliberate,
/// narrow exception as <c>PettyCashFundReadRepository</c>/<c>PettyCashExpenseDocReadRepository</c>.
/// </summary>
public sealed class PettyCashReplenishmentReadRepository : IPettyCashReplenishmentReadRepository
{
    private sealed record Row(
        TB_PC_REPLENISHMENT Replenishment,
        TB_CHARGEANDCOST_HEAD Head,
        TB_PC_FUND Fund,
        TB_ACCOUNT? SourceAccount);

    private readonly LegacyDbContext _dbContext;

    public PettyCashReplenishmentReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PettyCashReplenishmentPreviewLinesResult> GetUnlinkedApprovedGroupedAsync(
        Guid fundId, string vahedCode, CancellationToken cancellationToken = default)
    {
        // Same plain-join, no-navigation-chain shape as PettyCashExpenseDocRepository's write-side
        // twin (GetApprovedUnlinkedByFundAsync) — kept independent on purpose, see that method's
        // XML doc.
        var rows = await (
            from doc in _dbContext.TB_PC_EXPENSE_DOCs.AsNoTracking()
            where doc.FUND_ID == fundId && doc.VAHEDCODE == vahedCode && !doc.ISDELETED
                  && doc.DOC_STATE == PettyCashDocState.Approved
            join detail in _dbContext.TB_CHARGEANDCOST_DETAILs.AsNoTracking()
                on (Guid?)doc.CHARGEANDCOSTHEAD_ID equals detail.CHARGEANDCOSTHEAD_ID
            join expense in _dbContext.TB_EXPENCEs.AsNoTracking()
                on detail.EXPENSE_ID equals (Guid?)expense.ID into expenseGroup
            from expense in expenseGroup.DefaultIfEmpty()
            select new
            {
                doc.ID,
                DetailId = detail.ID,
                doc.AMOUNT_BEFORE_TAX,
                doc.VAT_AMOUNT,
                AccountCodeId = (Guid?)expense.ACCOUNTCODE_ID,
            })
            .ToListAsync(cancellationToken);

        var unlinked = new List<(Guid DocId, decimal Amount, Guid? AccountCodeId)>();

        foreach (var row in rows)
        {
            var alreadyLinked = await _dbContext.TB_CHARGE_LINK_COSTs
                .AsNoTracking()
                .Where(l => !l.ISDELETED && l.COST_ID == row.DetailId)
                .CountAsync(cancellationToken) > 0;

            if (!alreadyLinked)
            {
                unlinked.Add((row.ID, (row.AMOUNT_BEFORE_TAX ?? 0m) + (row.VAT_AMOUNT ?? 0m), row.AccountCodeId));
            }
        }

        if (unlinked.Count == 0)
        {
            return new PettyCashReplenishmentPreviewLinesResult(Array.Empty<PettyCashReplenishmentLineDto>(), 0m, Array.Empty<Guid>());
        }

        var accountCodeIds = unlinked.Where(u => u.AccountCodeId.HasValue).Select(u => u.AccountCodeId!.Value).Distinct().ToList();

        var accountCodes = accountCodeIds.Count == 0
            ? new Dictionary<Guid, (string? Code, string? Title)>()
            : (await _dbContext.TB_ACCOUNTCODEs.AsNoTracking()
                .Where(a => accountCodeIds.Contains(a.ID))
                .Select(a => new { a.ID, a.ACCCODE, a.ACCCODENAME })
                .ToListAsync(cancellationToken))
                .ToDictionary(a => a.ID, a => (a.ACCCODE, a.ACCCODENAME));

        var lines = unlinked
            .GroupBy(u => u.AccountCodeId)
            .Select(g =>
            {
                var (code, title) = g.Key.HasValue && accountCodes.TryGetValue(g.Key.Value, out var info) ? info : (null, null);
                return new PettyCashReplenishmentLineDto(g.Key, code, title, g.Sum(x => x.Amount), g.Count());
            })
            .OrderBy(l => l.AccountCode)
            .ToList();

        return new PettyCashReplenishmentPreviewLinesResult(
            lines, unlinked.Sum(u => u.Amount), unlinked.Select(u => u.DocId).ToList());
    }

    public async Task<PagedResult<PettyCashReplenishmentListItemDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? fundId,
        PettyCashReplenishmentState? state,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TB_PC_REPLENISHMENTs.AsNoTracking()
            .Where(r => !r.ISDELETED && r.VAHEDCODE == vahedCode);

        if (fundId is { } id)
        {
            query = query.Where(r => r.FUND_ID == id);
        }

        if (state is { } s)
        {
            query = query.Where(r => r.STATE == s);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageIds = await query
            .OrderByDescending(r => r.CREATEDDATE)
            .ThenByDescending(r => r.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => r.ID)
            .ToListAsync(cancellationToken);

        var rows = await HydrateRowsAsync(pageIds, cancellationToken);

        var docCounts = await GetDocCountsAsync(pageIds, cancellationToken);

        var items = pageIds
            .Select(id => rows[id])
            .Select(r => ToListItemDto(r, docCounts.GetValueOrDefault(r.Replenishment.ID)))
            .ToList();

        return new PagedResult<PettyCashReplenishmentListItemDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<PettyCashReplenishmentDto?> GetByIdAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        var ownerVahedCode = await _dbContext.TB_PC_REPLENISHMENTs
            .AsNoTracking()
            .Where(r => r.ID == id)
            .Select(r => r.VAHEDCODE)
            .FirstOrDefaultAsync(cancellationToken);

        VahedOwnership.EnsureOwned(ownerVahedCode, vahedCode, id, "PettyCashReplenishment");

        var rows = await HydrateRowsAsync(new[] { id }, cancellationToken);

        if (!rows.TryGetValue(id, out var row))
        {
            return null;
        }

        var linkedCostIds = await _dbContext.TB_CHARGE_LINK_COSTs
            .AsNoTracking()
            .Where(l => !l.ISDELETED && l.CHARGE_ID == row.Head.ID)
            .Select(l => l.COST_ID)
            .ToListAsync(cancellationToken);

        var linkedCostIdSet = linkedCostIds.Where(c => c.HasValue).Select(c => c!.Value).ToList();

        var docIds = linkedCostIdSet.Count == 0
            ? new List<Guid>()
            : await _dbContext.TB_CHARGEANDCOST_DETAILs.AsNoTracking()
                .Where(d => linkedCostIdSet.Contains(d.ID))
                .Join(_dbContext.TB_PC_EXPENSE_DOCs.AsNoTracking(),
                    d => (Guid?)d.CHARGEANDCOSTHEAD_ID,
                    doc => (Guid?)doc.CHARGEANDCOSTHEAD_ID,
                    (d, doc) => doc.ID)
                .ToListAsync(cancellationToken);

        var lines = await BuildLinesForDocsAsync(docIds, cancellationToken);

        return new PettyCashReplenishmentDto(
            row.Replenishment.ID,
            row.Replenishment.CODE,
            row.Replenishment.FUND_ID,
            row.Fund.NAME,
            row.Head.ACCOUNT_ID,
            row.SourceAccount?.ACCOUNTNUMBER,
            row.Replenishment.PAYMENT_METHOD,
            row.Replenishment.STATE,
            row.Replenishment.TOTAL_AMOUNT,
            row.Replenishment.NOTE,
            row.Replenishment.PAID_DATE,
            row.Replenishment.PAID_BY_USERID,
            row.Replenishment.APPROVED_BY_USERID,
            row.Replenishment.ADDUSERID,
            row.Replenishment.CREATEDDATE,
            lines,
            docIds);
    }

    private async Task<IReadOnlyList<PettyCashReplenishmentLineDto>> BuildLinesForDocsAsync(
        IReadOnlyList<Guid> docIds, CancellationToken cancellationToken)
    {
        if (docIds.Count == 0)
        {
            return Array.Empty<PettyCashReplenishmentLineDto>();
        }

        var rows = await (
            from doc in _dbContext.TB_PC_EXPENSE_DOCs.AsNoTracking()
            where docIds.Contains(doc.ID)
            join detail in _dbContext.TB_CHARGEANDCOST_DETAILs.AsNoTracking()
                on (Guid?)doc.CHARGEANDCOSTHEAD_ID equals detail.CHARGEANDCOSTHEAD_ID
            join expense in _dbContext.TB_EXPENCEs.AsNoTracking()
                on detail.EXPENSE_ID equals (Guid?)expense.ID into expenseGroup
            from expense in expenseGroup.DefaultIfEmpty()
            select new
            {
                doc.AMOUNT_BEFORE_TAX,
                doc.VAT_AMOUNT,
                AccountCodeId = (Guid?)expense.ACCOUNTCODE_ID,
            })
            .ToListAsync(cancellationToken);

        var accountCodeIds = rows.Where(r => r.AccountCodeId.HasValue).Select(r => r.AccountCodeId!.Value).Distinct().ToList();

        var accountCodes = accountCodeIds.Count == 0
            ? new Dictionary<Guid, (string? Code, string? Title)>()
            : (await _dbContext.TB_ACCOUNTCODEs.AsNoTracking()
                .Where(a => accountCodeIds.Contains(a.ID))
                .Select(a => new { a.ID, a.ACCCODE, a.ACCCODENAME })
                .ToListAsync(cancellationToken))
                .ToDictionary(a => a.ID, a => (a.ACCCODE, a.ACCCODENAME));

        return rows
            .GroupBy(r => r.AccountCodeId)
            .Select(g =>
            {
                var (code, title) = g.Key.HasValue && accountCodes.TryGetValue(g.Key.Value, out var info) ? info : (null, null);
                return new PettyCashReplenishmentLineDto(
                    g.Key, code, title, g.Sum(x => (x.AMOUNT_BEFORE_TAX ?? 0m) + (x.VAT_AMOUNT ?? 0m)), g.Count());
            })
            .OrderBy(l => l.AccountCode)
            .ToList();
    }

    private async Task<Dictionary<Guid, int>> GetDocCountsAsync(IReadOnlyList<Guid> replenishmentIds, CancellationToken cancellationToken)
    {
        if (replenishmentIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var headIds = await _dbContext.TB_PC_REPLENISHMENTs.AsNoTracking()
            .Where(r => replenishmentIds.Contains(r.ID))
            .Select(r => new { r.ID, r.CHARGEANDCOSTHEAD_ID })
            .ToListAsync(cancellationToken);

        var headToReplenishment = headIds.ToDictionary(h => h.CHARGEANDCOSTHEAD_ID, h => h.ID);

        var links = await _dbContext.TB_CHARGE_LINK_COSTs.AsNoTracking()
            .Where(l => !l.ISDELETED && l.CHARGE_ID != null && headToReplenishment.Keys.Contains(l.CHARGE_ID!.Value))
            .Select(l => l.CHARGE_ID!.Value)
            .ToListAsync(cancellationToken);

        return links
            .GroupBy(headId => headToReplenishment[headId])
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private async Task<Dictionary<Guid, Row>> HydrateRowsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, Row>();
        }

        var rows = await (
            from replenishment in _dbContext.TB_PC_REPLENISHMENTs.AsNoTracking()
            where ids.Contains(replenishment.ID)
            join head in _dbContext.TB_CHARGEANDCOST_HEADs.AsNoTracking() on replenishment.CHARGEANDCOSTHEAD_ID equals head.ID
            join fund in _dbContext.TB_PC_FUNDs.AsNoTracking() on replenishment.FUND_ID equals fund.ID
            join account in _dbContext.TB_ACCOUNTs.AsNoTracking()
                on head.ACCOUNT_ID equals (Guid?)account.ID into accountGroup
            from account in accountGroup.DefaultIfEmpty()
            select new Row(replenishment, head, fund, account))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.Replenishment.ID);
    }

    public async Task<PettyCashReplenishmentPaidSummaryDto> GetPaidSummaryAsync(
        Guid fundId, string vahedCode, CancellationToken cancellationToken = default)
    {
        var paidHeadIds = await _dbContext.TB_PC_REPLENISHMENTs.AsNoTracking()
            .Where(r => r.FUND_ID == fundId && r.VAHEDCODE == vahedCode && !r.ISDELETED
                        && r.STATE == PettyCashReplenishmentState.Paid)
            .Select(r => new { r.CHARGEANDCOSTHEAD_ID, r.TOTAL_AMOUNT })
            .ToListAsync(cancellationToken);

        if (paidHeadIds.Count == 0)
        {
            return new PettyCashReplenishmentPaidSummaryDto(0m, 0);
        }

        var headIds = paidHeadIds.Select(h => h.CHARGEANDCOSTHEAD_ID).ToList();

        var docCount = await _dbContext.TB_CHARGE_LINK_COSTs.AsNoTracking()
            .Where(l => !l.ISDELETED && l.CHARGE_ID != null && headIds.Contains(l.CHARGE_ID!.Value))
            .CountAsync(cancellationToken);

        return new PettyCashReplenishmentPaidSummaryDto(paidHeadIds.Sum(h => h.TOTAL_AMOUNT), docCount);
    }

    private static PettyCashReplenishmentListItemDto ToListItemDto(Row row, int docCount) => new(
        row.Replenishment.ID,
        row.Replenishment.CODE,
        row.Replenishment.FUND_ID,
        row.Fund.NAME,
        row.Replenishment.PAYMENT_METHOD,
        row.Replenishment.STATE,
        row.Replenishment.TOTAL_AMOUNT,
        docCount,
        row.Replenishment.CREATEDDATE,
        row.Replenishment.PAID_DATE,
        row.Replenishment.ADDUSERID);
}
