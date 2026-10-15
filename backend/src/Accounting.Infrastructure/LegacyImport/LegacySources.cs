using Accounting.Application.Common.Interfaces;
using Oracle.ManagedDataAccess.Client;

namespace Accounting.Infrastructure.LegacyImport;

public sealed record MainCode(long Seq, string Kol, string Moi, string Taf, string Name, string KindAct, string KindHes)
{
    public bool IsKol => Moi == "" && Taf == "";
    public bool IsMoein => Moi != "" && Taf == "";
    public bool IsTafsili => Taf != "";
}

/// <summary>ردیف LOWLEVELCODES: بدون ریز = فعالیت (سطح ۱)، با ریز = مرکز هزینه (سطح ۲).</summary>
public sealed record LowCode(long Seq, string Joz, string Riz, string Name)
{
    public bool IsCostCenter => Riz != "";
    public string Key => $"{Joz}/{Riz}";
}

public sealed record OldDoc(long Seq, string No, string Date, string Life, string SysType, string Desc, string Apendix)
{
    public string DocYear => Date.Length >= 4 ? Date[..4] : "";
}

public sealed record OldLine(long Seq, long DocSeq, long HesSeq, long? JrSeq, int Radif, string Desc,
    decimal Bed, decimal Bes, bool HasCheck, bool HasEtebar, long? CidSeq = null, long? PobSeq = null);

// ----- اطلاعات بانکی سیستم قدیم (HBJ/CBH/POB/CID شناسهٔ همان اسکیما است؛ بین سال‌ها با شمارهٔ حساب یکی می‌شوند) -----
/// <summary>HESABJARI: حساب جاری.</summary>
public sealed record OldBankAccount(long Seq, string BankCode, string BranchCode, string AccountNo, string Title, decimal FirstValue, long HesSeq, string Kind);
/// <summary>CHECKBOOKSINHESABJARI: دسته‌چک؛ CB_KIND ۱ = صوری.</summary>
public sealed record OldCheckBook(long Seq, long AccountSeq, string Title, string Date, long From, long To, string Kind, long? ChequeType);
/// <summary>PAPEROFBOOKS: برگ چک.</summary>
public sealed record OldCheck(long Seq, long BookSeq, string No, string Date, string Desc, string DateRsid, string Ebtal, string PayTo, string Print);
/// <summary>CHECKSINDOCS: فیش/حوالهٔ ردیف سند؛ CHEQ_KIND ۲ = حواله، ۳ = فیش (از دادهٔ ۱۳۹۸).</summary>
public sealed record OldReceipt(long Seq, string Kind, string Date, string No, string DateRsid);
/// <summary>BANKCARTDETAIL: کارت بانک؛ BCD_TYPE ۰ = چک صوری، ۱ = چک واقعی، ۲ = حواله، ۳ = فیش.</summary>
public sealed record OldBankCard(long Seq, long? CidSeq, long? PobSeq, string BankCode, string BranchCode, string AccountNo, string Month,
    string ChequeNo, string ReceiveDate, string Type, decimal Bed, decimal Bes, long? IncorrentSeq);
/// <summary>ACC_BRCH: کارگاه؛ BRHCODE = کد شعبهٔ تأمین (واحد)، STAT ۱ = فعال.</summary>
public sealed record OldWorkshop(long Seq, string BranchCode, string Name, string Code, string Kol, string Moi, string Taf, string Stat, long? HesSeq);
/// <summary>ATTRIBFORMAINCODE: ویژگی معین؛ FLAG ۱ = تاریخ، ۲ = عدد (برعکس enum جدید)؛ CONTROLID ۳ = بدون کنترل.</summary>
public sealed record OldAttribDef(long Seq, long HesSeq, int BoxNo, string Flag, int Length, string Sum, string Control);
/// <summary>ATTRIBSINDOCS: مقدار ویژگی ردیف سند.</summary>
public sealed record OldAttribValue(long LineSeq, long DefSeq, string Value);
/// <summary>ELAMHEAD: اعلامیه.</summary>
public sealed record OldElam(long Seq, long? DocSeq, string SerialNo, string Code, string DabirNo, string DabirDate, string SendRcvVahed,
    string Yy, long? PrintNo, string Case, string SerialInput, string WebStat, string Date, string Desc, string RcvNo, string RcvDate,
    string LastMonth, string PayNo, string ZFlag, string PeimanNo, string WorkshopCode, string WorkshopName, long? WorkshopSeq = null);
/// <summary>ELAMDETAIL: ردیف اعلامیه.</summary>
public sealed record OldElamLine(long Seq, long? HesSeq, long ElamSeq, decimal Bed, decimal Bes, string Desc, string AttribNo, long? JrSeq);
/// <summary>CHEQUES_INCORRENT: چک‌های در جریان وصول (منتقل‌شده از سال قبل).</summary>
public sealed record OldIncorrent(long Seq, string DocNo, string DocDate, string ChequeNo, string ChequeDate, string Desc, string PayTo,
    decimal Bes, string DateRsid, string AccountNo);

/// <summary>یک اسکیمای سالانهٔ قدیم و همهٔ دادهٔ لازمش (فقط SELECT).</summary>
public sealed class OldYear
{
    public required string Server { get; init; }
    public required string Schema { get; init; }
    public required int Year { get; init; }
    public required string VahedCode { get; init; }
    public List<MainCode> Codes { get; private init; } = new();
    public List<LowCode> Lows { get; private init; } = new();
    public List<(long HesSeq, long JrSeq)> LowLinks { get; private init; } = new();
    public List<OldDoc> Docs { get; private init; } = new();
    public List<OldLine> Lines { get; private init; } = new();
    public List<OldBankAccount> BankAccounts { get; private init; } = new();
    public List<OldCheckBook> CheckBooks { get; private init; } = new();
    public List<OldCheck> Checks { get; private init; } = new();
    public List<OldReceipt> Receipts { get; private init; } = new();
    public List<OldBankCard> BankCards { get; private init; } = new();
    public List<OldIncorrent> Incorrents { get; private init; } = new();
    public List<Dictionary<string, object?>> ChequeTypes { get; private init; } = new();
    public List<OldAttribDef> AttribDefs { get; private init; } = new();
    public List<OldAttribValue> AttribValues { get; private init; } = new();
    public List<OldElam> Elams { get; private init; } = new();
    public List<OldElamLine> ElamLines { get; private init; } = new();
    public List<OldWorkshop> Workshops { get; private init; } = new();

    /// <summary>برگ چک (POB) ← کلید پایدار بین سال‌ها: شمارهٔ حساب، اولین شمارهٔ دسته‌چک، شمارهٔ چک.</summary>
    public (string AccountNo, long BookFrom, string ChequeNo)? CheckKey(long pobSeq)
    {
        var p = Checks.FirstOrDefault(c => c.Seq == pobSeq);
        var b = p == null ? null : CheckBooks.FirstOrDefault(x => x.Seq == p.BookSeq);
        var a = b == null ? null : BankAccounts.FirstOrDefault(x => x.Seq == b.AccountSeq);
        return a == null ? null : (a.AccountNo, b!.From, p!.No);
    }
    public Dictionary<long, MainCode> CodeBySeq { get; private set; } = new();
    public Dictionary<long, LowCode> LowBySeq { get; private set; } = new();

    /// <summary>کلید پایدار تفصیلی در همهٔ سال‌ها؛ با <see cref="LegacyUnit"/> پر می‌شود.</summary>
    public Dictionary<long, string> TafsiliKey { get; } = new();

    public static async Task<OldYear> LoadAsync(LegacySourceLocator.Source s, string vahed, CancellationToken ct)
    {
        var (server, schema, year) = (s.Server, s.Schema, s.Year);
        await using var cn = await LegacySourceLocator.OpenAsync(s, ct);
        OracleParameter V() => new("v", vahed);
        var y = new OldYear
        {
            Server = server, Schema = schema, Year = year, VahedCode = vahed,
            Codes = await ListAsync(cn, "select hes_seq, cod_kol, cod_moi, cod_taf, hes_name, kind_act, kind_hes from maincodes where vahedcode = :v",
                r => new MainCode(r.GetInt64(0), S(r, 1), S(r, 2), S(r, 3), S(r, 4), S(r, 5), S(r, 6)), ct, V()),
            Lows = await ListAsync(cn, "select jr_seq, cod_joz, cod_riz, nam_haz from lowlevelcodes where vahedcode = :v",
                r => new LowCode(r.GetInt64(0), S(r, 1), S(r, 2), S(r, 3)), ct, V()),
            LowLinks = await ListAsync(cn, "select hes_seq, jr_seq from lowlevelcode_link_maincode where vahedcode = :v",
                r => (r.GetInt64(0), r.GetInt64(1)), ct, V()),
            Docs = await ListAsync(cn, "select doc_seq, no_doc, date_doc, doclife, system_type, head_desc, apendix from docshead where vahedcode = :v",
                r => new OldDoc(r.GetInt64(0), S(r, 1), S(r, 2), S(r, 3), S(r, 4), S(r, 5), S(r, 6)), ct, V()),
            Lines = await ListAsync(cn,
                "select det_seq, doc_seq, hes_seq, jr_seq, radif, detail_desc, amou_bed, amou_bes, " +
                "case when cid_seq is not null or pob_seq is not null then 1 else 0 end, " +
                "case when etebar_id is not null then 1 else 0 end, cid_seq, pob_seq from docsdetail where vahedcode = :v",
                r => new OldLine(r.GetInt64(0), r.GetInt64(1), r.GetInt64(2), L(r, 3),
                    r.GetInt32(4), S(r, 5), r.GetDecimal(6), r.GetDecimal(7), r.GetInt32(8) == 1, r.GetInt32(9) == 1, L(r, 10), L(r, 11)), ct, V()),
            BankAccounts = await OptionalAsync(cn,
                "select hbj_seq, bankcode, bnkbrhcode, cod_hes, des_hes, first_value, hes_seq, knd_hes from hesabjari where vahedcode = :v",
                r => new OldBankAccount(r.GetInt64(0), S(r, 1), S(r, 2), S(r, 3), S(r, 4), D(r, 5), r.GetInt64(6), S(r, 7)), ct, V()),
            CheckBooks = await OptionalAsync(cn,
                "select cbh_seq, hbj_seq, book_title, book_date, chkfrs, chkend, cb_kind, cheque_type from checkbooksinhesabjari where vahedcode = :v",
                r => new OldCheckBook(r.GetInt64(0), r.GetInt64(1), S(r, 2), S(r, 3), r.GetInt64(4), r.GetInt64(5), S(r, 6), L(r, 7)), ct, V()),
            Checks = await OptionalAsync(cn,
                "select pob_seq, cbh_seq, cheq_no, cheq_date, paper_desc, date_rsid, ebtal, payto, print from paperofbooks where vahedcode = :v",
                r => new OldCheck(r.GetInt64(0), r.GetInt64(1), S(r, 2), S(r, 3), S(r, 4), S(r, 5), S(r, 6), S(r, 7), S(r, 8)), ct, V()),
            Receipts = await OptionalAsync(cn,
                "select cid_seq, cheq_kind, cheq_date, cheq_no, date_rsid from checksindocs where vahedcode = :v",
                r => new OldReceipt(r.GetInt64(0), S(r, 1), S(r, 2), S(r, 3), S(r, 4)), ct, V()),
            BankCards = await OptionalAsync(cn,
                "select bcd_seq, cid_seq, pob_seq, bankcode, bnkbrhcode, cod_hes, month, cheqno, recivdate, bcd_type, amou_bed, amou_bes, seq_incor " +
                "from bankcartdetail where vahedcode = :v",
                r => new OldBankCard(r.GetInt64(0), L(r, 1), L(r, 2), S(r, 3), S(r, 4), S(r, 5), S(r, 6), S(r, 7), S(r, 8), S(r, 9),
                    D(r, 10), D(r, 11), L(r, 12)), ct, V()),
            Incorrents = await OptionalAsync(cn,
                "select seq_incor, no_doc, date_doc, cheq_no, cheq_date, paper_desc, payto, amou_bes, date_rsid, cod_hes from cheques_incorrent where vahedcode = :v",
                r => new OldIncorrent(r.GetInt64(0), S(r, 1), S(r, 2), S(r, 3), S(r, 4), S(r, 5), S(r, 6), D(r, 7), S(r, 8), S(r, 9)), ct, V()),
            AttribDefs = await OptionalAsync(cn,
                "select afm_seq, hes_seq, attribboxno, flag, lenatr, attribsum, controlid from attribformaincode where vahedcode = :v",
                r => new OldAttribDef(r.GetInt64(0), r.GetInt64(1), Convert.ToInt32(r.GetValue(2)), S(r, 3), Convert.ToInt32(r.GetValue(4)), S(r, 5), S(r, 6)), ct, V()),
            AttribValues = await OptionalAsync(cn, "select det_seq, afm_seq, attributevalue from attribsindocs where vahedcode = :v",
                r => new OldAttribValue(r.GetInt64(0), r.GetInt64(1), S(r, 2)), ct, V()),
            Elams = await OptionalAsync(cn,
                // select * و خواندن با نام: ستون‌های کارگاه (ELAMH_WSHCODE/NAME) در اسکیماهای قدیمی‌تر (مثل ۱۳۹۸) نیست
                "select * from elamhead where vahedcode = :v",
                r =>
                {
                    string N(string col) { var i = Ord(r, col); return i < 0 ? "" : S(r, i); }
                    long? NL(string col) { var i = Ord(r, col); return i < 0 ? null : L(r, i); }
                    return new OldElam(NL("ELH_SEQ")!.Value, NL("DOC_SEQ"), N("ELAMH_SERIALNO"), N("ELAMH_CODE"), N("ELAMH_ENO"), N("ELAMH_EDATE"),
                        N("ELAMH_SRVAHED"), N("ELAMH_YY"), NL("ELAMH_PRNNO"), N("ELAMH_CASE"), N("SERIALNO_INPUT"), N("WEB_STAT"), N("ELAMH_DATE"),
                        N("ELAMH_DESC"), N("ELAMH_RCVNO"), N("ELAMH_RCVDT"), N("ELAMH_LSTMON"), N("PAY_NO"), N("ZFLAG"), N("PEIMAN_NO"),
                        N("ELAMH_WSHCODE"), N("ELAMH_WSHNAME"), NL("BNH_SEQ"));
                }, ct, V()),
            Workshops = await OptionalAsync(cn, "select * from acc_brch where vahedcode = :v", r =>
            {
                string N(string col) { var i = Ord(r, col); return i < 0 ? "" : S(r, i); }
                long? NL(string col) { var i = Ord(r, col); return i < 0 ? null : L(r, i); }
                return new OldWorkshop(NL("BNH_SEQ")!.Value, N("BRHCODE"), N("WSHNAME"), N("WSHCODE"), N("COD_KOL"), N("COD_MOI"), N("COD_TAF"), N("STAT"), NL("HES_SEQ"));
            }, ct, V()),
            ElamLines = await OptionalAsync(cn,
                "select eld_seq, hes_seq, elh_seq, amou_bed, amou_bes, elamd_desc, elam_atrib_no, jr_seq from elamdetail where vahedcode = :v",
                r => new OldElamLine(r.GetInt64(0), L(r, 1), r.GetInt64(2), D(r, 3), D(r, 4), S(r, 5), S(r, 6), L(r, 7)), ct, V()),
            ChequeTypes = await OptionalAsync(cn, "select * from cheque_type where vahedcode = :v", r =>
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < r.FieldCount; i++) row[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
                return row;
            }, ct, V()),
        };
        y.CodeBySeq = y.Codes.ToDictionary(c => c.Seq);
        y.LowBySeq = y.Lows.ToDictionary(l => l.Seq);
        return y;
    }

    public string KolName(string kol) => Codes.FirstOrDefault(c => c.IsKol && c.Kol == kol)?.Name ?? "";
    public string MoeinName(string kol, string moi) => Codes.FirstOrDefault(c => c.IsMoein && c.Kol == kol && c.Moi == moi)?.Name ?? "";

    public (string Kol, string Moi)? MoeinOf(long hesSeq)
        => CodeBySeq.TryGetValue(hesSeq, out var c) && c.Moi != "" ? (c.Kol, c.Moi) : null;

    public Dictionary<long, (int Lines, decimal Bed, decimal Bes)> TotalsByHes()
        => Lines.GroupBy(l => l.HesSeq).ToDictionary(g => g.Key, g => (g.Count(), g.Sum(x => x.Bed), g.Sum(x => x.Bes)));

    /// <summary>
    /// همهٔ معین‌هایی که دادهٔ منتقل‌شدنی این سال به آن‌ها ارجاع دارد — ردیف سند، فعالیت/مرکز هزینه، ردیف اعلامیه، تعریف
    /// ویژگی، کارگاه و حساب بانکی. هر جدول تازه‌ای که منتقل شود باید این‌جا اضافه شود تا معینش در کدینگ ساخته شود.
    /// </summary>
    public HashSet<(string Kol, string Moi)> UsedMoeins()
    {
        var hes = Lines.Select(l => l.HesSeq)
            .Concat(LowLinks.Select(l => l.HesSeq))
            .Concat(ElamLines.Where(l => l.HesSeq != null).Select(l => l.HesSeq!.Value))
            .Concat(AttribDefs.Select(a => a.HesSeq))
            .Concat(BankAccounts.Select(a => a.HesSeq))
            .Concat(Workshops.Where(w => w.HesSeq != null).Select(w => w.HesSeq!.Value));
        var used = hes.Distinct().Select(MoeinOf).Where(m => m != null).Select(m => m!.Value).ToHashSet();
        foreach (var w in Workshops.Where(w => w.Kol != "" && w.Moi != "")) used.Add((w.Kol, w.Moi));
        return used;
    }

    public HashSet<(string, string)> MoeinsWithLow()
        => LowLinks.Select(l => MoeinOf(l.HesSeq)).Where(m => m != null).Select(m => m!.Value).ToHashSet();

    private static string S(OracleDataReader r, int i) => r.IsDBNull(i) ? "" : Convert.ToString(r.GetValue(i))!.Trim();
    private static int Ord(OracleDataReader r, string col)
    {
        for (var i = 0; i < r.FieldCount; i++) if (string.Equals(r.GetName(i), col, StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }
    private static long? L(OracleDataReader r, int i) => r.IsDBNull(i) ? null : Convert.ToInt64(r.GetValue(i));
    private static decimal D(OracleDataReader r, int i) => r.IsDBNull(i) ? 0 : Convert.ToDecimal(r.GetValue(i));

    /// <summary>جدولی که در اسکیمای قدیمی‌تر نیست (ORA-00942) خالی حساب می‌شود.</summary>
    private static async Task<List<T>> OptionalAsync<T>(OracleConnection cn, string sql, Func<OracleDataReader, T> map,
        CancellationToken ct, params OracleParameter[] ps)
    {
        try { return await ListAsync(cn, sql, map, ct, ps); }
        catch (OracleException ex) when (ex.Number == 942) { return new(); }
        catch (OracleException ex) when (ex.Number == 904)
        {
            // ستونی که در این اسکیما نیست: بی‌صدا خالی حساب نشود (اعلامیه‌های ۱۳۹۸ این‌طور جا ماندند)
            throw new InvalidDataException($"ساختار جدول سیستم قدیم با انتظار نمی‌خواند ({ex.Message.Split('\n')[0]}) در: {sql[..Math.Min(80, sql.Length)]}");
        }
    }

    internal static async Task<List<T>> ListAsync<T>(OracleConnection cn, string sql, Func<OracleDataReader, T> map,
        CancellationToken ct, params OracleParameter[] ps)
    {
        await using var cmd = new OracleCommand(sql, cn) { BindByName = true };
        cmd.Parameters.AddRange(ps);
        await using var r = (OracleDataReader)await cmd.ExecuteReaderAsync(ct);
        var list = new List<T>();
        while (await r.ReadAsync(ct)) list.Add(map(r));
        return list;
    }
}

/// <summary>همهٔ سال‌های یک واحد، با کلید پایدار تفصیلی‌ها در سراسر سال‌ها.</summary>
public sealed class LegacyUnit
{
    public required string VahedCode { get; init; }
    public required List<OldYear> Years { get; init; }

    /// <summary>
    /// کلید تفصیلی = «کل/معین/تفصیلی»؛ اگر همان کد در سال‌های مختلف نام متفاوت داشته باشد، «~سال» پسوند می‌گیرد.
    /// </summary>
    public void AssignTafsiliKeys()
    {
        var nameByCode = new Dictionary<string, string>();
        foreach (var y in Years.OrderBy(y => y.Year))
            foreach (var c in y.Codes.Where(c => c.IsTafsili))
            {
                var code = $"{c.Kol}/{c.Moi}/{c.Taf}";
                var norm = LegacyText.Norm(c.Name);
                if (!nameByCode.TryGetValue(code, out var first)) { nameByCode[code] = norm; y.TafsiliKey[c.Seq] = code; }
                else y.TafsiliKey[c.Seq] = first == norm ? code : $"{code}~{y.Year}";
            }
    }
}

/// <summary>
/// یافتن اسکیماهای سالانهٔ یک واحد: اگر کاربر اتصال دستی داده باشد همان‌ها، وگرنه جستجو روی سرورهای تنظیمات سامانه
/// با الگوی نام اسکیما.
/// </summary>
public sealed class LegacySourceLocator(LegacyImportOptions options)
{
    /// <param name="SetSchema">کاربر ورود با اسکیما فرق دارد ⇒ پس از ورود <c>CURRENT_SCHEMA</c> روی اسکیمای سال گذاشته می‌شود.</param>
    public sealed record Source(string Server, string Schema, int Year, string Connection, bool SetSchema = false);

    private static readonly System.Text.RegularExpressions.Regex OracleName = new("^[A-Za-z][A-Za-z0-9_$#]{0,127}$");

    public static async Task<OracleConnection> OpenAsync(Source s, CancellationToken ct)
    {
        var cn = new OracleConnection(s.Connection);
        try
        {
            await cn.OpenAsync(ct);
            if (s.SetSchema)
            {
                if (!OracleName.IsMatch(s.Schema)) throw new InvalidOperationException($"نام اسکیمای «{s.Schema}» معتبر نیست.");
                await using var cmd = new OracleCommand($"alter session set current_schema = \"{s.Schema.ToUpperInvariant()}\"", cn);
                await cmd.ExecuteNonQueryAsync(ct);
            }
            return cn;
        }
        catch
        {
            await cn.DisposeAsync();
            throw;
        }
    }

    /// <summary>OracleConnectionStringBuilder مقدارها را درست نقل‌قول می‌کند تا آدرس یا رمز نتواند رشتهٔ اتصال را بشکند.</summary>
    public string ConnectionString(string dataSource, string user, string password)
        => new OracleConnectionStringBuilder
        {
            DataSource = dataSource,
            UserID = user,
            Password = password,
            ConnectionTimeout = options.ConnectTimeoutSeconds,
            Pooling = false,
        }.ConnectionString;

    /// <summary>اتصال‌های داده‌شده را می‌آزماید (ورود + خواندن تعداد سند).</summary>
    public async Task<(List<Source> Found, List<LegacySourceDto> Report)> ProbeAsync(IEnumerable<Source> sources, string vahed, CancellationToken ct)
    {
        var found = new List<Source>();
        var report = new List<LegacySourceDto>();
        foreach (var s in sources)
        {
            var dto = await ProbeOneAsync(s, vahed, ct);
            report.Add(dto);
            if (dto.Connected) found.Add(s);
        }
        return (found.OrderBy(f => f.Year).ToList(), report.OrderBy(r => r.Year).ToList());
    }

    public async Task<LegacySourceDto> ProbeOneAsync(Source s, string vahed, CancellationToken ct)
    {
        try
        {
            await using var cn = await OpenAsync(s, ct);
            var docs = (await OldYear.ListAsync(cn, "select count(*) from docshead where vahedcode = :v",
                r => Convert.ToInt32(r.GetValue(0)), ct, new OracleParameter("v", vahed)))[0];
            return new LegacySourceDto(s.Server, s.Schema, s.Year, true, null, docs);
        }
        catch (OracleException ex)
        {
            var msg = ex.Number switch
            {
                1017 => "نام کاربر یا رمز نادرست است",
                942 => "جدول‌های سیستم قدیم در این اسکیما نیست (یا کاربر به آن دسترسی ندارد)",
                1435 => "اسکیمای این سال روی سرور نیست",
                12154 or 12514 or 12541 or 12170 or 12545 => "سرور در دسترس نیست یا آدرس نادرست است",
                28000 => "کاربر قفل شده است",
                _ => ex.Message.Split('\n')[0],
            };
            return new LegacySourceDto(s.Server, s.Schema, s.Year, false, $"{msg} (ORA-{ex.Number:00000})", 0);
        }
    }

    /// <summary>
    /// جستجو با الگوی نام روی سرورهای تنظیمات. نخستین اسکیمایی که وصل شود فهرست کاربران (ALL_USERS) را می‌دهد تا
    /// برای اسکیمای ناموجود تلاش ورود تکرار نشود (ورود ناموفق مکرر روی کاربر موجود ممکن است آن را قفل کند).
    /// </summary>
    public async Task<(List<Source> Found, List<LegacySourceDto> Report)> DiscoverAsync(string vahed, CancellationToken ct)
    {
        var found = new List<Source>();
        var report = new List<LegacySourceDto>();
        var candidates = Enumerable.Range(options.FromYear, Math.Max(0, options.ToYear - options.FromYear + 1))
            .Select(y => (Year: y, Schema: options.SchemaPattern.Replace("{vahed}", vahed).Replace("{yy}", (y % 100).ToString("00")).ToUpperInvariant()))
            .ToList();

        foreach (var server in options.Servers)
        {
            HashSet<string>? existing = null;
            foreach (var (year, schema) in candidates)
            {
                if (existing != null && !existing.Contains(schema)) continue;
                var source = new Source(server.Name, schema, year, ConnectionString(server.DataSource, schema, server.Password ?? options.Password));
                if (existing == null)
                {
                    try
                    {
                        await using var cn = new OracleConnection(source.Connection);
                        await cn.OpenAsync(ct);
                        existing = (await OldYear.ListAsync(cn, "select username from all_users", r => r.GetString(0), ct)).ToHashSet();
                    }
                    catch (OracleException ex) when (ex.Number == 1017) { continue; } // اسکیما نیست (یا رمز نادرست)
                    catch (OracleException ex)
                    {
                        report.Add(new LegacySourceDto(server.Name, schema, year, false, $"سرور در دسترس نیست (ORA-{ex.Number:00000})", 0));
                        break;
                    }
                }
                var dto = await ProbeOneAsync(source, vahed, ct);
                report.Add(dto);
                if (dto.Connected) found.Add(source);
            }
        }
        return (found.OrderBy(f => f.Year).ToList(), report.OrderBy(r => r.Year).ToList());
    }

    public async Task<LegacyUnit> LoadUnitAsync(string vahed, IReadOnlyList<Source>? manual, CancellationToken ct)
    {
        var (found, report) = manual is { Count: > 0 } ? await ProbeAsync(manual, vahed, ct) : await DiscoverAsync(vahed, ct);
        var failed = report.Where(r => !r.Connected).ToList();
        if (found.Count == 0 || (manual is { Count: > 0 } && failed.Count > 0))
            throw new Accounting.Application.Common.Exceptions.BusinessRuleException(found.Count == 0
                ? $"هیچ اسکیمای سیستم قدیم برای واحد {vahed} وصل نشد؛ اتصال‌ها را بررسی کنید."
                : "اتصال به این اسکیماها ممکن نشد: " + string.Join("، ", failed.Select(f => $"{f.Year} ({f.Error})")));
        var years = new List<OldYear>();
        foreach (var s in found)
            years.Add(await OldYear.LoadAsync(s, vahed, ct));
        var unit = new LegacyUnit { VahedCode = vahed, Years = years };
        unit.AssignTafsiliKeys();
        return unit;
    }
}
