using Accounting.Application.Vouchers.Queries.LineExtras;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// پیاده‌سازی <see cref="IVoucherLineDisplayReader"/> — چند کوئری ساده (یکی برای هر نوع اطلاعات) روی
/// همهٔ ردیف‌های سند با هم، نه یکی به‌ازای هر ردیف. پروژکشن بولی ندارد (اوراکل ORA-00904 "FALSE")؛
/// «صوری بودن» چک در حافظه از نوع دسته‌چک حساب می‌شود.
/// </summary>
public sealed class VoucherLineDisplayReader : IVoucherLineDisplayReader
{
    private readonly LegacyDbContext _db;
    public VoucherLineDisplayReader(LegacyDbContext db) => _db = db;

    public async Task<IReadOnlyList<VoucherLineDisplayDto>> GetAsync(Guid voucherHeadId, string vahedCode, CancellationToken ct)
    {
        var lines = await _db.TB_VOUCHERSDETAILs.AsNoTracking()
            .Where(d => d.VOUCHERSHEAD_ID == voucherHeadId && d.VAHEDCODE == vahedCode && d.ISDELETED != true)
            .Select(d => new { d.ID, d.RECEIP_ID, d.CHECK_ID })
            .ToListAsync(ct);
        if (lines.Count == 0)
            return [];
        var ids = lines.Select(l => l.ID).ToList();

        // ── تفصیلی‌ها ──
        var links = await _db.TB_VOUCHERDETAIL_LINK_TAFSILIs.AsNoTracking()
            .Where(l => ids.Contains(l.VOUCHERSDETAIL_ID) && l.ISDELETED != true)
            .Select(l => new { l.VOUCHERSDETAIL_ID, l.TAFSILI_ID, l.LEVEL_ID })
            .ToListAsync(ct);
        var tafsiliIds = links.Select(l => l.TAFSILI_ID).Distinct().ToList();
        var levelIds = links.Select(l => l.LEVEL_ID).Distinct().ToList();
        var tafsilis = tafsiliIds.Count == 0
            ? []
            : await _db.TB_TAFSILIs.AsNoTracking()
                .Where(t => tafsiliIds.Contains(t.ID))
                .Select(t => new { t.ID, t.TAFSILI_CODE, t.TAFSILI_NAME })
                .ToListAsync(ct);
        var levels = levelIds.Count == 0
            ? []
            : await _db.TB_LEVEL_TAFSILs.AsNoTracking()
                .Where(l => levelIds.Contains(l.ID))
                .Select(l => new { l.ID, l.LEVEL_CODE, l.LEVEL_NAME })
                .ToListAsync(ct);

        // ── شناسهٔ حساب شناسه‌دار ──
        var attributes = await _db.TB_ATTRIBSINVOUCHERs.AsNoTracking()
            .Where(a => ids.Contains(a.VOUCHERSDETAIL_ID) && a.ISDELETED != true)
            .Select(a => new
            {
                a.VOUCHERSDETAIL_ID,
                a.ATTRIBFORACCOUNTCODE.ATTRIBBOXNO,
                AccCode = a.ATTRIBFORACCOUNTCODE.ACCOUNTCODE.ACCCODE,
                a.ATTRIBUTEVALUE,
            })
            .ToListAsync(ct);

        // ── ویژگی ──
        var identityDetails = await _db.TB_IDENTITYDETAILs.AsNoTracking()
            .Where(d => ids.Contains(d.VOUCHERSDETAIL_ID) && d.ISDELETED != true)
            .Select(d => new
            {
                d.VOUCHERSDETAIL_ID,
                d.IDENTITYHEAD_ID,
                GroupTitle = d.IDENTITYHEAD.IDENTITYGROUPS.IDENTITYGROUPS_DESC,
                SubTitle = d.IDENTITYSUBGRPS.SUBGRPS_DESC,
                SubCode = d.IDENTITYSUBGRPS.IDENTYSUBGROUPS_CODE,
                d.DETAIL_VALUE,
            })
            .ToListAsync(ct);
        var headIds = identityDetails.Select(d => d.IDENTITYHEAD_ID).Distinct().ToList();
        var fixedItems = headIds.Count == 0
            ? []
            : await _db.TB_IDENTITYFIXITEMs.AsNoTracking()
                .Where(f => headIds.Contains(f.IDENTITYHEAD_ID) && f.ISDELETED != true)
                .Select(f => new
                {
                    f.IDENTITYHEAD_ID,
                    SubTitle = f.IDENTITYSUBGRPS.SUBGRPS_DESC,
                    SubCode = f.IDENTITYSUBGRPS.IDENTYSUBGROUPS_CODE,
                    f.FIXITEMS_VALUE,
                })
                .ToListAsync(ct);

        // ── فیش/حواله و چک ──
        var receiptIds = lines.Where(l => l.RECEIP_ID != null).Select(l => l.RECEIP_ID!.Value).Distinct().ToList();
        var receipts = receiptIds.Count == 0
            ? []
            : await _db.TB_RECEIPs.AsNoTracking()
                .Where(r => receiptIds.Contains(r.ID))
                .Select(r => new { r.ID, r.RECEIPT_KIND, r.RECEIPT_NO, r.RECEIPT_DATE })
                .ToListAsync(ct);
        var checkIds = lines.Where(l => l.CHECK_ID != null).Select(l => l.CHECK_ID!.Value).Distinct().ToList();
        var checks = checkIds.Count == 0
            ? []
            : await _db.TB_CHECKs.AsNoTracking()
                .Where(c => checkIds.Contains(c.ID))
                .Select(c => new { c.ID, c.CHEQ_NO, c.CHEQ_DATE, c.PAYTO, c.CHECKBOOK.CHECKBOOK_TYPE, c.CHECKBOOK.CHECKBOOK_TITLE })
                .ToListAsync(ct);

        return lines.Select(line =>
        {
            var lineTafsilis = links
                .Where(l => l.VOUCHERSDETAIL_ID == line.ID)
                .Select(l => new
                {
                    Level = levels.FirstOrDefault(v => v.ID == l.LEVEL_ID),
                    Tafsili = tafsilis.FirstOrDefault(t => t.ID == l.TAFSILI_ID),
                })
                .OrderBy(x => x.Level?.LEVEL_CODE)
                .Select(x => new VoucherLineTafsiliDisplay(x.Level?.LEVEL_NAME, x.Tafsili?.TAFSILI_CODE, x.Tafsili?.TAFSILI_NAME))
                .ToList();

            var lineAttributes = attributes
                .Where(a => a.VOUCHERSDETAIL_ID == line.ID)
                .OrderBy(a => a.ATTRIBBOXNO)
                .Select(a => new VoucherLineLabeledValue($"شناسهٔ {a.ATTRIBBOXNO} حساب {a.AccCode}", a.ATTRIBUTEVALUE))
                .ToList();

            var lineIdentities = identityDetails
                .Where(d => d.VOUCHERSDETAIL_ID == line.ID)
                .GroupBy(d => new { d.IDENTITYHEAD_ID, d.GroupTitle })
                .Select(g => new VoucherLineIdentityDisplay(
                    g.Key.GroupTitle,
                    fixedItems
                        .Where(f => f.IDENTITYHEAD_ID == g.Key.IDENTITYHEAD_ID)
                        .OrderBy(f => f.SubCode)
                        .Select(f => new VoucherLineLabeledValue(f.SubTitle, f.FIXITEMS_VALUE))
                        .ToList(),
                    g.OrderBy(d => d.SubCode).Select(d => new VoucherLineLabeledValue(d.SubTitle, d.DETAIL_VALUE)).ToList()))
                .ToList();

            var receipt = receipts.FirstOrDefault(r => r.ID == line.RECEIP_ID);
            var cheque = checks.FirstOrDefault(c => c.ID == line.CHECK_ID);

            return new VoucherLineDisplayDto(
                line.ID,
                lineTafsilis,
                lineAttributes,
                lineIdentities,
                receipt is null ? null : new VoucherLineReceiptDisplay((int)receipt.RECEIPT_KIND, receipt.RECEIPT_NO, receipt.RECEIPT_DATE),
                cheque is null
                    ? null
                    : new VoucherLineChequeDisplay(cheque.CHEQ_NO, cheque.CHEQ_DATE, cheque.PAYTO, cheque.CHECKBOOK_TYPE == CheckType.Sori, cheque.CHECKBOOK_TITLE));
        }).ToList();
    }
}
