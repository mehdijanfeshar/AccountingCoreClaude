using System.Text;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements.Engine;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// ماندهٔ معین‌ها برای موتور صورت‌های مالی (فاز ۴۵-ب، <c>docs/fs-module.md</c> §۷). SQL خام پارامتری،
/// هم‌الگوی <see cref="TrialBalanceReadRepository"/> و به همان دلیل (<c>DOCLIFE</c>/<c>FLAG_STATE</c> عددی
/// که روی Entity هنوز <c>bool</c>/<c>decimal</c> نگاشت شده‌اند). همهٔ مقادیر bind می‌شوند.
/// <list type="bullet">
/// <item><b>سند افتتاحیه</b> (تصمیم صاحب پروژه ۲۰۲۶-۰۹-۳۰): سندی که حداقل یک ردیف روی حسابی از
/// <c>TB_ACCOUNTCODE_INTERFACE</c> با <c>TYPE = 1</c> دارد — کل آن سند «ابتدا» است، هر تاریخی داشته باشد.</item>
/// <item><b>سند اختتامیه</b> (<c>FLAG_STATE = 1</c>، تصمیم صاحب پروژه ۲۰۲۶-۰۹-۳۰) هرگز جمع نمی‌شود،
/// وگرنه همهٔ مانده‌های پایان سال صفر می‌شدند.</item>
/// <item><c>DATE_DOC</c> رشتهٔ جلالی <c>YYYYMMDD</c> است و مقایسهٔ رشته‌ای درست است؛ سند غیرافتتاحیهٔ
/// بدون تاریخ حساب نمی‌شود.</item>
/// <item><b>واحدها:</b> فهرست صریح (fail-closed)، در تکه‌های ≤۱۰۰۰ تایی (سقف <c>IN</c> اوراکل).</item>
/// </list>
/// </summary>
public sealed class FsBalanceReadRepository : IFsBalanceReadRepository
{
    private const int InListChunk = 1000;

    private readonly LegacyDbContext _dbContext;

    public FsBalanceReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<FsAccountBalance>> GetBalancesAsync(
        string year,
        string fromDate,
        string toDate,
        IReadOnlyCollection<string> vahedCodes,
        int minDocLife,
        CancellationToken cancellationToken = default)
    {
        if (vahedCodes.Count == 0)
        {
            return Array.Empty<FsAccountBalance>();
        }

        var parameters = new List<OracleParameter>
        {
            new() { ParameterName = "year", OracleDbType = OracleDbType.Char, Value = year },
            new() { ParameterName = "fromDate", OracleDbType = OracleDbType.Varchar2, Value = fromDate },
            new() { ParameterName = "toDate", OracleDbType = OracleDbType.Varchar2, Value = toDate },
            new() { ParameterName = "minDocLife", OracleDbType = OracleDbType.Int32, Value = minDocLife },
        };

        var vahedSql = new StringBuilder();
        var codes = vahedCodes.ToList();

        for (var chunk = 0; chunk * InListChunk < codes.Count; chunk++)
        {
            if (chunk > 0)
            {
                vahedSql.Append(" OR ");
            }

            var names = new List<string>();

            foreach (var code in codes.Skip(chunk * InListChunk).Take(InListChunk))
            {
                var name = $"v{parameters.Count}";
                names.Add(":" + name);
                parameters.Add(new OracleParameter { ParameterName = name, OracleDbType = OracleDbType.Varchar2, Value = code });
            }

            vahedSql.Append("h.VAHEDCODE IN (").Append(string.Join(", ", names)).Append(')');
        }

        const string isOpening = "(oh.HID IS NOT NULL OR h.DATE_DOC < :fromDate)";
        const string isPeriod = "(oh.HID IS NULL AND h.DATE_DOC >= :fromDate)";

        var sql = $"""
            WITH oh AS (
              SELECT DISTINCT d2.VOUCHERSHEAD_ID AS HID
              FROM TB_VOUCHERSDETAIL d2
              JOIN TB_ACCOUNTCODE_INTERFACE i ON i.ACCOUNTCODEID = d2.ACCOUNT_ID
              WHERE i.TYPE = 1
                AND (i.ISDELETED IS NULL OR i.ISDELETED = 0)
                AND (d2.ISDELETED IS NULL OR d2.ISDELETED = 0)
            )
            SELECT
              a.ACCCODE          AS "AccCode",
              h.VAHEDCODE        AS "VahedCode",
              MAX(a.ACCCODENAME) AS "AccName",
              NVL(SUM(CASE WHEN {isOpening} THEN d.DEBTOR   END), 0) AS "OpeningDebtor",
              NVL(SUM(CASE WHEN {isOpening} THEN d.CREDITOR END), 0) AS "OpeningCreditor",
              NVL(SUM(CASE WHEN {isPeriod}  THEN d.DEBTOR   END), 0) AS "PeriodDebtor",
              NVL(SUM(CASE WHEN {isPeriod}  THEN d.CREDITOR END), 0) AS "PeriodCreditor"
            FROM TB_VOUCHERSDETAIL d
            JOIN TB_VOUCHERSHEAD h ON h.ID = d.VOUCHERSHEAD_ID
            JOIN TB_ACCOUNTCODE  a ON a.ID = d.ACCOUNT_ID
            LEFT JOIN oh ON oh.HID = h.ID
            WHERE a.TYPECODE = 3
              AND h.YEAR = :year
              AND (h.ISDELETED IS NULL OR h.ISDELETED = 0)
              AND (d.ISDELETED IS NULL OR d.ISDELETED = 0)
              AND (h.FLAG_STATE IS NULL OR h.FLAG_STATE <> 1)
              AND h.DOCLIFE >= :minDocLife
              AND (oh.HID IS NOT NULL OR (h.DATE_DOC IS NOT NULL AND h.DATE_DOC <= :toDate))
              AND ({vahedSql})
            GROUP BY a.ACCCODE, h.VAHEDCODE
            """;

        var rows = await _dbContext.Database
            .SqlQueryRaw<FsBalanceRow>(sql, parameters.ToArray<object>())
            .ToListAsync(cancellationToken);

        return rows
            .Where(r => !string.IsNullOrEmpty(r.AccCode))
            .Select(r => new FsAccountBalance(r.AccCode, r.AccName, r.OpeningDebtor, r.OpeningCreditor, r.PeriodDebtor, r.PeriodCreditor, r.VahedCode))
            .ToList();
    }

    /// <summary>شکل خام ردیف کوئری — ستون‌ها با alias هم‌نام پراپرتی‌ها.</summary>
    public sealed class FsBalanceRow
    {
        public string AccCode { get; set; } = string.Empty;

        public string? AccName { get; set; }

        public string? VahedCode { get; set; }

        public decimal OpeningDebtor { get; set; }

        public decimal OpeningCreditor { get; set; }

        public decimal PeriodDebtor { get; set; }

        public decimal PeriodCreditor { get; set; }
    }
}
