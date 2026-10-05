using Accounting.Application.BankCards;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>کارت حساب جاری — روی <c>TB_BANKCARTDETAIL</c>. رجوع <see cref="IBankCardRepository"/>.</summary>
public sealed class BankCardRepository : IBankCardRepository
{
    private readonly LegacyDbContext _dbContext;
    private readonly IBankAccountReadRepository _bankAccountReadRepository;

    public BankCardRepository(LegacyDbContext dbContext, IBankAccountReadRepository bankAccountReadRepository)
    {
        _dbContext = dbContext;
        _bankAccountReadRepository = bankAccountReadRepository;
    }

    public async Task<BankCardAccount?> GetAccountAsync(Guid bankAccountId, string vahedCode, CancellationToken cancellationToken = default)
    {
        var a = await _bankAccountReadRepository.GetByIdAsync(bankAccountId, vahedCode, cancellationToken);
        return a is null
            ? null
            : new BankCardAccount(
                a.Id, a.AccountNumber, a.AccountHolder, a.BankId, a.BranchId, a.AccountCodeId,
                a.TafsiliLinks.Select(l => l.TafsiliId).Distinct().ToList());
    }

    public async Task<IReadOnlyList<TB_BANKCARTDETAIL>> GetRowsAsync(
        string vahedCode, string year, string accountNumber, string? month, CancellationToken cancellationToken = default)
    {
        var q = _dbContext.TB_BANKCARTDETAILs
            .Where(r => r.ISDELETED != true && r.VAHEDCODE == vahedCode && r.YEAR == year && r.ACCOUNTNUMBER == accountNumber);
        if (month is not null)
            q = q.Where(r => r.MONTH == month);
        return await q.ToListAsync(cancellationToken);
    }

    public async Task<TB_BANKCARTDETAIL?> GetRowForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        var row = await _dbContext.TB_BANKCARTDETAILs
            .FirstOrDefaultAsync(r => r.ID == id && r.ISDELETED != true, cancellationToken);
        VahedOwnership.EnsureOwned(row?.VAHEDCODE, vahedCode, id, "BankCartDetail");
        return row;
    }

    public async Task AddRowAsync(TB_BANKCARTDETAIL row, CancellationToken cancellationToken = default)
        => await _dbContext.TB_BANKCARTDETAILs.AddAsync(row, cancellationToken);

    public async Task<IReadOnlyList<BankCardMatch>> FindChequesAsync(
        Guid bankAccountId, string vahedCode, string year, IReadOnlyCollection<string> numbers,
        CancellationToken cancellationToken = default)
    {
        // CHEQ_NO ممکن است با صفر پیشرو ذخیره شده باشد؛ مقایسهٔ نهایی بی صفر در Application است.
        // اینجا فقط محدود به چک‌های دسته‌چک همین حساب با ردیف سند بستانکار.
        var rows = await (
            from c in _dbContext.TB_CHECKs.AsNoTracking()
            join b in _dbContext.TB_CHECKBOOKs.AsNoTracking() on c.CHECKBOOK_ID equals b.ID
            join d in _dbContext.TB_VOUCHERSDETAILs.AsNoTracking() on (Guid?)c.ID equals d.CHECK_ID
            where !c.ISDELETED && !b.ISDELETED && b.ACCOUNT_ID == bankAccountId
                  && d.ISDELETED != true && d.VAHEDCODE == vahedCode && d.YEAR == year && d.CREDITOR > 0
            select new { c.ID, c.CHEQ_NO, Amount = d.CREDITOR ?? 0m })
            .ToListAsync(cancellationToken);

        var wanted = numbers.ToHashSet();
        return rows
            .Where(r => wanted.Contains(r.CHEQ_NO.Trim().TrimStart('0')))
            .Select(r => new BankCardMatch(r.ID, r.CHEQ_NO, r.Amount))
            .ToList();
    }

    public async Task<IReadOnlyList<BankCardMatch>> FindReceiptsAsync(
        Guid accountCodeId, string vahedCode, string year, IReadOnlyCollection<string> numbers,
        CancellationToken cancellationToken = default)
    {
        var rows = await (
            from r in _dbContext.TB_RECEIPs.AsNoTracking()
            join d in _dbContext.TB_VOUCHERSDETAILs.AsNoTracking() on (Guid?)r.ID equals d.RECEIP_ID
            where !r.ISDELETED && d.ISDELETED != true && d.VAHEDCODE == vahedCode && d.YEAR == year
                  && d.ACCOUNT_ID == accountCodeId && d.DEBTOR > 0
            select new { r.ID, r.RECEIPT_NO, Amount = d.DEBTOR ?? 0m })
            .ToListAsync(cancellationToken);

        var wanted = numbers.ToHashSet();
        return rows
            .Where(r => wanted.Contains(r.RECEIPT_NO.Trim().TrimStart('0')))
            .Select(r => new BankCardMatch(r.ID, r.RECEIPT_NO, r.Amount))
            .ToList();
    }

    public async Task SetCheckReceivedDateAsync(Guid checkId, string? date, CancellationToken cancellationToken = default)
    {
        var check = await _dbContext.TB_CHECKs.FirstOrDefaultAsync(c => c.ID == checkId, cancellationToken);
        if (check is not null)
            check.DATE_RSID = date;
    }

    public async Task SetReceiptReceivedDateAsync(Guid receiptId, string? date, CancellationToken cancellationToken = default)
    {
        var receipt = await _dbContext.TB_RECEIPs.FirstOrDefaultAsync(r => r.ID == receiptId, cancellationToken);
        if (receipt is not null)
            receipt.DATE_RSID = date;
    }

    public async Task<IReadOnlyList<BankCardBookItemDto>> GetOutstandingBookItemsAsync(
        Guid accountCodeId, IReadOnlyCollection<Guid> tafsiliIds, string vahedCode, string year, string toDate,
        CancellationToken cancellationToken = default)
    {
        var reconciledChecks = _dbContext.TB_BANKCARTDETAILs
            .Where(c => c.ISDELETED != true && c.VAHEDCODE == vahedCode && c.YEAR == year && c.CHECK_ID != null)
            .Select(c => c.CHECK_ID);
        var reconciledReceipts = _dbContext.TB_BANKCARTDETAILs
            .Where(c => c.ISDELETED != true && c.VAHEDCODE == vahedCode && c.YEAR == year && c.RECEIP_ID != null)
            .Select(c => c.RECEIP_ID);

        var q =
            from d in _dbContext.TB_VOUCHERSDETAILs.AsNoTracking()
            join h in _dbContext.TB_VOUCHERSHEADs.AsNoTracking() on d.VOUCHERSHEAD_ID equals h.ID
            where d.ISDELETED != true && h.ISDELETED != true
                  && d.ACCOUNT_ID == accountCodeId && h.VAHEDCODE == vahedCode && h.YEAR == year
                  && h.DATE_DOC != null && string.Compare(h.DATE_DOC, toDate) <= 0
                  && ((d.CHECK_ID != null && !reconciledChecks.Contains(d.CHECK_ID))
                      || (d.RECEIP_ID != null && !reconciledReceipts.Contains(d.RECEIP_ID)))
            select new { d, h };

        // همان قاعدهٔ تفصیلی موجودی بانک: فقط ردیف‌هایی که همهٔ تفصیلی‌های این حساب بانکی را دارند.
        var ids = tafsiliIds.Distinct().ToList();
        if (ids.Count > 0)
        {
            var matching =
                from link in _dbContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.AsNoTracking()
                where !link.ISDELETED && ids.Contains(link.TAFSILI_ID)
                group link by link.VOUCHERSDETAIL_ID into g
                where g.Select(l => l.TAFSILI_ID).Distinct().Count() == ids.Count
                select g.Key;
            q = q.Where(x => matching.Contains(x.d.ID));
        }

        var rows = await q
            .OrderBy(x => x.h.DATE_DOC)
            .ThenBy(x => x.h.DOC_NUM)
            .Select(x => new
            {
                x.d.ID,
                HeadId = x.h.ID,
                x.h.DOC_NUM,
                x.h.DATE_DOC,
                ChequeNo = _dbContext.TB_CHECKs.Where(c => c.ID == x.d.CHECK_ID).Select(c => c.CHEQ_NO).FirstOrDefault(),
                ReceiptNo = _dbContext.TB_RECEIPs.Where(r => r.ID == x.d.RECEIP_ID).Select(r => r.RECEIPT_NO).FirstOrDefault(),
                x.d.DESCRIPTION,
                Debit = x.d.DEBTOR ?? 0m,
                Credit = x.d.CREDITOR ?? 0m,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new BankCardBookItemDto(
                r.ID, r.HeadId, r.DOC_NUM, r.DATE_DOC, r.ChequeNo ?? r.ReceiptNo, r.DESCRIPTION, r.Debit, r.Credit))
            .ToList();
    }
}
