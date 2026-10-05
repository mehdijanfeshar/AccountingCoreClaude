using Accounting.Application.ChequeBook;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// دفتر چک — رجوع <see cref="IChequeBookRepository"/>. ⚠️ روی Oracle از <c>AnyAsync</c> استفاده نمی‌شود
/// (<c>ORA-00904</c>)؛ <c>CountAsync</c> جایگزین است.
/// </summary>
public sealed class ChequeBookRepository : IChequeBookRepository
{
    private readonly LegacyDbContext _dbContext;

    public ChequeBookRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TB_CHECK?> GetCheckForUpdateAsync(Guid checkId, string vahedCode, CancellationToken cancellationToken = default)
    {
        var cheque = await _dbContext.TB_CHECKs.Include(c => c.CHECKBOOK)
            .FirstOrDefaultAsync(c => c.ID == checkId && !c.ISDELETED, cancellationToken);
        VahedOwnership.EnsureOwned(cheque?.VAHEDCODE, vahedCode, checkId, "Check");
        return cheque;
    }

    public async Task<bool> IsUsedByOtherDetailAsync(Guid checkId, Guid? excludeDetailId, CancellationToken cancellationToken = default)
    {
        var q = ActiveDetails().Where(d => d.CHECK_ID == checkId);
        if (excludeDetailId is { } id)
            q = q.Where(d => d.ID != id);
        return await q.CountAsync(cancellationToken) > 0;
    }

    public Task<TB_VOUCHERSDETAIL?> GetActiveDetailAsync(Guid checkId, CancellationToken cancellationToken = default)
        => ActiveDetails().AsNoTracking().Where(d => d.CHECK_ID == checkId).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<AvailableChequeDto>> GetAvailableAsync(
        string vahedCode, Guid? accountCodeId, string? search, CancellationToken cancellationToken = default)
    {
        var used = ActiveDetails().Where(d => d.CHECK_ID != null).Select(d => d.CHECK_ID);
        var q =
            from c in _dbContext.TB_CHECKs.AsNoTracking()
            join b in _dbContext.TB_CHECKBOOKs.AsNoTracking() on c.CHECKBOOK_ID equals b.ID
            join a in _dbContext.TB_ACCOUNTs.AsNoTracking() on b.ACCOUNT_ID equals a.ID
            where !c.ISDELETED && !b.ISDELETED && a.ISDELETED != true
                  && c.VAHEDCODE == vahedCode
                  && b.CHECKBOOK_TYPE != CheckType.Sori
                  && c.EBTAL != CheckCancelStatus.Canceled
                  && !used.Contains(c.ID)
            select new { c, b, a };
        if (accountCodeId is { } acc)
            q = q.Where(x => x.a.ACCOUNTCODE_ID == acc);
        if (search is not null)
            q = q.Where(x => x.c.CHEQ_NO.Contains(search));

        return await q
            .OrderBy(x => x.a.ACCOUNTNUMBER)
            .ThenBy(x => x.c.CHEQ_NO)
            .Take(200)
            .Select(x => new AvailableChequeDto(
                x.c.ID, x.c.CHEQ_NO, x.b.CHECKBOOK_TITLE, x.a.ID, x.a.ACCOUNTNUMBER, x.a.BANK!.BANKNAME))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SoriChequeBookDto>> GetSoriBooksAsync(
        string vahedCode, Guid? accountCodeId, CancellationToken cancellationToken = default)
    {
        var q =
            from b in _dbContext.TB_CHECKBOOKs.AsNoTracking()
            join a in _dbContext.TB_ACCOUNTs.AsNoTracking() on b.ACCOUNT_ID equals a.ID
            where !b.ISDELETED && a.ISDELETED != true && b.VAHEDCODE == vahedCode && b.CHECKBOOK_TYPE == CheckType.Sori
            select new { b, a };
        if (accountCodeId is { } acc)
            q = q.Where(x => x.a.ACCOUNTCODE_ID == acc);

        var books = await q
            .OrderBy(x => x.a.ACCOUNTNUMBER).ThenByDescending(x => x.b.FROMCHECKNUMBER)
            .Take(100)
            .Select(x => new
            {
                x.b.ID, x.b.CHECKBOOK_TITLE, AccountId = x.a.ID, x.a.ACCOUNTNUMBER, BankName = x.a.BANK!.BANKNAME,
                x.b.FROMCHECKNUMBER, x.b.TOCHECKNUMBER,
            })
            .ToListAsync(cancellationToken);
        if (books.Count == 0)
            return [];

        var ids = books.Select(b => b.ID).ToList();
        var maxByBook = await _dbContext.TB_CHECKs.AsNoTracking()
            .Where(c => ids.Contains(c.CHECKBOOK_ID))
            .GroupBy(c => c.CHECKBOOK_ID)
            .Select(g => new { g.Key, Max = g.Max(c => c.CHEQ_NO) })
            .ToListAsync(cancellationToken);
        var max = maxByBook.ToDictionary(x => x.Key, x => (string?)x.Max);

        return books.Select(b => new SoriChequeBookDto(
                b.ID, b.CHECKBOOK_TITLE, b.AccountId, b.ACCOUNTNUMBER, b.BankName, b.FROMCHECKNUMBER, b.TOCHECKNUMBER,
                Accounting.Application.CheckBooks.CheckBookLeaves.NextSoriNumber(
                    new TB_CHECKBOOK { FROMCHECKNUMBER = b.FROMCHECKNUMBER, TOCHECKNUMBER = b.TOCHECKNUMBER },
                    max.GetValueOrDefault(b.ID))))
            .ToList();
    }

    public async Task<PagedResult<ChequeBookItemDto>> GetPagedAsync(ChequeBookFilter f, CancellationToken cancellationToken = default)
    {
        var q =
            from d in ActiveDetails().AsNoTracking()
            join h in _dbContext.TB_VOUCHERSHEADs.AsNoTracking() on d.VOUCHERSHEAD_ID equals h.ID
            join c in _dbContext.TB_CHECKs.AsNoTracking() on d.CHECK_ID equals c.ID
            join b in _dbContext.TB_CHECKBOOKs.AsNoTracking() on c.CHECKBOOK_ID equals b.ID
            join a in _dbContext.TB_ACCOUNTs.AsNoTracking() on b.ACCOUNT_ID equals a.ID
            join ap0 in _dbContext.TB_CHECK_APPROVALs.AsNoTracking().Where(x => !x.ISDELETED) on c.ID equals ap0.CHECK_ID into apj
            from ap in apj.DefaultIfEmpty()
            where h.ISDELETED != true && d.VAHEDCODE == f.VahedCode && d.YEAR == f.Year && !c.ISDELETED
            select new { d, h, c, b, a, ap };

        if (f.BankAccountId is { } accountId)
            q = q.Where(x => x.a.ID == accountId);
        if (f.FromDate is not null)
            q = q.Where(x => string.Compare(x.c.CHEQ_DATE, f.FromDate) >= 0);
        if (f.ToDate is not null)
            q = q.Where(x => string.Compare(x.c.CHEQ_DATE, f.ToDate) <= 0);
        if (f.Canceled is { } canceled)
            q = canceled ? q.Where(x => x.c.EBTAL == CheckCancelStatus.Canceled) : q.Where(x => x.c.EBTAL != CheckCancelStatus.Canceled);
        if (f.Printed is { } printed)
            q = printed ? q.Where(x => x.c.PRINT == CheckPrintStatus.Printed) : q.Where(x => x.c.PRINT != CheckPrintStatus.Printed);
        if (f.ChequeNo is not null)
            q = q.Where(x => x.c.CHEQ_NO == f.ChequeNo);
        if (f.Amount is { } amount)
            q = q.Where(x => x.d.CREDITOR == amount);
        if (f.Description is not null)
            q = q.Where(x => (x.c.PAPER_DESC != null && x.c.PAPER_DESC.Contains(f.Description))
                || (x.d.DESCRIPTION != null && x.d.DESCRIPTION.Contains(f.Description))
                || (x.c.PAYTO != null && x.c.PAYTO.Contains(f.Description)));
        if (f.OnlyUnissued)
            q = q.Where(x => x.ap == null || x.ap.STATE == ChequeApprovalState.Returned);
        else if (f.ApprovalState is { } state)
            q = q.Where(x => x.ap != null && x.ap.STATE == state);

        var total = await q.CountAsync(cancellationToken);
        // ⚠️ هیچ مقایسهٔ bool در projection نیست: Oracle «CASE … THEN True» را نمی‌فهمد (ORA-00904 "FALSE").
        // مقدار خام enumها خوانده و در حافظه به bool تبدیل می‌شود.
        var rows = await q
            .OrderByDescending(x => x.c.CHEQ_DATE)
            .ThenByDescending(x => x.c.CHEQ_NO)
            .Skip((f.PageNumber - 1) * f.PageSize)
            .Take(f.PageSize)
            .Select(x => new
            {
                CheckId = x.c.ID,
                DetailId = x.d.ID,
                HeadId = x.h.ID,
                x.c.CHEQ_NO,
                x.c.CHEQ_DATE,
                x.c.PAYTO,
                x.c.PAPER_DESC,
                LineDesc = x.d.DESCRIPTION,
                Amount = x.d.CREDITOR ?? 0m,
                x.a.ACCOUNTNUMBER,
                BankName = x.a.BANK!.BANKNAME,
                x.b.CHECKBOOK_TITLE,
                x.h.DOC_NUM,
                x.h.DATE_DOC,
                x.h.DOCLIFE,
                x.c.EBTAL,
                x.c.PRINT,
                State = x.ap == null ? (ChequeApprovalState?)null : x.ap.STATE,
                PreparedBy = x.ap == null ? null : x.ap.PREPARED_BY,
                AccountingBy = x.ap == null ? null : x.ap.ACCOUNTING_BY,
                ManagerBy = x.ap == null ? null : x.ap.MANAGER_BY,
                Note = x.ap == null ? null : x.ap.NOTE,
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(r => new ChequeBookItemDto(
                r.CheckId, r.DetailId, r.HeadId, r.CHEQ_NO, r.CHEQ_DATE, r.PAYTO, r.PAPER_DESC, r.LineDesc, r.Amount,
                r.ACCOUNTNUMBER, r.BankName, r.CHECKBOOK_TITLE, r.DOC_NUM, r.DATE_DOC,
                r.DOCLIFE == null ? null : (int)r.DOCLIFE.Value,
                r.EBTAL == CheckCancelStatus.Canceled,
                r.PRINT == CheckPrintStatus.Printed,
                r.State, r.PreparedBy, r.AccountingBy, r.ManagerBy, r.Note))
            .ToList();

        return new PagedResult<ChequeBookItemDto>
        {
            Items = items,
            PageNumber = f.PageNumber,
            PageSize = f.PageSize,
            TotalCount = total,
        };
    }

    public Task<TB_CHECK_APPROVAL?> GetApprovalForUpdateAsync(Guid checkId, CancellationToken cancellationToken = default)
        => _dbContext.TB_CHECK_APPROVALs.FirstOrDefaultAsync(a => a.CHECK_ID == checkId, cancellationToken);

    public async Task AddApprovalAsync(TB_CHECK_APPROVAL approval, CancellationToken cancellationToken = default)
        => await _dbContext.TB_CHECK_APPROVALs.AddAsync(approval, cancellationToken);

    public async Task AddApprovalEventAsync(TB_CHECK_APPROVAL_EVENT approvalEvent, CancellationToken cancellationToken = default)
        => await _dbContext.TB_CHECK_APPROVAL_EVENTs.AddAsync(approvalEvent, cancellationToken);

    public async Task<IReadOnlyList<ChequeApprovalEventDto>> GetEventsAsync(Guid checkId, CancellationToken cancellationToken = default)
        => await (
            from e in _dbContext.TB_CHECK_APPROVAL_EVENTs.AsNoTracking()
            join a in _dbContext.TB_CHECK_APPROVALs.AsNoTracking() on e.APPROVAL_ID equals a.ID
            where a.CHECK_ID == checkId
            orderby e.CREATEDDATE
            select new ChequeApprovalEventDto(e.ACTION, e.FROM_STATE, e.TO_STATE, e.USERID, e.NOTE, e.CREATEDDATE))
            .ToListAsync(cancellationToken);

    public async Task<ChequePrintDto?> GetPrintDataAsync(Guid checkId, CancellationToken cancellationToken = default)
    {
        var row = await (
            from d in ActiveDetails().AsNoTracking()
            join c in _dbContext.TB_CHECKs.AsNoTracking() on d.CHECK_ID equals c.ID
            join b in _dbContext.TB_CHECKBOOKs.AsNoTracking() on c.CHECKBOOK_ID equals b.ID
            join a in _dbContext.TB_ACCOUNTs.AsNoTracking() on b.ACCOUNT_ID equals a.ID
            where c.ID == checkId
            select new
            {
                c.ID,
                c.CHEQ_NO,
                c.CHEQ_DATE,
                c.PAYTO,
                c.PAPER_DESC,
                Amount = d.CREDITOR ?? 0m,
                a.ACCOUNTNUMBER,
                BankName = a.BANK!.BANKNAME,
                b.CHECKTYPE_ID,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null)
            return null;

        var type = row.CHECKTYPE_ID is { } typeId
            ? await _dbContext.TB_CHECK_TYPEs.AsNoTracking().FirstOrDefaultAsync(t => t.ID == typeId, cancellationToken)
            : null;

        return new ChequePrintDto(
            row.ID, row.CHEQ_NO, row.CHEQ_DATE, row.PAYTO, row.PAPER_DESC, row.Amount, row.ACCOUNTNUMBER, row.BankName,
            type?.ID, type?.CHEQUE_TYPE_TITLE, type?.CHEQUE_WIDTH, type?.CHEQUE_HEIGHT,
            type?.PRINTER_MARGINE_TOP, type?.PRINTER_MARGINE_LEFT, type?.CHEQUE_IMAGE);
    }

    public async Task<IReadOnlyList<ChequeLeafDto>> GetLeavesAsync(Guid checkBookId, CancellationToken cancellationToken = default)
    {
        // مقادیر خام enum خوانده و در حافظه به bool تبدیل می‌شوند (Oracle: هیچ مقایسهٔ bool در projection).
        var rows = await (
            from c in _dbContext.TB_CHECKs.AsNoTracking()
            where c.CHECKBOOK_ID == checkBookId && !c.ISDELETED
            join d0 in ActiveDetails().AsNoTracking() on (Guid?)c.ID equals d0.CHECK_ID into dj
            from d in dj.DefaultIfEmpty()
            join ap0 in _dbContext.TB_CHECK_APPROVALs.AsNoTracking().Where(a => !a.ISDELETED) on c.ID equals ap0.CHECK_ID into apj
            from ap in apj.DefaultIfEmpty()
            orderby c.CHEQ_NO
            select new
            {
                c.ID,
                c.CHEQ_NO,
                c.CHEQ_DATE,
                c.PAYTO,
                c.PAPER_DESC,
                c.EBTAL,
                c.PRINT,
                HeadId = d == null ? null : d.VOUCHERSHEAD_ID,
                DocNum = d == null ? null : d.VOUCHERSHEAD!.DOC_NUM,
                DateDoc = d == null ? null : d.VOUCHERSHEAD!.DATE_DOC,
                Amount = d == null ? null : d.CREDITOR,
                State = ap == null ? (ChequeApprovalState?)null : ap.STATE,
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.ID)
            .Select(g => g.First())
            .Select(r => new ChequeLeafDto(
                r.ID, r.CHEQ_NO, r.CHEQ_DATE, r.PAYTO, r.PAPER_DESC,
                r.EBTAL == CheckCancelStatus.Canceled, r.PRINT == CheckPrintStatus.Printed,
                r.HeadId, r.DocNum, r.DateDoc, r.Amount, r.State))
            .ToList();
    }

    private IQueryable<TB_VOUCHERSDETAIL> ActiveDetails()
        => _dbContext.TB_VOUCHERSDETAILs.Where(d => d.ISDELETED != true && d.VOUCHERSHEAD!.ISDELETED != true);
}
