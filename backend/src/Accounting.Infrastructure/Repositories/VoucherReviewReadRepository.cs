using Accounting.Application.Common.Interfaces;
using Accounting.Application.Reports.VoucherReview;
using Accounting.Application.Reports.VoucherReview.GetVoucherReview;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IVoucherReviewReadRepository"/> — مرور اسناد.
///
/// <para>
/// <b>⚠️ Recorded exception to team working-rule #2 ("the read side reads from a View").</b> A view
/// for this report does exist — <c>VW_VOUCHERREVIEW</c> — and this repository was first written
/// against it. Reading its definition off live Oracle (2026-09-22) showed it cannot be used:
/// </para>
///
/// <code>
/// LEFT JOIN TB_VOUCHERDETAIL_LINK_TAFSILI vt ON dd.ID = vt.VOUCHERSDETAIL_ID
/// ...
/// SUM(dd.Debtor), SUM(dd.Creditor)   -- grouped only by the HEAD's columns
/// </code>
///
/// <para>
/// That join is never referenced in the SELECT or the GROUP BY, so it does nothing but fan each
/// detail line out into one row per تفصیلی link before the sums are taken. A line with three
/// تفصیلی links is counted three times. Because the number of links varies per line, the fan-out
/// factor differs between the debtor and creditor sides of the same voucher — so a perfectly
/// balanced voucher can be reported as out of balance. For the one report whose entire purpose is
/// to reveal imbalance, that is not a rounding concern; it is the report lying.
/// </para>
///
/// <para>
/// Compare <see cref="MatrixReportReadRepository"/>, which does follow rule #2: its view joins each
/// تفصیلی level separately (<c>level_code = n</c>), keeps one row per line, and filters
/// <c>isdeleted = 0</c> in the join. The rule is sound; this particular view is not.
/// </para>
///
/// <para>
/// So the aggregation is done here instead, over <c>TB_VOUCHERSHEAD</c>/<c>TB_VOUCHERSDETAIL</c>,
/// grouped by the head — no fan-out is possible because no تفصیلی table is touched. This also lets
/// the report return the <b>gross</b> totals of both sides, which the view could not express at
/// all: its two "balance" columns are one-sided nets that are zero for every healthy voucher.
/// </para>
///
/// <para>
/// ⚠️ No boolean reaches the generated SQL as a literal. <c>ISDELETED</c> on these tables is mapped
/// with an explicit <c>NUMBER(1)</c> store type, which is what makes <c>!= true</c> render as a
/// number comparison here and not as <c>&lt;&gt; True</c> — see
/// <c>OracleBooleanProjectionTests</c>, which pins exactly that distinction.
/// </para>
/// </summary>
public sealed class VoucherReviewReadRepository : IVoucherReviewReadRepository
{
    /// <summary>
    /// Width of <c>DOC_NUM</c>. Voucher numbers are stored zero-padded, so a range bound has to be
    /// padded to the same width before an ordinal comparison means anything: unpadded, "9" sorts
    /// after "10". The reference project pads identically before running its own query.
    /// </summary>
    private const int VoucherNumberWidth = 6;

    private readonly LegacyDbContext _dbContext;

    public VoucherReviewReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<VoucherReviewResultDto> GetAsync(
        GetVoucherReviewQuery query,
        CancellationToken cancellationToken = default)
    {
        var heads = FilterHeads(query);

        // One aggregated row per voucher. The detail sub-query is correlated per head rather than
        // joined, which is what keeps a voucher with many lines from multiplying the head's own
        // columns — the mistake VW_VOUCHERREVIEW makes with its تفصیلی join.
        var aggregated = heads.Select(h => new
        {
            Head = h,
            Debtor = h.TB_VOUCHERSDETAILs
                .Where(d => d.ISDELETED != true)
                .Sum(d => d.DEBTOR ?? 0m),
            Creditor = h.TB_VOUCHERSDETAILs
                .Where(d => d.ISDELETED != true)
                .Sum(d => d.CREDITOR ?? 0m),
        });

        // Count and both totals in one round trip. The totals deliberately cover the whole
        // filtered set, not the returned page — see VoucherReviewResultDto.
        var summary = await aggregated
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Debtor = g.Sum(x => x.Debtor),
                Creditor = g.Sum(x => x.Creditor),
                Unbalanced = g.Count(x => x.Debtor != x.Creditor),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var items = await aggregated
            // Newest first, matching the کارتابل ordering: a report opened to review recent work
            // should not start at the oldest voucher of the year.
            .OrderByDescending(x => x.Head.DATE_DOC)
            .ThenByDescending(x => x.Head.DOC_NUM)
            .ThenBy(x => x.Head.ID)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new VoucherReviewRowDto(
                x.Head.ID,
                x.Head.DOC_NUM ?? string.Empty,
                x.Head.SYSTEM_TYPENavigation!.SYS_NAME ?? string.Empty,
                x.Head.ATF_NUM ?? string.Empty,
                x.Head.HEAD_DESC ?? string.Empty,
                x.Head.DOCLIFE == null ? null : (int)x.Head.DOCLIFE.Value,
                x.Head.DATE_DOC ?? string.Empty,
                x.Head.YEAR ?? string.Empty,
                x.Debtor,
                x.Creditor))
            .ToListAsync(cancellationToken);

        return new VoucherReviewResultDto(
            items,
            query.PageNumber,
            query.PageSize,
            summary?.Count ?? 0,
            summary?.Debtor ?? 0m,
            summary?.Creditor ?? 0m,
            summary?.Unbalanced ?? 0);
    }

    private IQueryable<TB_VOUCHERSHEAD> FilterHeads(GetVoucherReviewQuery query)
    {
        var heads = _dbContext.TB_VOUCHERSHEADs
            .AsNoTracking()
            .Where(h => h.ISDELETED != true)
            .Where(h => h.YEAR == query.Year)
            .Where(h => h.VAHEDCODE == query.VahedCode);

        // DATE_DOC (YYYYMMDD) and the padded DOC_NUM are fixed-width strings, so ordinal
        // comparison is chronological / numerical with no conversion needed.
        if (!string.IsNullOrWhiteSpace(query.FromDate))
        {
            heads = heads.Where(h => h.DATE_DOC != null && string.Compare(h.DATE_DOC, query.FromDate) >= 0);
        }

        if (!string.IsNullOrWhiteSpace(query.ToDate))
        {
            heads = heads.Where(h => h.DATE_DOC != null && string.Compare(h.DATE_DOC, query.ToDate) <= 0);
        }

        if (!string.IsNullOrWhiteSpace(query.FromVoucherNo))
        {
            var from = query.FromVoucherNo.PadLeft(VoucherNumberWidth, '0');
            heads = heads.Where(h => h.DOC_NUM != null && string.Compare(h.DOC_NUM, from) >= 0);
        }

        if (!string.IsNullOrWhiteSpace(query.ToVoucherNo))
        {
            var to = query.ToVoucherNo.PadLeft(VoucherNumberWidth, '0');
            heads = heads.Where(h => h.DOC_NUM != null && string.Compare(h.DOC_NUM, to) <= 0);
        }

        if (!string.IsNullOrWhiteSpace(query.FromAtfNo))
        {
            var from = query.FromAtfNo;
            heads = heads.Where(h => h.ATF_NUM != null && string.Compare(h.ATF_NUM, from) >= 0);
        }

        if (!string.IsNullOrWhiteSpace(query.ToAtfNo))
        {
            var to = query.ToAtfNo;
            heads = heads.Where(h => h.ATF_NUM != null && string.Compare(h.ATF_NUM, to) <= 0);
        }

        if (query.DocLife.HasValue)
        {
            var docLife = (Accounting.Domain.ValueObjects.DocLife)query.DocLife.Value;
            heads = heads.Where(h => h.DOCLIFE == docLife);
        }

        if (query.SystemTypeId.HasValue)
        {
            heads = heads.Where(h => h.SYSTEM_TYPE == query.SystemTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Description))
        {
            var term = query.Description;
            heads = heads.Where(h => h.HEAD_DESC != null && h.HEAD_DESC.Contains(term));
        }

        return heads;
    }
}
