using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Queries;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>See <see cref="IBankStatementBookCandidateReadRepository"/> XML doc.</summary>
public sealed class BankStatementBookCandidateReadRepository : IBankStatementBookCandidateReadRepository
{
    private readonly LegacyDbContext _dbContext;

    public BankStatementBookCandidateReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<BankStatementBookLineDto>> GetCandidatesAsync(
        Guid accountCodeId,
        IReadOnlyCollection<Guid> bankTafsiliIds,
        bool debitSide,
        string fromDate,
        string toDate,
        string vahedCode,
        IReadOnlyCollection<Guid> excludeVoucherDetailIds,
        CancellationToken cancellationToken = default)
    {
        var tafsiliIds = bankTafsiliIds.Distinct().ToList();
        var excludeIds = excludeVoucherDetailIds.Distinct().ToList();

        var query =
            from detail in _dbContext.TB_VOUCHERSDETAILs.AsNoTracking()
            join head in _dbContext.TB_VOUCHERSHEADs.AsNoTracking() on detail.VOUCHERSHEAD_ID equals head.ID
            where detail.ISDELETED != true
                  && head.ISDELETED != true
                  && detail.ACCOUNT_ID == accountCodeId
                  && detail.VAHEDCODE == vahedCode
                  && head.DATE_DOC != null
                  && string.Compare(head.DATE_DOC, fromDate) >= 0
                  && string.Compare(head.DATE_DOC, toDate) <= 0
                  && (debitSide ? detail.DEBTOR > 0 : detail.CREDITOR > 0)
            select new { detail, head };

        if (excludeIds.Count > 0)
        {
            query = query.Where(x => !excludeIds.Contains(x.detail.ID));
        }

        // همان قاعدهٔ محدودسازی تفصیلی TreasuryBankAccountBalanceReadRepository — چند حساب بانکی
        // معمولاً یک معین مشترک دارند؛ فقط ردیف‌هایی که همهٔ تفصیلی‌های این حساب بانکی را دارند.
        if (tafsiliIds.Count > 0)
        {
            var matchingDetailIds =
                from link in _dbContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.AsNoTracking()
                where !link.ISDELETED && tafsiliIds.Contains(link.TAFSILI_ID)
                group link by link.VOUCHERSDETAIL_ID into g
                where g.Select(l => l.TAFSILI_ID).Distinct().Count() == tafsiliIds.Count
                select g.Key;

            query = query.Where(x => matchingDetailIds.Contains(x.detail.ID));
        }

        var rows = await query
            .OrderBy(x => x.head.DATE_DOC)
            .ThenBy(x => x.head.DOC_NUM)
            .Select(x => new
            {
                x.detail.ID,
                HeadId = x.head.ID,
                x.head.DOC_NUM,
                x.head.DATE_DOC,
                Debit = x.detail.DEBTOR ?? 0m,
                Credit = x.detail.CREDITOR ?? 0m,
                x.detail.DESCRIPTION,
            })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return Array.Empty<BankStatementBookLineDto>();
        }

        var headIds = rows.Select(r => r.HeadId).Distinct().ToList();

        // اولویت (۱) auto-match — مرجع بانکی سند صاحب این ردیف را، اگر توسط یکی از سه جریان
        // خزانه‌داری (درخواست پرداخت/دریافت/انتقال) صادر شده باشد، پیدا می‌کند. سه کوئری مستقل،
        // batched (نه N+1 به‌ازای ردیف) — هرگز join سراسری روی جدول‌های دو ماژول.
        var refFromPaymentVoucher = await _dbContext.TB_TR_PAYMENT_REQUESTs.AsNoTracking()
            .Where(p => !p.ISDELETED && p.BANK_REFERENCE != null
                        && p.PAYMENT_VOUCHER_ID != null && headIds.Contains(p.PAYMENT_VOUCHER_ID.Value))
            .Select(p => new { HeadId = p.PAYMENT_VOUCHER_ID, p.BANK_REFERENCE })
            .ToListAsync(cancellationToken);

        var refFromLiabilityVoucher = await _dbContext.TB_TR_PAYMENT_REQUESTs.AsNoTracking()
            .Where(p => !p.ISDELETED && p.BANK_REFERENCE != null
                        && p.LIABILITY_VOUCHER_ID != null && headIds.Contains(p.LIABILITY_VOUCHER_ID.Value))
            .Select(p => new { HeadId = p.LIABILITY_VOUCHER_ID, p.BANK_REFERENCE })
            .ToListAsync(cancellationToken);

        var refFromPaymentRequest = refFromPaymentVoucher.Concat(refFromLiabilityVoucher).ToList();

        var refFromReceipt = await _dbContext.TB_TR_RECEIPTs.AsNoTracking()
            .Where(r => !r.ISDELETED && r.VOUCHER_ID != null && headIds.Contains(r.VOUCHER_ID.Value))
            .Select(r => new { HeadId = r.VOUCHER_ID, BANK_REFERENCE = (string?)r.BANK_REFERENCE })
            .ToListAsync(cancellationToken);

        var refFromTransfer = await _dbContext.TB_TR_TRANSFERs.AsNoTracking()
            .Where(t => !t.ISDELETED && t.VOUCHER_ID != null && headIds.Contains(t.VOUCHER_ID.Value))
            .Select(t => new { HeadId = t.VOUCHER_ID, t.BANK_REFERENCE })
            .ToListAsync(cancellationToken);

        var bankRefByHeadId = new Dictionary<Guid, string?>();

        foreach (var x in refFromPaymentRequest.Concat(refFromReceipt).Concat(refFromTransfer))
        {
            if (x.HeadId is { } id && !bankRefByHeadId.ContainsKey(id))
            {
                bankRefByHeadId[id] = x.BANK_REFERENCE;
            }
        }

        return rows.Select(r => new BankStatementBookLineDto(
            r.ID,
            r.HeadId,
            r.DOC_NUM,
            r.DATE_DOC ?? string.Empty,
            r.Debit,
            r.Credit,
            r.DESCRIPTION,
            bankRefByHeadId.TryGetValue(r.HeadId, out var bankRef) ? bankRef : null))
            .ToList();
    }
}
