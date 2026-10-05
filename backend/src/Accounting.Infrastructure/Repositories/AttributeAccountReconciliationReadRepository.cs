using Accounting.Application.Common.Interfaces;
using Accounting.Application.Reports.AttributeAccountReconciliation;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// «مغایرت‌گیری حساب‌های شناسه‌دار» (FINACC-523) — مستقیم از جدول‌ها.
///
/// <para>
/// <b>⚠️ استثنای ثبت‌شدهٔ قاعدهٔ ۹ (ریسک #۳۰).</b> پروژهٔ مرجع از سه View می‌خواند که هیچ‌کدام
/// قابل استفاده نیستند (تعریف‌ها از اوراکل زنده، ۲۰۲۶-۱۰-۰۵):
/// <c>VW_ATTRIBVALUEBYMOINREPORT</c> شناسه‌ها را فقط با <c>tinv.attribforaccountcode_id = ta.id</c>
/// وصل می‌کند و هر ردیف سند را در همهٔ شناسه‌های آن معین ضرب می‌کند؛ <c>VW_ATTRIBMISMATCHREPORT</c>
/// مغایرت را روی تک‌ردیف (بدهکار ≠ بستانکار) می‌سنجد که برای هر ردیف عادی درست است؛ و هیچ‌کدام
/// واحد/سال/<c>ISDELETED</c> را فیلتر نمی‌کنند.
/// </para>
///
/// <para>
/// اینجا هر ردیف سند دقیقاً یک بار می‌آید: تعریف شناسهٔ معین (یکتا روی معین+واحد+سال) با inner join،
/// و مقدار شناسهٔ همان ردیف با زیرکوئری همبسته (نه join) تا ضرب پیش نیاید.
/// </para>
/// </summary>
public sealed class AttributeAccountReconciliationReadRepository : IAttributeAccountReconciliationReadRepository
{
    private readonly LegacyDbContext _dbContext;

    public AttributeAccountReconciliationReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AttributeAccountRawLine>> GetLinesAsync(
        AttributeAccountFilter filter,
        CancellationToken cancellationToken = default)
    {
        var definitions = _dbContext.TB_ATTRIBFORACCOUNTCODEs
            .AsNoTracking()
            .Where(a => a.ISDELETED != true && a.VAHEDCODE == filter.VahedCode && a.YEAR == filter.Year);

        if (filter.AccountId is { } accountId)
            definitions = definitions.Where(a => a.ACCOUNTCODE_ID == accountId);

        var details = _dbContext.TB_VOUCHERSDETAILs
            .AsNoTracking()
            .Where(d => d.ISDELETED != true
                && d.VOUCHERSHEAD!.ISDELETED != true
                && d.VOUCHERSHEAD.VAHEDCODE == filter.VahedCode
                && d.VOUCHERSHEAD.YEAR == filter.Year);

        if (filter.FromDate is not null)
            details = details.Where(d => string.Compare(d.VOUCHERSHEAD!.DATE_DOC, filter.FromDate) >= 0);
        if (filter.ToDate is not null)
            details = details.Where(d => string.Compare(d.VOUCHERSHEAD!.DATE_DOC, filter.ToDate) <= 0);
        if (filter.DocLife is { } docLife)
            details = details.Where(d => (int?)d.VOUCHERSHEAD!.DOCLIFE == docLife);

        var rows = await (
            from d in details
            join a in definitions on d.ACCOUNT_ID equals (Guid?)a.ACCOUNTCODE_ID
            select new
            {
                LineId = d.ID,
                AccountId = a.ACCOUNTCODE_ID,
                AccCode = a.ACCOUNTCODE.ACCCODE,
                AccName = a.ACCOUNTCODE.ACCCODENAME,
                AttribSum = (int)a.ATTRIBSUM,
                Value = d.TB_ATTRIBSINVOUCHERs
                    .Where(t => t.ISDELETED != true && t.ATTRIBFORACCOUNTCODE_ID == a.ID)
                    .Select(t => t.ATTRIBUTEVALUE)
                    .FirstOrDefault(),
                HeadId = d.VOUCHERSHEAD_ID,
                DocNum = d.VOUCHERSHEAD!.DOC_NUM,
                DateDoc = d.VOUCHERSHEAD.DATE_DOC,
                DocLife = (int?)d.VOUCHERSHEAD.DOCLIFE,
                HeadDesc = d.VOUCHERSHEAD.HEAD_DESC,
                LineDesc = d.DESCRIPTION,
                Debtor = d.DEBTOR ?? 0m,
                Creditor = d.CREDITOR ?? 0m,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new AttributeAccountRawLine(
                r.LineId,
                r.AccountId,
                r.AccCode ?? string.Empty,
                r.AccName,
                r.AttribSum,
                r.Value,
                r.HeadId ?? Guid.Empty,
                r.DocNum,
                r.DateDoc,
                r.DocLife,
                r.HeadDesc,
                r.LineDesc,
                r.Debtor,
                r.Creditor))
            .ToList();
    }
}
