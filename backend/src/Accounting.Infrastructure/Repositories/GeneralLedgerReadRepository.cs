using Accounting.Application.Common.Security;
using Accounting.Application.Reports.GeneralLedger;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// دفتر کل از جدول‌ها (نه <c>VW_LEDGERREPORT</c> — رجوع <see cref="GetGeneralLedgerQuery"/>). سه کوئری روی یک
/// پایهٔ مشترک «یک سطر به‌ازای (کل، سند)»: جمع هر کل، شمار سطرهای دوره، و صفحهٔ سطرها با ماندهٔ جاری که با
/// تابع پنجره‌ای روی کل مجموعه (نه فقط صفحه) حساب می‌شود تا صفحه‌بندی ماندهٔ جاری را خراب نکند.
/// </summary>
public sealed class GeneralLedgerReadRepository : IGeneralLedgerReadRepository
{
    private const string OpeningPredicate = "(:fromDate IS NOT NULL AND b.DATE_DOC IS NOT NULL AND b.DATE_DOC < :fromDate)";

    private readonly LegacyDbContext _dbContext;
    private readonly IReportUnitScope? _unitScope;

    public GeneralLedgerReadRepository(LegacyDbContext dbContext, IReportUnitScope? unitScope = null)
    {
        _dbContext = dbContext;
        _unitScope = unitScope;
    }

    public async Task<GeneralLedgerResultDto> GetAsync(GetGeneralLedgerQuery q, CancellationToken cancellationToken = default)
    {
        var (baseSql, baseParams) = BuildBase(q);

        var accounts = await _dbContext.Database.SqlQueryRaw<AccountRow>(
            $"""
            WITH base AS ({baseSql})
            SELECT b.KOL AS "KolCode",
                   MAX(b.KOLNAME) AS "KolName",
                   NVL(SUM(CASE WHEN {OpeningPredicate} THEN b.DEB - b.CRED END), 0) AS "Opening",
                   NVL(SUM(CASE WHEN NOT {OpeningPredicate} THEN b.DEB END), 0) AS "PeriodDebit",
                   NVL(SUM(CASE WHEN NOT {OpeningPredicate} THEN b.CRED END), 0) AS "PeriodCredit"
            FROM base b
            GROUP BY b.KOL
            ORDER BY b.KOL
            """,
            CloneList(baseParams).ToArray()).ToListAsync(cancellationToken);

        var total = (await _dbContext.Database.SqlQueryRaw<CountRow>(
            $"""
            WITH base AS ({baseSql})
            SELECT COUNT(*) AS "Value" FROM base b WHERE NOT {OpeningPredicate}
            """,
            CloneList(baseParams).ToArray()).ToListAsync(cancellationToken)).FirstOrDefault()?.Value ?? 0;

        var pageParams = CloneList(baseParams);
        pageParams.Add(new OracleParameter { ParameterName = "skip", OracleDbType = OracleDbType.Int32, Value = (q.PageNumber - 1) * q.PageSize });
        pageParams.Add(new OracleParameter { ParameterName = "take", OracleDbType = OracleDbType.Int32, Value = q.PageSize });
        var rows = await _dbContext.Database.SqlQueryRaw<LedgerRow>(
            $"""
            WITH base AS ({baseSql}),
            opening AS (
              SELECT b.KOL, NVL(SUM(CASE WHEN {OpeningPredicate} THEN b.DEB - b.CRED END), 0) AS OB
              FROM base b GROUP BY b.KOL
            ),
            period AS (
              SELECT b.KOL, b.HEADID, b.VAHED, b.DATE_DOC, b.DOC_NUM, b.DESCR, b.DEB, b.CRED,
                     o.OB + SUM(b.DEB - b.CRED) OVER (
                       PARTITION BY b.KOL ORDER BY b.DATE_DOC NULLS LAST, b.DOC_NUM, b.HEADID
                       ROWS UNBOUNDED PRECEDING) AS BAL
              FROM base b JOIN opening o ON o.KOL = b.KOL
              WHERE NOT {OpeningPredicate}
            )
            SELECT p.KOL AS "KolCode", p.HEADID AS "VoucherHeadId", p.VAHED AS "VahedCode", p.DATE_DOC AS "DateDoc",
                   p.DOC_NUM AS "DocNum", p.DESCR AS "Description", p.DEB AS "Debit", p.CRED AS "Credit", p.BAL AS "Balance"
            FROM period p
            ORDER BY p.KOL, p.DATE_DOC NULLS LAST, p.DOC_NUM, p.HEADID
            OFFSET :skip ROWS FETCH NEXT :take ROWS ONLY
            """,
            pageParams.ToArray()).ToListAsync(cancellationToken);

        return new GeneralLedgerResultDto(
            accounts.Select(a => new GeneralLedgerAccountDto(
                a.KolCode, a.KolName, a.Opening, a.PeriodDebit, a.PeriodCredit, a.Opening + a.PeriodDebit - a.PeriodCredit)).ToList(),
            rows.Select(r => new GeneralLedgerRowDto(
                r.KolCode, Guid.TryParse(r.VoucherHeadId?.Trim(), out var id) ? id : Guid.Empty, r.VahedCode, r.DateDoc,
                r.DocNum, r.Description, r.Debit, r.Credit, r.Balance)).ToList(),
            q.PageNumber,
            q.PageSize,
            total);
    }

    private (string Sql, List<OracleParameter> Params) BuildBase(GetGeneralLedgerQuery q)
    {
        var p = new List<OracleParameter>
        {
            new() { ParameterName = "year", OracleDbType = OracleDbType.Char, Value = q.Year },
            new() { ParameterName = "docLife", OracleDbType = OracleDbType.Int32, Value = (object?)q.DocLife ?? DBNull.Value },
            new() { ParameterName = "fromDate", OracleDbType = OracleDbType.Varchar2, Value = Blank(q.FromDate) ?? (object)DBNull.Value },
            new() { ParameterName = "toDate", OracleDbType = OracleDbType.Varchar2, Value = Blank(q.ToDate) ?? (object)DBNull.Value },
        };

        var codes = _unitScope?.VahedCodes;
        string vahedSql;
        if (codes is { Count: > 0 })
        {
            var (inSql, inParams) = ReportUnitScopeSql.InClause("h.VAHEDCODE", codes, "vc");
            vahedSql = inSql;
            p.AddRange(inParams);
        }
        else
        {
            vahedSql = "h.VAHEDCODE = :vahedCode";
            p.Add(new OracleParameter { ParameterName = "vahedCode", OracleDbType = OracleDbType.Varchar2, Value = q.VahedCode });
        }

        var extra = string.Empty;
        if (Blank(q.FromKol) is { } fromKol)
        {
            extra += " AND ak.ACCCODE >= :fromKol";
            p.Add(new OracleParameter { ParameterName = "fromKol", OracleDbType = OracleDbType.Varchar2, Value = fromKol });
        }
        if (Blank(q.ToKol) is { } toKol)
        {
            extra += " AND ak.ACCCODE <= :toKol";
            p.Add(new OracleParameter { ParameterName = "toKol", OracleDbType = OracleDbType.Varchar2, Value = toKol });
        }
        // شمارهٔ سند صفرپیشرو ۶ رقمی ذخیره می‌شود (عین سیستم قدیم: PadLeft(6,'0')).
        if (Blank(q.FromVoucherNo) is { } fromNo)
        {
            extra += " AND h.DOC_NUM >= :fromNo";
            p.Add(new OracleParameter { ParameterName = "fromNo", OracleDbType = OracleDbType.Varchar2, Value = fromNo.PadLeft(6, '0') });
        }
        if (Blank(q.ToVoucherNo) is { } toNo)
        {
            extra += " AND h.DOC_NUM <= :toNo";
            p.Add(new OracleParameter { ParameterName = "toNo", OracleDbType = OracleDbType.Varchar2, Value = toNo.PadLeft(6, '0') });
        }

        var sql = $"""
            SELECT ak.ACCCODE AS KOL, MAX(ak.ACCCODENAME) AS KOLNAME, h.ID AS HEADID, h.VAHEDCODE AS VAHED,
                   h.DATE_DOC, h.DOC_NUM, MAX(h.HEAD_DESC) AS DESCR,
                   NVL(SUM(d.DEBTOR), 0) AS DEB, NVL(SUM(d.CREDITOR), 0) AS CRED
            FROM TB_VOUCHERSDETAIL d
            JOIN TB_VOUCHERSHEAD h ON h.ID = d.VOUCHERSHEAD_ID
            JOIN TB_ACCOUNTCODE a ON a.ID = d.ACCOUNT_ID
            JOIN TB_ACCOUNTCODE ak ON ak.ID = a.PARENTID
            WHERE a.TYPECODE = 3
              AND h.YEAR = :year
              AND (h.ISDELETED IS NULL OR h.ISDELETED = 0)
              AND (d.ISDELETED IS NULL OR d.ISDELETED = 0)
              AND {vahedSql}
              AND (:docLife IS NULL OR h.DOCLIFE >= :docLife)
              AND (:toDate IS NULL OR (h.DATE_DOC IS NOT NULL AND h.DATE_DOC <= :toDate)){extra}
            GROUP BY ak.ACCCODE, h.ID, h.VAHEDCODE, h.DATE_DOC, h.DOC_NUM
            """;
        return (sql, p);
    }

    private static List<OracleParameter> CloneList(List<OracleParameter> source)
        => source.Select(o => new OracleParameter { ParameterName = o.ParameterName, OracleDbType = o.OracleDbType, Value = o.Value }).ToList();

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    public sealed class AccountRow
    {
        public string KolCode { get; set; } = string.Empty;
        public string? KolName { get; set; }
        public decimal Opening { get; set; }
        public decimal PeriodDebit { get; set; }
        public decimal PeriodCredit { get; set; }
    }

    public sealed class CountRow
    {
        public int Value { get; set; }
    }

    public sealed class LedgerRow
    {
        public string KolCode { get; set; } = string.Empty;
        public string? VoucherHeadId { get; set; }
        public string? VahedCode { get; set; }
        public string? DateDoc { get; set; }
        public string? DocNum { get; set; }
        public string? Description { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
    }
}
