using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Queries;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IVoucherAccountingReader"/> — extracted verbatim
/// from <c>PaymentRequestReadRepository.GetVoucherAccountingAsync</c>/<c>GetVoucherDocNumAsync</c>
/// (بخش ۴-ب) so خزانه‌داری بخش ۴-ج can reuse the exact same projection. See interface XML doc.
/// </summary>
public sealed class VoucherAccountingReader : IVoucherAccountingReader
{
    private readonly LegacyDbContext _dbContext;

    public VoucherAccountingReader(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaymentRequestVoucherAccountingDto?> GetAsync(
        Guid? voucherHeadId, CancellationToken cancellationToken = default)
    {
        if (voucherHeadId is not { } id)
        {
            return null;
        }

        var head = await _dbContext.TB_VOUCHERSHEADs
            .AsNoTracking()
            .Where(v => v.ID == id)
            .Select(v => new { v.ID, v.DOC_NUM, v.DATE_DOC, v.DOCLIFE })
            .FirstOrDefaultAsync(cancellationToken);

        if (head is null)
        {
            return null;
        }

        var details = await _dbContext.TB_VOUCHERSDETAILs
            .AsNoTracking()
            .Where(d => d.VOUCHERSHEAD_ID == id && d.ISDELETED != true)
            .OrderBy(d => d.RADIF)
            .Select(d => new { d.ID, d.ACCOUNT_ID, d.DEBTOR, d.CREDITOR })
            .ToListAsync(cancellationToken);

        var lines = new List<PaymentRequestVoucherLineAccountingDto>();

        foreach (var detail in details)
        {
            string? accountCode = null;
            string? accountName = null;

            if (detail.ACCOUNT_ID is { } accountCodeId)
            {
                var account = await _dbContext.TB_ACCOUNTCODEs
                    .AsNoTracking()
                    .Where(a => a.ID == accountCodeId)
                    .Select(a => new { a.ACCCODE, a.ACCCODENAME })
                    .FirstOrDefaultAsync(cancellationToken);

                accountCode = account?.ACCCODE;
                accountName = account?.ACCCODENAME;
            }

            var tafsiliLabels = await (
                from link in _dbContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.AsNoTracking()
                where link.VOUCHERSDETAIL_ID == detail.ID && !link.ISDELETED
                join tafsili in _dbContext.TB_TAFSILIs.AsNoTracking()
                    on link.TAFSILI_ID equals tafsili.ID into tafsilis
                from tafsili in tafsilis.DefaultIfEmpty()
                select tafsili != null ? tafsili.TAFSILI_CODE + " - " + tafsili.TAFSILI_NAME : null)
                .ToListAsync(cancellationToken);

            lines.Add(new PaymentRequestVoucherLineAccountingDto(
                accountCode,
                accountName,
                string.Join("، ", tafsiliLabels.Where(l => l is not null)),
                detail.DEBTOR ?? 0,
                detail.CREDITOR ?? 0));
        }

        return new PaymentRequestVoucherAccountingDto(
            head.ID,
            head.DOC_NUM,
            head.DATE_DOC,
            head.DOCLIFE,
            lines,
            lines.Sum(l => l.Debit),
            lines.Sum(l => l.Credit));
    }
}
