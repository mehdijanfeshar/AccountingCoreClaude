using Accounting.Application.Common.Interfaces;
using Accounting.Application.Queries.Treasury;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>See <see cref="IBankStatementBookCandidateReadRepository"/> XML doc.</summary>
public sealed class BankStatementBookCandidateReadRepository : IBankStatementBookCandidateReadRepository
{
    // اوراکل بیش از ۱۰۰۰ عنصر در IN را نمی‌پذیرد (ORA-01795)؛ لیست‌های بزرگ دسته‌دسته پرسیده می‌شوند.
    private const int InListChunk = 900;

    private static async Task<List<T>> ChunkedAsync<TId, T>(IReadOnlyCollection<TId> ids, Func<List<TId>, Task<List<T>>> run)
    {
        var result = new List<T>();
        foreach (var chunk in ids.Chunk(InListChunk))
        {
            result.AddRange(await run(chunk.ToList()));
        }

        return result;
    }

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

        foreach (var excludeChunk in excludeIds.Chunk(InListChunk))
        {
            var part = excludeChunk;
            query = query.Where(x => !part.Contains(x.detail.ID));
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
                x.detail.CHECK_ID,
                x.detail.RECEIP_ID,
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
        var refFromPaymentVoucher = await ChunkedAsync(headIds, ids => _dbContext.TB_TR_PAYMENT_REQUESTs.AsNoTracking()
            .Where(p => !p.ISDELETED && p.BANK_REFERENCE != null
                        && p.PAYMENT_VOUCHER_ID != null && ids.Contains(p.PAYMENT_VOUCHER_ID.Value))
            .Select(p => new { HeadId = p.PAYMENT_VOUCHER_ID, p.BANK_REFERENCE })
            .ToListAsync(cancellationToken));

        var refFromLiabilityVoucher = await ChunkedAsync(headIds, ids => _dbContext.TB_TR_PAYMENT_REQUESTs.AsNoTracking()
            .Where(p => !p.ISDELETED && p.BANK_REFERENCE != null
                        && p.LIABILITY_VOUCHER_ID != null && ids.Contains(p.LIABILITY_VOUCHER_ID.Value))
            .Select(p => new { HeadId = p.LIABILITY_VOUCHER_ID, p.BANK_REFERENCE })
            .ToListAsync(cancellationToken));

        var refFromPaymentRequest = refFromPaymentVoucher.Concat(refFromLiabilityVoucher).ToList();

        var refFromReceipt = await ChunkedAsync(headIds, ids => _dbContext.TB_TR_RECEIPTs.AsNoTracking()
            .Where(r => !r.ISDELETED && r.VOUCHER_ID != null && ids.Contains(r.VOUCHER_ID.Value))
            .Select(r => new { HeadId = r.VOUCHER_ID, BANK_REFERENCE = (string?)r.BANK_REFERENCE })
            .ToListAsync(cancellationToken));

        var refFromTransfer = await ChunkedAsync(headIds, ids => _dbContext.TB_TR_TRANSFERs.AsNoTracking()
            .Where(t => !t.ISDELETED && t.VOUCHER_ID != null && ids.Contains(t.VOUCHER_ID.Value))
            .Select(t => new { HeadId = t.VOUCHER_ID, t.BANK_REFERENCE })
            .ToListAsync(cancellationToken));

        var bankRefByHeadId = new Dictionary<Guid, string?>();

        foreach (var x in refFromPaymentRequest.Concat(refFromReceipt).Concat(refFromTransfer))
        {
            if (x.HeadId is { } id && !bankRefByHeadId.ContainsKey(id))
            {
                bankRefByHeadId[id] = x.BANK_REFERENCE;
            }
        }

        // شمارهٔ چک/فیش خود ردیف سند (CHECK_ID/RECEIP_ID) — برای تطبیق با شمارهٔ چک/فیش دیسکت بانک،
        // همان کلید تطبیق سیستم قدیم (ReconciliationCommandHandler).
        var checkIds = rows.Where(r => r.CHECK_ID != null).Select(r => r.CHECK_ID!.Value).Distinct().ToList();
        var receiptIds = rows.Where(r => r.RECEIP_ID != null).Select(r => r.RECEIP_ID!.Value).Distinct().ToList();
        var chequeNoById = checkIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await ChunkedAsync(checkIds, ids => _dbContext.TB_CHECKs.AsNoTracking()
                .Where(c => ids.Contains(c.ID)).Select(c => new { c.ID, c.CHEQ_NO }).ToListAsync(cancellationToken)))
                .ToDictionary(c => c.ID, c => c.CHEQ_NO);
        var receiptNoById = receiptIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await ChunkedAsync(receiptIds, ids => _dbContext.TB_RECEIPs.AsNoTracking()
                .Where(c => ids.Contains(c.ID)).Select(c => new { c.ID, c.RECEIPT_NO }).ToListAsync(cancellationToken)))
                .ToDictionary(c => c.ID, c => c.RECEIPT_NO);

        return rows.Select(r => new BankStatementBookLineDto(
            r.ID,
            r.HeadId,
            r.DOC_NUM,
            r.DATE_DOC ?? string.Empty,
            r.Debit,
            r.Credit,
            r.DESCRIPTION,
            bankRefByHeadId.TryGetValue(r.HeadId, out var bankRef) ? bankRef : null,
            r.CHECK_ID is { } cid && chequeNoById.TryGetValue(cid, out var cno) ? cno
                : r.RECEIP_ID is { } rid && receiptNoById.TryGetValue(rid, out var rno) ? rno : null))
            .ToList();
    }
}
