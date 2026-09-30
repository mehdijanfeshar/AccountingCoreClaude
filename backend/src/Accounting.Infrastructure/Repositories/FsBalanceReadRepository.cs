using System.Text;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements.Engine;
using Accounting.Application.FinancialStatements.Queries;
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

    /// <summary>اسناد افتتاحیه: سندی که ردیفی روی حساب رابط افتتاحیه دارد (تصمیم صاحب پروژه ۲۰۲۶-۰۹-۳۰).</summary>
    private const string OpeningVoucherCte = """
        WITH oh AS (
          SELECT DISTINCT d2.VOUCHERSHEAD_ID AS HID
          FROM TB_VOUCHERSDETAIL d2
          JOIN TB_ACCOUNTCODE_INTERFACE i ON i.ACCOUNTCODEID = d2.ACCOUNT_ID
          WHERE i.TYPE = 1
            AND (i.ISDELETED IS NULL OR i.ISDELETED = 0)
            AND (d2.ISDELETED IS NULL OR d2.ISDELETED = 0)
        )
        """;

    private const string isOpening = "(oh.HID IS NOT NULL OR h.DATE_DOC < :fromDate)";
    private const string isPeriod = "(oh.HID IS NULL AND h.DATE_DOC >= :fromDate)";

    /// <summary>فیلتر مشترک همهٔ کوئری‌ها: سال، حذف‌نشده، بدون اختتامیه، حداقل وضعیت، تا پایان دوره (افتتاحیه همیشه).</summary>
    private const string CommonWhere = """
          a.TYPECODE = 3
          AND h.YEAR = :year
          AND (h.ISDELETED IS NULL OR h.ISDELETED = 0)
          AND (d.ISDELETED IS NULL OR d.ISDELETED = 0)
          AND (h.FLAG_STATE IS NULL OR h.FLAG_STATE <> 1)
          AND h.DOCLIFE >= :minDocLife
          AND (oh.HID IS NOT NULL OR (h.DATE_DOC IS NOT NULL AND h.DATE_DOC <= :toDate))
        """;

    public async Task<FsDrillVoucherPageDto> GetVoucherLinesAsync(
        string year,
        string fromDate,
        string toDate,
        IReadOnlyCollection<string> vahedCodes,
        int minDocLife,
        string accCode,
        FsDrillWindow window,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (vahedCodes.Count == 0)
        {
            return new FsDrillVoucherPageDto(Array.Empty<FsDrillVoucherLineDto>(), 0, pageNumber, pageSize, 0, 0);
        }

        var parameters = new List<OracleParameter>
        {
            new() { ParameterName = "year", OracleDbType = OracleDbType.Char, Value = year },
            new() { ParameterName = "fromDate", OracleDbType = OracleDbType.Varchar2, Value = fromDate },
            new() { ParameterName = "toDate", OracleDbType = OracleDbType.Varchar2, Value = toDate },
            new() { ParameterName = "minDocLife", OracleDbType = OracleDbType.Int32, Value = minDocLife },
            new() { ParameterName = "accCode", OracleDbType = OracleDbType.Varchar2, Value = accCode },
            new() { ParameterName = "firstRow", OracleDbType = OracleDbType.Int32, Value = (pageNumber - 1) * pageSize },
            new() { ParameterName = "lastRow", OracleDbType = OracleDbType.Int32, Value = pageNumber * pageSize },
        };

        var vahedSql = AppendVahedClause(parameters, vahedCodes);
        var windowSql = window switch
        {
            FsDrillWindow.Opening => $" AND {isOpening}",
            FsDrillWindow.Period => $" AND {isPeriod}",
            _ => string.Empty,
        };

        // صفحه‌بندی با ROW_NUMBER (بدون OFFSET/FETCH) تا به نسخهٔ اوراکل وابسته نباشد؛ جمع و تعداد کل با
        // توابع تحلیلی در همان کوئری.
        var sql = $"""
            {OpeningVoucherCte}
            SELECT * FROM (
              SELECT
                h.ID                 AS "VoucherHeadId",
                h.DOC_NUM            AS "DocNum",
                h.DATE_DOC           AS "DateDoc",
                h.VAHEDCODE          AS "VahedCode",
                h.HEAD_DESC          AS "HeadDesc",
                d.DESCRIPTION        AS "LineDesc",
                NVL(d.DEBTOR, 0)     AS "Debtor",
                NVL(d.CREDITOR, 0)   AS "Creditor",
                CASE WHEN {isOpening} THEN 1 ELSE 0 END AS "IsOpening",
                ROW_NUMBER() OVER (ORDER BY h.DATE_DOC, h.DOC_NUM, d.RADIF, d.ID) AS "Rn",
                COUNT(*) OVER () AS "TotalCount",
                SUM(NVL(d.DEBTOR, 0)) OVER () AS "SumDebtor",
                SUM(NVL(d.CREDITOR, 0)) OVER () AS "SumCreditor"
              FROM TB_VOUCHERSDETAIL d
              JOIN TB_VOUCHERSHEAD h ON h.ID = d.VOUCHERSHEAD_ID
              JOIN TB_ACCOUNTCODE  a ON a.ID = d.ACCOUNT_ID
              LEFT JOIN oh ON oh.HID = h.ID
              WHERE {CommonWhere}
                AND a.ACCCODE = :accCode{windowSql}
                AND ({vahedSql})
            ) WHERE "Rn" > :firstRow AND "Rn" <= :lastRow
            ORDER BY "Rn"
            """;

        var rows = await _dbContext.Database
            .SqlQueryRaw<FsVoucherLineRow>(sql, parameters.ToArray<object>())
            .ToListAsync(cancellationToken);

        var first = rows.FirstOrDefault();

        return new FsDrillVoucherPageDto(
            rows.Select(r => new FsDrillVoucherLineDto(
                Guid.Parse(r.VoucherHeadId.Trim()),
                r.DocNum,
                r.DateDoc,
                r.VahedCode,
                r.HeadDesc,
                r.LineDesc,
                r.Debtor,
                r.Creditor,
                r.IsOpening == 1)).ToList(),
            first is null ? 0 : (int)first.TotalCount,
            pageNumber,
            pageSize,
            first?.SumDebtor ?? 0,
            first?.SumCreditor ?? 0);
    }

    public async Task<FsOutOfPeriodVouchers> GetOutOfPeriodVouchersAsync(
        string year,
        IReadOnlyCollection<string> vahedCodes,
        int minDocLife,
        CancellationToken cancellationToken = default)
    {
        if (vahedCodes.Count == 0)
        {
            return new FsOutOfPeriodVouchers(0, 0, Array.Empty<string>());
        }

        var parameters = new List<OracleParameter>
        {
            new() { ParameterName = "year", OracleDbType = OracleDbType.Char, Value = year },
            new() { ParameterName = "minDocLife", OracleDbType = OracleDbType.Int32, Value = minDocLife },
        };

        var vahedSql = AppendVahedClause(parameters, vahedCodes);

        // همان فیلترهای GetBalancesAsync روی سرِ سند (بدون افتتاحیه)، به‌علاوهٔ «تاریخ خالی یا سال دیگر».
        var where = $"""
              h.YEAR = :year
              AND (h.ISDELETED IS NULL OR h.ISDELETED = 0)
              AND (h.FLAG_STATE IS NULL OR h.FLAG_STATE <> 1)
              AND h.DOCLIFE >= :minDocLife
              AND oh.HID IS NULL
              AND (h.DATE_DOC IS NULL OR SUBSTR(h.DATE_DOC, 1, 4) <> :year)
              AND ({vahedSql})
            """;

        var countSql = $"""
            {OpeningVoucherCte}
            SELECT
              NVL(SUM(CASE WHEN h.DATE_DOC IS NULL THEN 1 ELSE 0 END), 0) AS "NullDateCount",
              NVL(SUM(CASE WHEN h.DATE_DOC IS NOT NULL THEN 1 ELSE 0 END), 0) AS "OtherYearCount"
            FROM TB_VOUCHERSHEAD h
            LEFT JOIN oh ON oh.HID = h.ID
            WHERE {where}
            """;

        var counts = (await _dbContext.Database
            .SqlQueryRaw<FsOutOfPeriodRow>(countSql, parameters.ToArray<object>())
            .ToListAsync(cancellationToken)).FirstOrDefault();

        if (counts is null || counts.NullDateCount + counts.OtherYearCount == 0)
        {
            return new FsOutOfPeriodVouchers(0, 0, Array.Empty<string>());
        }

        // پارامترها دوباره ساخته می‌شوند — یک OracleParameter را نمی‌شود به دو فرمان داد.
        var sampleParameters = new List<OracleParameter>
        {
            new() { ParameterName = "year", OracleDbType = OracleDbType.Char, Value = year },
            new() { ParameterName = "minDocLife", OracleDbType = OracleDbType.Int32, Value = minDocLife },
        };
        var sampleVahedSql = AppendVahedClause(sampleParameters, vahedCodes);
        var sampleSql = $"""
            {OpeningVoucherCte}
            SELECT * FROM (
              SELECT h.VAHEDCODE || '/' || NVL(h.DOC_NUM, '?') || ' (' || NVL(h.DATE_DOC, 'بدون تاریخ') || ')' AS "Value"
              FROM TB_VOUCHERSHEAD h
              LEFT JOIN oh ON oh.HID = h.ID
              WHERE {where.Replace(vahedSql, sampleVahedSql, StringComparison.Ordinal)}
              ORDER BY h.DATE_DOC NULLS FIRST, h.DOC_NUM
            ) WHERE ROWNUM <= 10
            """;

        var samples = await _dbContext.Database
            .SqlQueryRaw<string>(sampleSql, sampleParameters.ToArray<object>())
            .ToListAsync(cancellationToken);

        return new FsOutOfPeriodVouchers((int)counts.NullDateCount, (int)counts.OtherYearCount, samples);
    }

    public async Task<IReadOnlyList<Application.FinancialStatements.Queries.AccountMapping.FsChartMoein>> GetChartMoeinsAsync(
        CancellationToken cancellationToken = default)
    {
        // کدینگ کوچک است (چند هزار ردیف): یک‌جا خوانده و والدها در حافظه وصل می‌شوند.
        var codes = await _dbContext.TB_ACCOUNTCODEs
            .AsNoTracking()
            .Where(a => a.ISDELETED != true)
            .Select(a => new { a.ID, a.ACCCODE, a.ACCCODENAME, a.TYPECODE, a.PARENTID })
            .ToListAsync(cancellationToken);

        var byId = codes.ToDictionary(a => a.ID);

        return codes
            .Where(a => a.TYPECODE == Domain.ValueObjects.TypeCodes.Moin && !string.IsNullOrEmpty(a.ACCCODE))
            .Select(a =>
            {
                var kol = a.PARENTID is { } k && byId.TryGetValue(k, out var kv) ? kv : null;
                var group = kol?.PARENTID is { } g && byId.TryGetValue(g, out var gv) ? gv : null;
                return new Application.FinancialStatements.Queries.AccountMapping.FsChartMoein(
                    a.ACCCODE!, a.ACCCODENAME, kol?.ACCCODE, kol?.ACCCODENAME, group?.ACCCODE, group?.ACCCODENAME);
            })
            .OrderBy(m => m.AccCode, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>شکل خام شمارش V-09.</summary>
    public sealed class FsOutOfPeriodRow
    {
        public long NullDateCount { get; set; }

        public long OtherYearCount { get; set; }
    }

    /// <summary>فهرست صریح واحدها (fail-closed)، در تکه‌های ≤۱۰۰۰ تایی <c>IN</c> (سقف اوراکل)، همه bind.</summary>
    private static string AppendVahedClause(List<OracleParameter> parameters, IReadOnlyCollection<string> vahedCodes)
    {
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

        return vahedSql.ToString();
    }

    /// <summary>شکل خام ردیف سطح «سند».</summary>
    public sealed class FsVoucherLineRow
    {
        public string VoucherHeadId { get; set; } = string.Empty;

        public string? DocNum { get; set; }

        public string? DateDoc { get; set; }

        public string? VahedCode { get; set; }

        public string? HeadDesc { get; set; }

        public string? LineDesc { get; set; }

        public decimal Debtor { get; set; }

        public decimal Creditor { get; set; }

        public int IsOpening { get; set; }

        public long Rn { get; set; }

        public long TotalCount { get; set; }

        public decimal SumDebtor { get; set; }

        public decimal SumCreditor { get; set; }
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

        var vahedSql = AppendVahedClause(parameters, vahedCodes);

        var sql = $"""
            {OpeningVoucherCte}
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
            WHERE {CommonWhere}
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
