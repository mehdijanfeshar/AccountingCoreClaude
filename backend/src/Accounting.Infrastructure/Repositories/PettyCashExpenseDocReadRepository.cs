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

    /// <summary>
    /// Filtering, state counts and paging all run against this bare <c>TB_PC_EXPENSE_DOC</c>
    /// (+ <c>TB_CHARGEANDCOST_HEAD</c>, only for <paramref name="search"/>) shape — never against
    /// the full 5-table <see cref="Row"/> projection. EF Core's Oracle provider cannot translate a
    /// <c>Where</c>/<c>GroupBy</c> predicate that reaches through a <c>new Row(...)</c> record
    /// constructed from a chain including two <c>LeftJoin</c>/<c>DefaultIfEmpty</c> pairs
    /// ("The LINQ expression ... could not be translated" — hit against live Oracle only, since
    /// this path was never exercised in SQLite tests). Filtering, counting and ordering only ever
    /// need columns off <c>TB_PC_EXPENSE_DOC</c>/<c>TB_CHARGEANDCOST_HEAD</c> directly, so this
    /// leaner shape sidesteps the issue entirely; <see cref="GetPagedAsync"/> hydrates the full
    /// <see cref="Row"/> shape afterwards, only for the already-page-bounded set of ids.
    /// </summary>
    private IQueryable<TB_PC_EXPENSE_DOC> FilteredDocsQuery(string vahedCode, Guid? fundId, string? search)
    {
        var docs = _dbContext.TB_PC_EXPENSE_DOCs.AsNoTracking()
            .Where(d => !d.ISDELETED && d.VAHEDCODE == vahedCode);

        if (fundId is { } id)
        {
            docs = docs.Where(d => d.REVOLVINGFUND_ID == id);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            docs =
                from doc in docs
                join head in _dbContext.TB_CHARGEANDCOST_HEADs.AsNoTracking() on doc.CHARGEANDCOSTHEAD_ID equals head.ID
                where (head.CHARGEANDCOST_CODE != null && head.CHARGEANDCOST_CODE.Contains(term)) ||
                      (doc.VENDOR_NAME != null && doc.VENDOR_NAME.Contains(term)) ||
                      (doc.INVOICE_NO != null && doc.INVOICE_NO.Contains(term)) ||
                      (head.DESCRIPTION != null && head.DESCRIPTION.Contains(term))
                select doc;
        }

        return docs;
    }

    private static IQueryable<TB_PC_EXPENSE_DOC> ApplyState(
        IQueryable<TB_PC_EXPENSE_DOC> docs, PettyCashDocState? state, IReadOnlyList<PettyCashDocState>? states)
    {
        // States takes precedence over State when non-empty — see PettyCashExpenseDocFilter XML doc.
        if (states is { Count: > 0 })
        {
            return docs.Where(d => states.Contains(d.DOC_STATE));
        }

        if (state is { } s)
        {
            return docs.Where(d => d.DOC_STATE == s);
        }

        return docs;
    }

    /// <summary>Hydrates the full display <see cref="Row"/> shape for exactly these ids (a
    /// bounded, already-paged set), preserving <paramref name="orderedIds"/>'s order since an
    /// <c>IN</c>-style filter gives no ordering guarantee of its own.</summary>
    private async Task<List<Row>> HydrateRowsAsync(IReadOnlyList<Guid> orderedIds, CancellationToken cancellationToken)
    {
        if (orderedIds.Count == 0)
        {
            return [];
        }

        var rows = await (
            from doc in _dbContext.TB_PC_EXPENSE_DOCs.AsNoTracking()
            where orderedIds.Contains(doc.ID)
            join head in _dbContext.TB_CHARGEANDCOST_HEADs.AsNoTracking() on doc.CHARGEANDCOSTHEAD_ID equals head.ID
            join fund in _dbContext.TB_REVOLVING_FUNDs.AsNoTracking() on doc.REVOLVINGFUND_ID equals fund.ID
            join detail in _dbContext.TB_CHARGEANDCOST_DETAILs.AsNoTracking()
                on (Guid?)doc.CHARGEANDCOSTHEAD_ID equals detail.CHARGEANDCOSTHEAD_ID into detailGroup
            from detail in detailGroup.DefaultIfEmpty()
            join expense in _dbContext.TB_EXPENCEs.AsNoTracking()
                on detail!.EXPENSE_ID equals (Guid?)expense.ID into expenseGroup
            from expense in expenseGroup.DefaultIfEmpty()
            select new Row(doc, head, fund, detail, expense))
            .ToListAsync(cancellationToken);

        var rowsById = rows.ToDictionary(r => r.Doc.ID);
        return orderedIds.Select(id => rowsById[id]).ToList();
    }

    public async Task<PettyCashExpenseDocListResult> GetPagedAsync(
        int pageNumber,
        int pageSize,
        PettyCashExpenseDocFilter filter,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        var scopedDocs = FilteredDocsQuery(vahedCode, filter.FundId, filter.Search);

        // Computed with fundId/search applied but the state/states filter deliberately IGNORED,
        // so one request populates every کارتابل tab badge — see PettyCashDocStateCountDto.
        var stateCounts = await scopedDocs
            .GroupBy(d => d.DOC_STATE)
            .Select(g => new PettyCashDocStateCountDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        var filteredDocs = ApplyState(scopedDocs, filter.State, filter.States);

        var totalCount = await filteredDocs.CountAsync(cancellationToken);

        // Oldest-submitted-first: documents with no SUBMITTED_DATE (i.e. drafts) sort last, since
        // they are not yet in anyone's queue. ID is a pure tie-breaker for stable paging.
        var pageIds = await filteredDocs
            .OrderBy(d => d.SUBMITTED_DATE ?? DateTime.MaxValue)
            .ThenBy(d => d.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(d => d.ID)
            .ToListAsync(cancellationToken);

        var rows = await HydrateRowsAsync(pageIds, cancellationToken);

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
