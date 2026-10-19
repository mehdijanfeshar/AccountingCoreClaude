using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Accounting.Application.OperationTemplates.History;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;

namespace Accounting.Infrastructure.OperationTemplates;

/// <summary>
/// خواندن تجمیعی سندها برای «یادگیری از سوابق» (فاز ۶۳) با SQL خام پارامتری، هم‌الگوی
/// <c>FsBalanceReadRepository</c> (<c>Database.SqlQueryRaw</c>، چون DOCLIFE/FLAG_STATE در Entity نوع عددی ندارند و
/// <c>LISTAGG/MEDIAN/GROUPING SETS</c> در LINQ نیست). استثنای ثبت‌شدهٔ «خواندن مستقیم از جدول نوشتن»: خروجی ذخیرهٔ دائمی
/// نمی‌شود، فقط جدول محاسبه‌شدهٔ سوابق را می‌سازد و View مناسبی برای ساختار «مجموعهٔ معین‌های هر سند» وجود ندارد.
///
/// <para>هدف (تصمیم صاحب پروژه، ۲۰۲۶-۱۰-۱۸): فقط سندهای دستیِ تکرارشوندهٔ طول سال. فیلتر سند: DOCLIFE ∈ {بررسی‌شده ۳، تأیید دائم ۴}،
/// حذف‌نشده، بدون اختتامیه (FLAG_STATE=1)، بدون هر سندی که ردیفی روی حساب رابط دارد (افتتاحیه/اختتامیه
/// TB_ACCOUNTCODE_INTERFACE، رابط اعلامیه TB_RABET، رابط اختتامیهٔ گروه TB_RABET_CLOSING)، بدون سند اعلامیه (TB_ELAMHEAD)،
/// بدون سند ارسالی بین واحدها (SNDVAHEDCODE) و سند برگشتی (PARENTHEAD_ID)، بدون هر سند خودکار (ISAUTOMATIC، از جمله حسابیار)،
/// بدون سندی که عنوان یکی از معین‌هایش «اعلامیه» یا «رابط» دارد (رابط اعلامیه ممکن است در TB_RABET ثبت نشده باشد، ریسک #۳۱)، بدون شرح حاوی «اعلامیه/افتتاحیه/اختتامیه» (ي/ك عربی با TRANSLATE یکسان می‌شود)، و شرح غیراصلاحی (اصلاح/برگشت/معکوس/ابطال). فقط ۲ تا ۸ ردیف، همه روی معین فعال، و تراز.</para>
/// </summary>
public sealed class HistorySourceReader : IHistorySourceReader
{
    private const string ReversalRegex = "اصلاح|برگشت|معکوس|معكوس|ابطال";

    private readonly LegacyDbContext _db;
    public HistorySourceReader(LegacyDbContext db) => _db = db;

    // ───────────────────────── SQL ─────────────────────────

    private static string Schema()
    {
        var s = LegacyDbContext.DefaultSchema;
        return !string.IsNullOrWhiteSpace(s) && Regex.IsMatch(s, "^[A-Za-z0-9_$#]+$") ? s + "." : string.Empty;
    }

    /// <summary>زنجیرهٔ CTE: oh → ln (به‌ازای سند، معین، سمت) → vs (به‌ازای سند، امضا) → v → vq (فقط امضاهای ≥ minCount).</summary>
    private static string Chain(string s) => $"""
        WITH rab AS (
          SELECT i.ACCOUNTCODEID AS ACC FROM {s}TB_ACCOUNTCODE_INTERFACE i WHERE NVL(i.ISDELETED, 0) = 0
          UNION
          SELECT r.ACCOUNTCODE_ID FROM {s}TB_RABET r WHERE NVL(r.ISDELETED, 0) = 0 AND r.ACCOUNTCODE_ID IS NOT NULL
          UNION
          SELECT rc.ACCOUNTCODE_RABET_ID FROM {s}TB_RABET_CLOSING rc WHERE NVL(rc.ISDELETED, 0) = 0 AND rc.ACCOUNTCODE_RABET_ID IS NOT NULL
        ),
        oh AS (
          SELECT d2.VOUCHERSHEAD_ID AS HID
          FROM {s}TB_VOUCHERSDETAIL d2
          JOIN rab ON rab.ACC = d2.ACCOUNT_ID
          WHERE NVL(d2.ISDELETED, 0) = 0
            AND d2.YEAR BETWEEN :fromYear AND :toYear
          UNION
          SELECT e.VOUCHERSHEAD_ID FROM {s}TB_ELAMHEAD e WHERE e.VOUCHERSHEAD_ID IS NOT NULL
          UNION
          SELECT d3.VOUCHERSHEAD_ID
          FROM {s}TB_VOUCHERSDETAIL d3
          JOIN {s}TB_ACCOUNTCODE a3 ON a3.ID = d3.ACCOUNT_ID
          WHERE NVL(d3.ISDELETED, 0) = 0
            AND d3.YEAR BETWEEN :fromYear AND :toYear
            AND (TRANSLATE(a3.ACCCODENAME, :trFrom, :trTo) LIKE :kwAnn OR TRANSLATE(a3.ACCCODENAME, :trFrom, :trTo) LIKE :kwRab)
        ),
        ln AS (
          SELECT h.ID AS hid,
                 LOWER(TRIM(d.ACCOUNT_ID)) AS acc,
                 CASE WHEN NVL(d.DEBTOR, 0) > 0 THEN 1 ELSE 2 END AS side,
                 SUM(CASE WHEN NVL(d.DEBTOR, 0) > 0 THEN d.DEBTOR ELSE NVL(d.CREDITOR, 0) END) AS amt,
                 COUNT(*) AS cnt,
                 MAX(CASE WHEN a.ID IS NULL OR a.TYPECODE <> 3 OR NVL(a.ISDELETED, 0) = 1 THEN 1 ELSE 0 END) AS bad
          FROM {s}TB_VOUCHERSDETAIL d
          JOIN {s}TB_VOUCHERSHEAD h ON h.ID = d.VOUCHERSHEAD_ID
          LEFT JOIN {s}TB_ACCOUNTCODE a ON a.ID = d.ACCOUNT_ID
          LEFT JOIN oh ON oh.HID = h.ID
          WHERE oh.HID IS NULL
            AND h.YEAR BETWEEN :fromYear AND :toYear
            AND NVL(h.ISDELETED, 0) = 0
            AND NVL(d.ISDELETED, 0) = 0
            AND h.DOCLIFE IN (3, 4)
            AND NVL(h.FLAG_STATE, 0) <> 1
            AND h.VAHEDCODE IS NOT NULL
            AND h.SNDVAHEDCODE IS NULL
            AND h.PARENTHEAD_ID IS NULL
            AND NVL(h.ISAUTOMATIC, 0) = 0
            AND (h.HEAD_DESC IS NULL OR NOT REGEXP_LIKE(h.HEAD_DESC, :revRx))
            AND (h.HEAD_DESC IS NULL OR (TRANSLATE(h.HEAD_DESC, :trFrom, :trTo) NOT LIKE :kwAnn
                 AND TRANSLATE(h.HEAD_DESC, :trFrom, :trTo) NOT LIKE :kwOpen
                 AND TRANSLATE(h.HEAD_DESC, :trFrom, :trTo) NOT LIKE :kwClose))
            AND (NVL(d.DEBTOR, 0) > 0 OR NVL(d.CREDITOR, 0) > 0)
          GROUP BY h.ID, LOWER(TRIM(d.ACCOUNT_ID)), CASE WHEN NVL(d.DEBTOR, 0) > 0 THEN 1 ELSE 2 END
        ),
        vs AS (
          SELECT hid,
                 SUM(CASE WHEN side = 1 THEN amt ELSE 0 END) AS dr,
                 LISTAGG(acc || ':' || side, '|') WITHIN GROUP (ORDER BY acc, side) AS sig
          FROM ln
          GROUP BY hid
          HAVING SUM(cnt) BETWEEN 2 AND 8
             AND MAX(bad) = 0
             AND COUNT(*) >= 2
             AND SUM(CASE WHEN side = 1 THEN amt ELSE 0 END) = SUM(CASE WHEN side = 2 THEN amt ELSE 0 END)
             AND SUM(CASE WHEN side = 1 THEN amt ELSE 0 END) > 0
        ),
        v AS (
          SELECT vs.hid AS hid, vs.sig AS sig, vs.dr AS total,
                 h.VAHEDCODE AS vahed, h.YEAR AS yr, h.DATE_DOC AS dt, h.HEAD_DESC AS hd
          FROM vs
          JOIN {s}TB_VOUCHERSHEAD h ON h.ID = vs.hid
        ),
        vq AS (
          SELECT v.hid AS hid, v.sig AS sig, v.total AS total, v.vahed AS vahed, v.yr AS yr, v.dt AS dt, v.hd AS hd
          FROM v
          WHERE v.sig IN (SELECT v2.sig FROM v v2 GROUP BY v2.sig HAVING COUNT(*) >= :minCount)
        )
        """;

    private static OracleParameter[] Parameters(int fromYear, int toYear, int minCount, params OracleParameter[] extra)
    {
        var list = new List<OracleParameter>
        {
            new() { ParameterName = "fromYear", OracleDbType = OracleDbType.Char, Value = fromYear.ToString(CultureInfo.InvariantCulture) },
            new() { ParameterName = "toYear", OracleDbType = OracleDbType.Char, Value = toYear.ToString(CultureInfo.InvariantCulture) },
            new() { ParameterName = "revRx", OracleDbType = OracleDbType.NVarchar2, Value = ReversalRegex },
            new() { ParameterName = "minCount", OracleDbType = OracleDbType.Int32, Value = minCount },
            // ي/ك عربی ⇒ ی/ک فارسی در مقایسه؛ کلمه‌ها هم فارسی‌اند
            new() { ParameterName = "trFrom", OracleDbType = OracleDbType.NVarchar2, Value = "يك" },
            new() { ParameterName = "trTo", OracleDbType = OracleDbType.NVarchar2, Value = "یک" },
            new() { ParameterName = "kwAnn", OracleDbType = OracleDbType.NVarchar2, Value = "%اعلامیه%" },
            new() { ParameterName = "kwRab", OracleDbType = OracleDbType.NVarchar2, Value = "%رابط%" },
            new() { ParameterName = "kwOpen", OracleDbType = OracleDbType.NVarchar2, Value = "%افتتاحیه%" },
            new() { ParameterName = "kwClose", OracleDbType = OracleDbType.NVarchar2, Value = "%اختتامیه%" },
        };
        list.AddRange(extra);
        return list.ToArray();
    }

    /// <summary>تجمیع سنگین است؛ زمان‌سنج پیش‌فرض (۳۰ ثانیه) کافی نیست.</summary>
    private void LongTimeout() => _db.Database.SetCommandTimeout(TimeSpan.FromMinutes(15));

    // ───────────────────────── خواندن ─────────────────────────

    public async Task<IReadOnlyList<HistoryAggRow>> ReadAggregatesAsync(int fromYear, int toYear, int minCount, CancellationToken ct)
    {
        LongTimeout();
        var sql = Chain(Schema()) + """

            SELECT sig AS "Sig", vahed AS "Vahed", GROUPING(vahed) AS "IsPattern",
                   COUNT(*) AS "Cnt", COUNT(DISTINCT vahed) AS "Units", MEDIAN(total) AS "Med",
                   MAX(dt) AS "LastDate", MIN(yr) AS "MinYear", MAX(yr) AS "MaxYear"
            FROM vq
            GROUP BY GROUPING SETS ((sig), (sig, vahed))
            """;
        var rows = await _db.Database.SqlQueryRaw<AggRow>(sql, Parameters(fromYear, toYear, minCount).ToArray<object>()).ToListAsync(ct);
        return rows.Where(r => !string.IsNullOrEmpty(r.Sig))
            .Select(r => new HistoryAggRow(r.Sig, r.Vahed?.Trim(), r.IsPattern == 1, (int)r.Cnt, (int)r.Units,
                (long)Math.Round(r.Med, MidpointRounding.AwayFromZero), r.LastDate?.Trim(), YearOf(r.MinYear), YearOf(r.MaxYear)))
            .ToList();
    }

    public async Task<IReadOnlyList<HistoryShareRow>> ReadSharesAsync(int fromYear, int toYear, int minCount, CancellationToken ct)
    {
        LongTimeout();
        var sql = Chain(Schema()) + """

            SELECT vq.sig AS "Sig", l.acc AS "Acc", l.side AS "Side", MEDIAN(l.amt * 100 / vq.total) AS "Share"
            FROM vq
            JOIN ln l ON l.hid = vq.hid
            GROUP BY vq.sig, l.acc, l.side
            """;
        var rows = await _db.Database.SqlQueryRaw<ShareRow>(sql, Parameters(fromYear, toYear, minCount).ToArray<object>()).ToListAsync(ct);
        return rows.Where(r => Guid.TryParse(r.Acc?.Trim(), out _))
            .Select(r => new HistoryShareRow(r.Sig, Guid.Parse(r.Acc!.Trim()), (int)r.Side, r.Share))
            .ToList();
    }

    public async IAsyncEnumerable<HistorySampleRow> ReadSamplesAsync(int fromYear, int toYear, int minCount, int globalSample, int unitSample,
        [EnumeratorCancellation] CancellationToken ct)
    {
        LongTimeout();
        var s = Schema();
        var sql = Chain(s) + $"""
            ,
            ranked AS (
              SELECT vq.hid AS hid, vq.sig AS sig, vq.vahed AS vahed, vq.hd AS hd,
                     ROW_NUMBER() OVER (PARTITION BY vq.sig, vq.vahed ORDER BY vq.dt DESC, vq.hid) AS rnu,
                     ROW_NUMBER() OVER (PARTITION BY vq.sig ORDER BY vq.dt DESC, vq.hid) AS rng
              FROM vq
            )
            SELECT r.sig AS "Sig", r.vahed AS "Vahed", r.rnu AS "UnitRank", r.rng AS "GlobalRank", r.hd AS "HeadDesc",
                   (SELECT LISTAGG(SUBSTR(d3.DESCRIPTION, 1, 60), ' ') WITHIN GROUP (ORDER BY d3.RADIF)
                      FROM {s}TB_VOUCHERSDETAIL d3
                     WHERE d3.VOUCHERSHEAD_ID = r.hid AND NVL(d3.ISDELETED, 0) = 0) AS "LineDescs"
            FROM ranked r
            WHERE r.rnu <= :unitSample OR r.rng <= :globalSample
            """;
        var parameters = Parameters(fromYear, toYear, minCount,
            new OracleParameter { ParameterName = "unitSample", OracleDbType = OracleDbType.Int32, Value = unitSample },
            new OracleParameter { ParameterName = "globalSample", OracleDbType = OracleDbType.Int32, Value = globalSample });

        await foreach (var r in _db.Database.SqlQueryRaw<SampleRowRaw>(sql, parameters.ToArray<object>()).AsAsyncEnumerable().WithCancellation(ct))
            yield return new HistorySampleRow(r.Sig, (r.Vahed ?? "").Trim(), (int)r.UnitRank, (int)r.GlobalRank, r.HeadDesc, r.LineDescs);
    }

    private static int YearOf(string? y) => int.TryParse(y?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : 0;

    // ───────────────────────── ردیف‌های خام (نام ستون = نام نگارش‌های بالا) ─────────────────────────

    public sealed class AggRow
    {
        public string Sig { get; set; } = string.Empty;
        public string? Vahed { get; set; }
        public decimal IsPattern { get; set; }
        public decimal Cnt { get; set; }
        public decimal Units { get; set; }
        public decimal Med { get; set; }
        public string? LastDate { get; set; }
        public string? MinYear { get; set; }
        public string? MaxYear { get; set; }
    }

    public sealed class ShareRow
    {
        public string Sig { get; set; } = string.Empty;
        public string? Acc { get; set; }
        public decimal Side { get; set; }
        public decimal Share { get; set; }
    }

    public sealed class SampleRowRaw
    {
        public string Sig { get; set; } = string.Empty;
        public string? Vahed { get; set; }
        public decimal UnitRank { get; set; }
        public decimal GlobalRank { get; set; }
        public string? HeadDesc { get; set; }
        public string? LineDescs { get; set; }
    }
}
