using K = Accounting.Infrastructure.LegacyImport.LegacyWorkbookKit;

namespace Accounting.Infrastructure.LegacyImport;

/// <summary>
/// کاربرگ مخصوص یک واحد (همهٔ سال‌هایش): تفصیلی‌ها با ستون نوع شخص و کد ملی/شناسهٔ ملی، فعالیت/مرکز هزینه،
/// اسناد ناتراز و اسناد بیرون از سال اسکیما. شمارهٔ ستون‌ها را <see cref="UnitMapping"/> هم می‌خواند.
/// </summary>
internal static class LegacyUnitWorkbook
{
    public const string TafSheet = "تفصیلی‌ها", LowSheet = "فعالیت و مرکز هزینه", UnbalSheet = "اسناد ناتراز", OutSheet = "اسناد بیرون از سال";

    public const string ActNew = "ایجاد تفصیلی جدید", ActMerge = "ادغام با تفصیلی دیگر", ActNone = "بدون تفصیلی (ثبت روی خود معین)";
    public const string Natural = "حقیقی", Legal = "حقوقی", Other = "سایر";
    public const string LowMove = "منتقل شود", LowSkip = "منتقل نشود";
    public const string UFix = "در سیستم قدیم اصلاح و تراز می‌شود", UDraft = "به‌صورت یادداشت منتقل شود", USkip = "منتقل نشود";
    public const string OutMove = "در سال تاریخ خودش منتقل شود", OutSkip = "منتقل نشود";

    // تفصیلی‌ها
    public const int T_Key = 1, T_Kol = 2, T_Moi = 3, T_Name = 5, T_Act = 14, T_Merge = 15, T_Person = 16, T_NationalId = 17,
        T_Level = 18, T_Group = 19, T_NewName = 20, T_Control = 21, T_FinalCode = 23;
    // فعالیت و مرکز هزینه
    public const int L_Key = 1, L_Name = 6, L_Act = 13, L_NewName = 14, L_Control = 15, L_FinalCode = 17;
    // اسناد ناتراز / بیرون از سال
    public const int U_Key = 1, U_Act = 12, U_Control = 13;
    public const int O_Key = 1, O_Act = 8, O_Control = 9;

    public static string DocKey(OldYear y, OldDoc d) => $"{y.Year}:{d.Seq}";

    private sealed class TafRow
    {
        public required string Key, Kol, Moi, Taf, Name, MoeinName, KolName;
        public SortedSet<int> Years { get; } = new();
        public int Lines; public decimal Bed, Bes; public bool HasLow;
    }

    public static byte[] Build(LegacyUnit u)
    {
        var k = new K();

        // ----- تفصیلی‌ها: فقط آن‌هایی که در یکی از سال‌ها سند دارند -----
        var rows = new Dictionary<string, TafRow>();
        foreach (var y in u.Years)
        {
            var totals = y.TotalsByHes();
            var withLow = y.MoeinsWithLow();
            foreach (var c in y.Codes.Where(c => c.IsTafsili && totals.ContainsKey(c.Seq)))
            {
                var key = y.TafsiliKey[c.Seq];
                if (!rows.TryGetValue(key, out var r))
                    rows[key] = r = new TafRow { Key = key, Kol = c.Kol, Moi = c.Moi, Taf = c.Taf, Name = c.Name,
                        MoeinName = y.MoeinName(c.Kol, c.Moi), KolName = y.KolName(c.Kol) };
                var t = totals[c.Seq];
                r.Years.Add(y.Year); r.Lines += t.Lines; r.Bed += t.Bed; r.Bes += t.Bes;
                r.HasLow |= withLow.Contains((c.Kol, c.Moi));
            }
        }
        var ordered = rows.Values.OrderBy(r => r.Kol).ThenBy(r => r.Moi).ThenBy(r => r.Taf).ThenBy(r => r.Key).ToList();
        // «000000» = خود معین بدون تفصیلی و نامش نام معین است؛ نه مقصد ادغام است نه با تفصیلیِ هم‌نامِ معین یکی می‌شود
        var sameName = ordered.Where(r => r.Taf != "000000" && LegacyText.Norm(r.Name).Length >= 3)
            .GroupBy(r => LegacyText.Norm(r.Name)).ToDictionary(g => g.Key, g => g.ToList());

        var wt = k.Sheet(TafSheet,
            ["کلید", "کل قدیم", "معین قدیم", "کد تفصیلی قدیم", "نام تفصیلی قدیم", "نام معین قدیم", "سال‌ها",
             "تعداد ردیف سند", "جمع بدهکار (ریال)", "جمع بستانکار (ریال)", "تعداد هم‌نام", "اقدام پیشنهادی", "ادغام پیشنهادی با کلید",
             "✎ اقدام نهایی", "✎ ادغام با کلید", "✎ نوع شخص (حقیقی / حقوقی / سایر)", "✎ کد ملی (حقیقی، ۱۰ رقم) / شناسهٔ ملی (حقوقی، ۱۱ رقم)",
             "✎ سطح شناور جدید (۱ تا ۷)", "✎ گروه تفصیلی (پیش‌فرض: نام کل قدیم)", "✎ نام تفصیلی جدید (خالی = همان نام قدیم)",
             "کنترل (خودکار)", "✎ توضیح", "کد تفصیلی نهایی (سیستم پر می‌کند)"],
            [14, 6, 6, 9, 34, 26, 10, 8, 17, 17, 7, 24, 14, 26, 14, 12, 16, 10, 28, 28, 28, 24, 14]);
        var row = 2;
        foreach (var t in ordered)
        {
            var norm = LegacyText.Norm(t.Name);
            var group = sameName.GetValueOrDefault(norm);
            string act; string? target = null;
            if (t.Taf == "000000") act = ActNone;
            else if (norm.Length < 3) { act = ActNew; wt.Cell(row, 22).Value = "نام تفصیلی در سیستم قدیم خالی/نامعتبر است — نام جدید را بنویسید"; }
            else if (group is { Count: > 1 } && group[0].Key != t.Key) { act = ActMerge; target = group[0].Key; }
            else act = ActNew;

            wt.Cell(row, 1).SetValue(t.Key); wt.Cell(row, 2).SetValue(t.Kol); wt.Cell(row, 3).SetValue(t.Moi); wt.Cell(row, 4).SetValue(t.Taf);
            wt.Cell(row, 5).Value = t.Name; wt.Cell(row, 6).Value = t.MoeinName; wt.Cell(row, 7).Value = string.Join("، ", t.Years);
            wt.Cell(row, 8).Value = t.Lines; wt.Cell(row, 9).Value = t.Bed; wt.Cell(row, 10).Value = t.Bes;
            wt.Cell(row, 11).Value = group?.Count ?? 1;
            wt.Cell(row, 12).Value = act; if (target != null) wt.Cell(row, 13).SetValue(target);
            wt.Cell(row, 14).Value = act; if (target != null) wt.Cell(row, 15).SetValue(target);
            wt.Cell(row, 15).Style.NumberFormat.Format = "@";
            wt.Cell(row, 17).Style.NumberFormat.Format = "@";
            // سطح ۱ و ۲ برای فعالیت و مرکز هزینه رزرو است؛ اگر معین آن‌ها را دارد، تفصیلی قدیم به سطح ۳ می‌رود
            if (act != ActNone) wt.Cell(row, 18).Value = t.HasLow ? 3 : 1;
            if (act == ActNew) wt.Cell(row, 19).Value = LegacyText.Clean(t.KolName);
            var hl = t.HasLow ? "TRUE" : "FALSE";
            // رقم کنترل بدون آرایهٔ ثابت تا در Excel و در ClosedXML (که سرور با آن فایل بارگذاری‌شده را می‌خواند) یکسان
            // محاسبه شود. MID در ClosedXML برای شروع ≥ ۱۰ رشتهٔ خالی می‌دهد، پس رقم‌های ۱۰ و ۱۱ از RIGHT خوانده می‌شوند.
            var q = $"Q{row}&\"\"";
            string D(int i, int len) => i <= 9 ? $"VALUE(MID({q},{i},1))" : $"VALUE(LEFT(RIGHT({q},{len - i + 1}),1))";
            var natSum = string.Join("+", Enumerable.Range(1, 9).Select(i => $"{D(i, 10)}*{11 - i}"));
            var nat = $"IFERROR(IF(LEN({q})<>10,FALSE,IF(MOD({natSum},11)<2,{D(10, 10)}=MOD({natSum},11),{D(10, 10)}=11-MOD({natSum},11))),FALSE)";
            int[] w = [29, 27, 23, 19, 17, 29, 27, 23, 19, 17];
            var legSum = string.Join("+", Enumerable.Range(1, 10).Select(i => $"({D(i, 11)}+{D(10, 11)}+2)*{w[i - 1]}"));
            var leg = $"IFERROR(IF(LEN({q})<>11,FALSE,MOD(MOD({legSum},11),10)={D(11, 11)}),FALSE)";
            wt.Cell(row, 21).FormulaA1 =
                $"IF(N{row}=\"\",\"تکمیل نشده\",IF(N{row}=\"{ActNone}\",\"OK\",IF(N{row}=\"{ActMerge}\",IF(O{row}=\"\",\"کلید مقصد خالی است\"," +
                $"IF(AND(ISNA(MATCH(O{row}&\"\",$A:$A,0)),ISNA(MATCH(SUBSTITUTE(O{row}&\"\",\"~\",\"~~\"),$A:$A,0))),\"کلید مقصد در این فهرست نیست\",IF(O{row}&\"\"=A{row}&\"\",\"ادغام با خودش\",\"OK\")))," +
                $"IF(R{row}=\"\",\"سطح شناور خالی است\",IF(S{row}=\"\",\"گروه تفصیلی خالی است\"," +
                $"IF(AND({hl},OR(R{row}=1,R{row}=2)),\"سطح ۱ و ۲ این معین مال فعالیت و مرکز هزینه است\"," +
                $"IF(P{row}=\"{Natural}\",IF({nat},\"OK\",\"کد ملی نامعتبر است\")," +
                $"IF(P{row}=\"{Legal}\",IF({leg},\"OK\",\"شناسهٔ ملی نامعتبر است\"),\"OK\"))))))))";
            row++;
        }
        if (row > 2)
        {
            wt.Range(2, 14, row - 1, 14).CreateDataValidation().List($"\"{ActNew},{ActMerge},{ActNone}\"", true);
            wt.Range(2, 16, row - 1, 16).CreateDataValidation().List($"\"{Natural},{Legal},{Other}\"", true);
            wt.Range(2, 18, row - 1, 18).CreateDataValidation().List("\"1,2,3,4,5,6,7\"", true);
            wt.Range(2, 23, row - 1, 23).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#EEEEEE");
        }
        K.Finish(wt, row - 1, 23, [14, 15, 16, 17, 18, 19, 20, 22], [12, 13], [9, 10], 21);
        var tafLast = Math.Max(row - 1, 2);

        // ----- فعالیت و مرکز هزینه -----
        var lows = new Dictionary<string, (LowCode Low, SortedSet<int> Years, int Lines, HashSet<string> Links, string Parent)>();
        foreach (var y in u.Years)
        {
            var usedJr = y.Lines.Where(l => l.JrSeq != null).GroupBy(l => l.JrSeq!.Value).ToDictionary(g => g.Key, g => g.Count());
            var actName = y.Lows.Where(l => !l.IsCostCenter).GroupBy(l => l.Joz).ToDictionary(g => g.Key, g => g.First().Name);
            foreach (var l in y.Lows)
            {
                if (!lows.TryGetValue(l.Key, out var e))
                    e = (l, new SortedSet<int>(), 0, new HashSet<string>(), l.IsCostCenter ? actName.GetValueOrDefault(l.Joz, "— جزء والد تعریف نشده —") : "");
                e.Years.Add(y.Year);
                e.Lines += usedJr.GetValueOrDefault(l.Seq);
                foreach (var (hes, jr) in y.LowLinks.Where(x => x.JrSeq == l.Seq))
                    if (y.CodeBySeq.TryGetValue(hes, out var c)) e.Links.Add($"{c.Kol}/{c.Moi}");
                lows[l.Key] = e;
            }
        }
        var wl = k.Sheet(LowSheet,
            ["کلید (جزء/ریز)", "نوع", "سطح شناور جدید", "کد جزء", "کد ریز", "نام", "فعالیت والد", "سال‌ها", "تعداد ردیف سند",
             "تعداد معین متصل", "معین‌های قدیم متصل", "اقدام پیشنهادی", "✎ اقدام نهایی", "✎ نام جدید (خالی = همان نام قدیم)",
             "کنترل (خودکار)", "✎ توضیح", "کد تفصیلی نهایی (سیستم پر می‌کند)"],
            [12, 10, 8, 8, 11, 34, 24, 10, 8, 8, 40, 16, 16, 26, 18, 24, 14]);
        row = 2;
        foreach (var (key, e) in lows.OrderBy(x => x.Value.Low.IsCostCenter).ThenBy(x => x.Key, StringComparer.Ordinal))
        {
            wl.Cell(row, 1).SetValue(key);
            wl.Cell(row, 2).Value = e.Low.IsCostCenter ? "مرکز هزینه" : "فعالیت";
            wl.Cell(row, 3).Value = e.Low.IsCostCenter ? 2 : 1;
            wl.Cell(row, 4).SetValue(e.Low.Joz); wl.Cell(row, 5).SetValue(e.Low.Riz); wl.Cell(row, 6).Value = e.Low.Name;
            wl.Cell(row, 7).Value = e.Parent; wl.Cell(row, 8).Value = string.Join("، ", e.Years);
            wl.Cell(row, 9).Value = e.Lines; wl.Cell(row, 10).Value = e.Links.Count; wl.Cell(row, 11).Value = string.Join("، ", e.Links.Order());
            var a = e.Lines > 0 || e.Links.Count > 0 ? LowMove : LowSkip;
            wl.Cell(row, 12).Value = a; wl.Cell(row, 13).Value = a;
            wl.Cell(row, 15).FormulaA1 = $"IF(M{row}=\"\",\"تکمیل نشده\",IF(AND(M{row}=\"{LowSkip}\",I{row}>0),\"در سند استفاده شده\",\"OK\"))";
            row++;
        }
        if (row > 2)
        {
            wl.Range(2, 13, row - 1, 13).CreateDataValidation().List($"\"{LowMove},{LowSkip}\"", true);
            wl.Range(2, 17, row - 1, 17).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#EEEEEE");
        }
        K.Finish(wl, row - 1, 17, [13, 14, 16], [12], [], 15);
        var lowLast = Math.Max(row - 1, 2);

        // ----- اسناد ناتراز -----
        var wu = k.Sheet(UnbalSheet,
            ["کلید (سال:شناسه)", "اسکیما", "شمارهٔ سند", "تاریخ", "وضعیت قدیم", "نوع سیستم", "شرح سند", "تعداد ردیف",
             "جمع بدهکار (ریال)", "جمع بستانکار (ریال)", "اختلاف (ریال)", "✎ اقدام", "کنترل (خودکار)", "✎ توضیح"],
            [14, 11, 9, 11, 8, 8, 40, 8, 17, 17, 15, 30, 18, 24]);
        row = 2;
        foreach (var y in u.Years)
        {
            var byDoc = y.Lines.GroupBy(l => l.DocSeq).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var d in y.Docs.OrderBy(d => d.Date).ThenBy(d => d.No))
            {
                var ls = byDoc.GetValueOrDefault(d.Seq) ?? new();
                if (ls.Sum(x => x.Bed) == ls.Sum(x => x.Bes)) continue;
                wu.Cell(row, 1).SetValue(DocKey(y, d)); wu.Cell(row, 2).Value = y.Schema; wu.Cell(row, 3).SetValue(d.No);
                wu.Cell(row, 4).SetValue(LegacyText.Date8(d.Date)); wu.Cell(row, 5).SetValue(d.Life); wu.Cell(row, 6).SetValue(d.SysType);
                wu.Cell(row, 7).Value = d.Desc; wu.Cell(row, 8).Value = ls.Count;
                wu.Cell(row, 9).Value = ls.Sum(x => x.Bed); wu.Cell(row, 10).Value = ls.Sum(x => x.Bes);
                wu.Cell(row, 11).FormulaA1 = $"I{row}-J{row}";
                wu.Cell(row, 13).FormulaA1 = $"IF(L{row}=\"\",\"تکمیل نشده\",\"OK\")";
                row++;
            }
        }
        if (row > 2) wu.Range(2, 12, row - 1, 12).CreateDataValidation().List($"\"{UFix},{UDraft},{USkip}\"", true);
        K.Finish(wu, row - 1, 14, [12, 14], [], [9, 10, 11], 13);
        var unbal = row - 2;

        // ----- اسناد بیرون از سال اسکیما -----
        var wo = k.Sheet(OutSheet,
            ["کلید (سال:شناسه)", "اسکیما (سال)", "شمارهٔ سند", "تاریخ", "تعداد ردیف", "جمع بدهکار (ریال)", "شرح سند", "✎ اقدام", "کنترل (خودکار)", "✎ توضیح"],
            [14, 16, 9, 11, 8, 17, 40, 28, 16, 24]);
        row = 2;
        foreach (var y in u.Years)
        {
            var byDoc = y.Lines.GroupBy(l => l.DocSeq).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var d in y.Docs.Where(d => d.DocYear != y.Year.ToString()).OrderBy(d => d.Date))
            {
                var ls = byDoc.GetValueOrDefault(d.Seq) ?? new();
                wo.Cell(row, 1).SetValue(DocKey(y, d)); wo.Cell(row, 2).Value = $"{y.Schema} ({y.Year})"; wo.Cell(row, 3).SetValue(d.No);
                wo.Cell(row, 4).SetValue(LegacyText.Date8(d.Date)); wo.Cell(row, 5).Value = ls.Count; wo.Cell(row, 6).Value = ls.Sum(x => x.Bed);
                wo.Cell(row, 7).Value = d.Desc; wo.Cell(row, 8).Value = OutMove;
                wo.Cell(row, 9).FormulaA1 = $"IF(H{row}=\"\",\"تکمیل نشده\",\"OK\")";
                row++;
            }
        }
        if (row > 2) wo.Range(2, 8, row - 1, 8).CreateDataValidation().List($"\"{OutMove},{OutSkip}\"", true);
        K.Finish(wo, row - 1, 10, [8, 10], [], [6], 9);
        var outCount = row - 2;

        // ----- راهنما -----
        k.G($"کدینگ واحد {u.VahedCode} در سیستم قدیم — برای تعریف در سیستم جدید", null, true, 14);
        k.G("سال‌ها (اسکیما)", string.Join("، ", u.Years.Select(y => $"{y.Year} ({y.Schema} روی {y.Server}، {y.Docs.Count} سند)")));
        k.G("کدینگ کل و معین", "برای همهٔ واحدها مشترک است و جدا تعریف شده؛ اینجا فقط تفصیلی‌ها و داده‌های همین واحد است.");
        k.Gap();
        k.Legend();
        k.G("کار شما", null, true, 11);
        k.G("۱. اشخاص", "برای هر تفصیلی که شخص است «نوع شخص» را حقیقی یا حقوقی بگذارید و کد ملی (۱۰ رقم) یا شناسهٔ ملی (۱۱ رقم) را بنویسید. کد ملی همان کد تفصیلی در سیستم جدید می‌شود؛ اگر این شخص در واحد دیگری قبلاً تعریف شده باشد، همان تفصیلی استفاده می‌شود و فقط برای واحد شما فعال می‌شود. چند ردیف با یک کد ملی هم یکی می‌شوند.");
        k.G("۲. ادغام", "تفصیلی‌های هم‌نام از قبل «ادغام» پیشنهاد شده‌اند؛ اگر دو شخص متفاوت‌اند، اقدام را «ایجاد تفصیلی جدید» کنید.");
        k.G("۳. فعالیت و مرکز هزینه", "جزء = فعالیت (سطح ۱)، ریز = مرکز هزینه (سطح ۲). هرکدام که به معینی وصل است یا در سند آمده، پیش‌فرض «منتقل شود» است.");
        k.G("۴. اسناد", "برای اسناد ناتراز و اسناد بیرون از سال اسکیما یک اقدام انتخاب کنید. بقیهٔ اسناد بعد از تعریف کدینگ خودکار منتقل می‌شوند.");
        k.G("بارگذاری", "وقتی همهٔ ستون‌های کنترل OK شد، همین فایل را در صفحهٔ «انتقال از سیستم قدیم» بارگذاری کنید؛ اول «بررسی» و بعد «ثبت».");
        k.Gap();
        k.G("پیشرفت کار", null, true, 11);
        k.Progress("تفصیلی — OK", $"COUNTIF('{TafSheet}'!$U$2:$U${tafLast},\"OK\")&\" از \"&COUNTA('{TafSheet}'!$A$2:$A${tafLast})");
        k.Progress("اشخاص با کد ملی/شناسه", $"COUNTIF('{TafSheet}'!$P$2:$P${tafLast},\"{Natural}\")+COUNTIF('{TafSheet}'!$P$2:$P${tafLast},\"{Legal}\")");
        k.Progress("فعالیت و مرکز هزینه — OK", $"COUNTIF('{LowSheet}'!$O$2:$O${lowLast},\"OK\")&\" از \"&COUNTA('{LowSheet}'!$A$2:$A${lowLast})");
        k.Progress("اسناد ناتراز — تعیین‌تکلیف‌شده", $"COUNTIF('{UnbalSheet}'!$M:$M,\"OK\")&\" از {unbal}\"");
        k.Progress("اسناد بیرون از سال — تعیین‌تکلیف‌شده", $"COUNTIF('{OutSheet}'!$I:$I,\"OK\")&\" از {outCount}\"");
        return k.ToBytes();
    }
}

internal sealed record TafMap(string Key, string Kol, string Moi, string OldName, string Act, string MergeKey, string Person, string NationalId,
    int? Level, string Group, string NewName, string Control, int Row);

internal sealed record LowMap(string Key, string OldName, string Act, string NewName, string Control, int Row);

/// <summary>تصمیم‌های کاربرگ یک واحد.</summary>
internal sealed class UnitMapping
{
    public Dictionary<string, TafMap> Tafsili { get; } = new();
    public Dictionary<string, LowMap> Lows { get; } = new();
    public Dictionary<string, (string Act, string Control, int Row)> Unbalanced { get; } = new();
    public Dictionary<string, (string Act, string Control, int Row)> OutOfYear { get; } = new();

    /// <summary>پس از ثبت کدینگ واحد: کلید تفصیلی ← کد نهایی (ستون «سیستم پر می‌کند»).</summary>
    public Dictionary<string, string> TafsiliFinal { get; } = new();

    /// <summary>پس از ثبت کدینگ واحد: کلید فعالیت/مرکز هزینه ← کد نهایی تفصیلی.</summary>
    public Dictionary<string, string> LowFinal { get; } = new();

    public static UnitMapping Read(byte[] bytes)
    {
        using var wb = K.Open(bytes);
        var m = new UnitMapping();
        foreach (var r in K.DataRows(K.SheetOrThrow(wb, LegacyUnitWorkbook.TafSheet)))
        {
            var t = new TafMap(K.V(r.Cell(LegacyUnitWorkbook.T_Key)), K.V(r.Cell(LegacyUnitWorkbook.T_Kol)), K.V(r.Cell(LegacyUnitWorkbook.T_Moi)),
                K.V(r.Cell(LegacyUnitWorkbook.T_Name)), K.V(r.Cell(LegacyUnitWorkbook.T_Act)), K.V(r.Cell(LegacyUnitWorkbook.T_Merge)),
                K.V(r.Cell(LegacyUnitWorkbook.T_Person)), K.V(r.Cell(LegacyUnitWorkbook.T_NationalId)).Replace(" ", ""),
                int.TryParse(K.V(r.Cell(LegacyUnitWorkbook.T_Level)), out var lv) ? lv : null,
                LegacyText.Clean(K.V(r.Cell(LegacyUnitWorkbook.T_Group))), LegacyText.Clean(K.V(r.Cell(LegacyUnitWorkbook.T_NewName))),
                K.V(r.Cell(LegacyUnitWorkbook.T_Control)), r.RowNumber());
            m.Tafsili[t.Key] = t;
            if (K.V(r.Cell(LegacyUnitWorkbook.T_FinalCode)) is { Length: > 0 } tf) m.TafsiliFinal[t.Key] = tf;
        }
        foreach (var r in K.DataRows(K.SheetOrThrow(wb, LegacyUnitWorkbook.LowSheet)))
        {
            var l = new LowMap(K.V(r.Cell(LegacyUnitWorkbook.L_Key)), K.V(r.Cell(LegacyUnitWorkbook.L_Name)), K.V(r.Cell(LegacyUnitWorkbook.L_Act)),
                LegacyText.Clean(K.V(r.Cell(LegacyUnitWorkbook.L_NewName))), K.V(r.Cell(LegacyUnitWorkbook.L_Control)), r.RowNumber());
            m.Lows[l.Key] = l;
            if (K.V(r.Cell(LegacyUnitWorkbook.L_FinalCode)) is { Length: > 0 } lf) m.LowFinal[l.Key] = lf;
        }
        foreach (var r in K.DataRows(K.SheetOrThrow(wb, LegacyUnitWorkbook.UnbalSheet)))
            m.Unbalanced[K.V(r.Cell(LegacyUnitWorkbook.U_Key))] = (K.V(r.Cell(LegacyUnitWorkbook.U_Act)), K.V(r.Cell(LegacyUnitWorkbook.U_Control)), r.RowNumber());
        foreach (var r in K.DataRows(K.SheetOrThrow(wb, LegacyUnitWorkbook.OutSheet)))
            m.OutOfYear[K.V(r.Cell(LegacyUnitWorkbook.O_Key))] = (K.V(r.Cell(LegacyUnitWorkbook.O_Act)), K.V(r.Cell(LegacyUnitWorkbook.O_Control)), r.RowNumber());
        return m;
    }

    /// <summary>کد نهایی تفصیلی‌ها و فعالیت‌ها را در ستون «سیستم پر می‌کند» می‌نویسد (برای انتقال اسناد).</summary>
    public static byte[] WriteFinalCodes(byte[] bytes, IReadOnlyDictionary<string, string> tafsiliCodes, IReadOnlyDictionary<string, string> lowCodes)
    {
        using var wb = K.Open(bytes);
        foreach (var r in K.DataRows(wb.Worksheet(LegacyUnitWorkbook.TafSheet)))
            if (tafsiliCodes.TryGetValue(K.V(r.Cell(LegacyUnitWorkbook.T_Key)), out var c)) r.Cell(LegacyUnitWorkbook.T_FinalCode).SetValue(c);
        foreach (var r in K.DataRows(wb.Worksheet(LegacyUnitWorkbook.LowSheet)))
            if (lowCodes.TryGetValue(K.V(r.Cell(LegacyUnitWorkbook.L_Key)), out var c)) r.Cell(LegacyUnitWorkbook.L_FinalCode).SetValue(c);
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
