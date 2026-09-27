using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Queries;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IPettyCashExpenseDocReadRepository"/>.
///
/// Joins <c>TB_PC_EXPENSE_DOC</c> to its Legacy <c>TB_CHARGEANDCOST_HEAD</c>/
/// <c>TB_CHARGEANDCOST_DETAIL</c>/<c>TB_EXPENCE</c> chain and to <c>TB_REVOLVING_FUND</c>, all
/// with plain query-syntax joins on explicit FK columns (never nested-collection navigation
/// dereferences), so the translation stays simple and predictable on both SQLite (tests) and
/// Oracle. <c>TB_CHARGEANDCOST_DETAIL.CHARGEANDCOSTHEAD_ID</c>/<c>EXPENSE_ID</c> are both
/// nullable at the Legacy schema level (unlike this module's own tables), hence the explicit
/// <c>Guid?</c> casts on those two join keys.
/// </summary>
public sealed class PettyCashExpenseDocReadRepository : IPettyCashExpenseDocReadRepository
{
    private sealed record Row(
        TB_PC_EXPENSE_DOC Doc,
        TB_CHARGEANDCOST_HEAD Head,
        TB_REVOLVING_FUND Fund,
        TB_CHARGEANDCOST_DETAIL? Detail,
        TB_EXPENCE? Expense);

    private readonly LegacyDbContext _dbContext;

    public PettyCashExpenseDocReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private IQueryable<Row> BaseQuery(string vahedCode) =>
        from doc in _dbContext.TB_PC_EXPENSE_DOCs.AsNoTracking()
        join head in _dbContext.TB_CHARGEANDCOST_HEADs.AsNoTracking() on doc.CHARGEANDCOSTHEAD_ID equals head.ID
        join fund in _dbContext.TB_REVOLVING_FUNDs.AsNoTracking() on doc.REVOLVINGFUND_ID equals fund.ID
        join detail in _dbContext.TB_CHARGEANDCOST_DETAILs.AsNoTracking()
            on (Guid?)doc.CHARGEANDCOSTHEAD_ID equals detail.CHARGEANDCOSTHEAD_ID into detailGroup
        from detail in detailGroup.DefaultIfEmpty()
        join expense in _dbContext.TB_EXPENCEs.AsNoTracking()
            on detail!.EXPENSE_ID equals (Guid?)expense.ID into expenseGroup
        from expense in expenseGroup.DefaultIfEmpty()
        where !doc.ISDELETED && doc.VAHEDCODE == vahedCode
        select new Row(doc, head, fund, detail, expense);

    private static IQueryable<Row> ApplyFundAndSearch(IQueryable<Row> query, Guid? fundId, string? search)
    {
        if (fundId is { } id)
        {
            query = query.Where(r => r.Doc.REVOLVINGFUND_ID == id);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r =>
                (r.Head.CHARGEANDCOST_CODE != null && r.Head.CHARGEANDCOST_CODE.Contains(term)) ||
                (r.Doc.VENDOR_NAME != null && r.Doc.VENDOR_NAME.Contains(term)) ||
                (r.Doc.INVOICE_NO != null && r.Doc.INVOICE_NO.Contains(term)) ||
                (r.Head.DESCRIPTION != null && r.Head.DESCRIPTION.Contains(term)));
        }

        return query;
    }

    private static IQueryable<Row> ApplyState(IQueryable<Row> query, PettyCashDocState? state, IReadOnlyList<PettyCashDocState>? states)
    {
        // States takes precedence over State when non-empty — see PettyCashExpenseDocFilter XML doc.
        if (states is { Count: > 0 })
        {
            return query.Where(r => states.Contains(r.Doc.DOC_STATE));
        }

        if (state is { } s)
        {
            return query.Where(r => r.Doc.DOC_STATE == s);
        }

        return query;
    }

    public async Task<PettyCashExpenseDocListResult> GetPagedAsync(
        int pageNumber,
        int pageSize,
        PettyCashExpenseDocFilter filter,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        var scopedQuery = ApplyFundAndSearch(BaseQuery(vahedCode), filter.FundId, filter.Search);

        // Computed with fundId/search applied but the state/states filter deliberately IGNORED,
        // so one request populates every کارتابل tab badge — see PettyCashDocStateCountDto.
        var stateCounts = await scopedQuery
            .GroupBy(r => r.Doc.DOC_STATE)
            .Select(g => new PettyCashDocStateCountDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        var filteredQuery = ApplyState(scopedQuery, filter.State, filter.States);

        var totalCount = await filteredQuery.CountAsync(cancellationToken);

        // Oldest-submitted-first: documents with no SUBMITTED_DATE (i.e. drafts) sort last, since
        // they are not yet in anyone's queue. ID is a pure tie-breaker for stable paging.
        var rows = await filteredQuery
            .OrderBy(r => r.Doc.SUBMITTED_DATE ?? DateTime.MaxValue)
            .ThenBy(r => r.Doc.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var items = rows.Select(r => ToListItemDto(r, now)).ToList();

        return new PettyCashExpenseDocListResult(
            new PagedResult<PettyCashExpenseDocListItemDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
            },
            stateCounts);
    }

    public async Task<PettyCashExpenseDocDto?> GetByIdAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        // The owning unit is read first as a scalar so the access decision can tell "no such row"
        // (null, caller gets null, 404) from "another unit's row" (403) — same pattern as every
        // other by-id read repository in this project.
        var ownerVahedCode = await _dbContext.TB_PC_EXPENSE_DOCs
            .AsNoTracking()
            .Where(d => d.ID == id)
            .Select(d => d.VAHEDCODE)
            .FirstOrDefaultAsync(cancellationToken);

        VahedOwnership.EnsureOwned(ownerVahedCode, vahedCode, id, "PettyCashExpenseDoc");

        var row = await (
            from doc in _dbContext.TB_PC_EXPENSE_DOCs.AsNoTracking()
            join head in _dbContext.TB_CHARGEANDCOST_HEADs.AsNoTracking() on doc.CHARGEANDCOSTHEAD_ID equals head.ID
            join fund in _dbContext.TB_REVOLVING_FUNDs.AsNoTracking() on doc.REVOLVINGFUND_ID equals fund.ID
            join detail in _dbContext.TB_CHARGEANDCOST_DETAILs.AsNoTracking()
                on (Guid?)doc.CHARGEANDCOSTHEAD_ID equals detail.CHARGEANDCOSTHEAD_ID into detailGroup
            from detail in detailGroup.DefaultIfEmpty()
            join expense in _dbContext.TB_EXPENCEs.AsNoTracking()
                on detail!.EXPENSE_ID equals (Guid?)expense.ID into expenseGroup
            from expense in expenseGroup.DefaultIfEmpty()
            where doc.ID == id
            select new Row(doc, head, fund, detail, expense))
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : ToDto(row, DateTime.UtcNow);
    }

    private static PettyCashExpenseDocListItemDto ToListItemDto(Row row, DateTime now) => new(
        row.Doc.ID,
        "TH-" + row.Head.CHARGEANDCOST_CODE,
        row.Head.CHARGEANDCOST_CODE,
        row.Head.CHARGEANDCOST_DATE,
        row.Doc.REVOLVINGFUND_ID,
        row.Fund.NAME,
        row.Detail?.EXPENSE_ID ?? Guid.Empty,
        row.Expense?.EXPENCENAME,
        row.Doc.VENDOR_NAME,
        row.Head.DESCRIPTION,
        (row.Doc.AMOUNT_BEFORE_TAX ?? 0m) + (row.Doc.VAT_AMOUNT ?? 0m),
        row.Doc.DOC_STATE,
        row.Doc.SUBMITTED_DATE,
        row.Doc.SUBMITTED_DATE is { } submitted ? (int)(now - submitted).TotalDays : null,
        row.Doc.ADDUSERID);

    private static PettyCashExpenseDocDto ToDto(Row row, DateTime now)
    {
        var listItem = ToListItemDto(row, now);

        return new PettyCashExpenseDocDto(
            listItem.Id,
            listItem.DocNumber,
            listItem.Code,
            listItem.RegisterDate,
            listItem.FundId,
            listItem.FundName,
            listItem.ExpenseId,
            listItem.ExpenseName,
            listItem.VendorName,
            listItem.Description,
            listItem.TotalAmount,
            listItem.State,
            listItem.SubmittedDate,
            listItem.AgeDays,
            listItem.AddUserId,
            row.Doc.VENDOR_NATIONAL_ID,
            row.Doc.INVOICE_NO,
            row.Doc.INVOICE_DATE,
            row.Doc.EVIDENCE_TYPE,
            row.Doc.AMOUNT_BEFORE_TAX,
            row.Doc.VAT_AMOUNT,
            row.Doc.RETURN_DEADLINE);
    }
}
