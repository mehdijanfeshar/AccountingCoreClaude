using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class PettyCashExpenseDocRepository : IPettyCashExpenseDocRepository
{
    private static readonly PettyCashDocState[] InFlightStates =
    {
        PettyCashDocState.New,
        PettyCashDocState.PendingReview,
        PettyCashDocState.Returned,
    };

    private readonly LegacyDbContext _dbContext;

    public PettyCashExpenseDocRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_PC_EXPENSE_DOC expenseDoc, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_PC_EXPENSE_DOCs.AddAsync(expenseDoc, cancellationToken);
    }

    public async Task<TB_PC_EXPENSE_DOC?> GetForUpdateAsync(
        Guid id,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TB_PC_EXPENSE_DOCs
            .FirstOrDefaultAsync(d => d.ID == id, cancellationToken);

        VahedOwnership.EnsureOwned(entity?.VAHEDCODE, vahedCode, id, "PettyCashExpenseDoc");

        return entity;
    }

    public async Task<bool> ExistsActiveDuplicateAsync(
        string vendorNationalId,
        string invoiceNo,
        string vahedCode,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TB_PC_EXPENSE_DOCs
            .AsNoTracking()
            .Where(d => d.VAHEDCODE == vahedCode
                && !d.ISDELETED
                && d.DOC_STATE != PettyCashDocState.Rejected
                && d.VENDOR_NATIONAL_ID == vendorNationalId
                && d.INVOICE_NO == invoiceNo);

        if (excludeId is { } id)
        {
            query = query.Where(d => d.ID != id);
        }

        // CountAsync, not AnyAsync: the Oracle provider renders AnyAsync as
        // "CASE WHEN EXISTS ... THEN True ELSE False", which pre-23ai Oracle rejects (ORA-00904).
        return await query.CountAsync(cancellationToken) > 0;
    }

    public async Task<PettyCashFundExposure> GetFundExposureAsync(
        Guid fundId,
        string vahedCode,
        Guid? excludeDocId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TB_PC_EXPENSE_DOCs
            .AsNoTracking()
            .Where(d => d.FUND_ID == fundId && d.VAHEDCODE == vahedCode && !d.ISDELETED);

        if (excludeDocId is { } id)
        {
            query = query.Where(d => d.ID != id);
        }

        // Materialised client-side (bounded by one fund's live document count, never a hot
        // aggregate over the whole table) so the two-column sum is a plain C# Sum rather than
        // asking the provider to translate "SUM(a ?? 0 + b ?? 0)", which is where EF Core /
        // Oracle null-coalescing-inside-aggregate translations tend to get fragile.
        var approvedRows = await query
            .Where(d => d.DOC_STATE == PettyCashDocState.Approved)
            .Select(d => new { d.AMOUNT_BEFORE_TAX, d.VAT_AMOUNT })
            .ToListAsync(cancellationToken);

        var inFlightRows = await query
            .Where(d => InFlightStates.Contains(d.DOC_STATE))
            .Select(d => new { d.AMOUNT_BEFORE_TAX, d.VAT_AMOUNT })
            .ToListAsync(cancellationToken);

        var approvedAmount = approvedRows.Sum(r => (r.AMOUNT_BEFORE_TAX ?? 0m) + (r.VAT_AMOUNT ?? 0m));
        var inFlightAmount = inFlightRows.Sum(r => (r.AMOUNT_BEFORE_TAX ?? 0m) + (r.VAT_AMOUNT ?? 0m));

        return new PettyCashFundExposure(approvedAmount, approvedRows.Count, inFlightAmount, inFlightRows.Count);
    }

    public async Task<IReadOnlyList<PettyCashReplenishableDocRow>> GetApprovedUnlinkedByFundAsync(
        Guid fundId, string vahedCode, CancellationToken cancellationToken = default)
    {
        // Deliberately no navigation-heavy join chain (see PettyCashExpenseDocReadRepository's
        // XML doc for why): plain query-syntax joins on explicit FK columns only, so the
        // translation stays simple and predictable on both SQLite (tests) and Oracle.
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

        var result = new List<PettyCashReplenishableDocRow>(rows.Count);

        foreach (var row in rows)
        {
            // Skipping already-linked documents needs a per-row existence check rather than a
            // single SQL "NOT EXISTS" join, for the same Oracle-translation-safety reason as
            // PettyCashExpenseDocReadRepository's leaner filter shape — one fund's live Approved
            // set is small, so this stays cheap.
            var alreadyLinked = await _dbContext.TB_CHARGE_LINK_COSTs
                .AsNoTracking()
                .Where(l => !l.ISDELETED && l.COST_ID == row.DetailId)
                .CountAsync(cancellationToken) > 0;

            if (alreadyLinked)
            {
                continue;
            }

            result.Add(new PettyCashReplenishableDocRow(
                row.ID,
                row.DetailId,
                (row.AMOUNT_BEFORE_TAX ?? 0m) + (row.VAT_AMOUNT ?? 0m),
                row.AccountCodeId));
        }

        return result;
    }

    public async Task<IReadOnlyList<TB_PC_EXPENSE_DOC>> GetManyForUpdateAsync(
        IReadOnlyList<Guid> ids, string vahedCode, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<TB_PC_EXPENSE_DOC>();
        }

        return await _dbContext.TB_PC_EXPENSE_DOCs
            .Where(d => ids.Contains(d.ID) && d.VAHEDCODE == vahedCode)
            .ToListAsync(cancellationToken);
    }
}
