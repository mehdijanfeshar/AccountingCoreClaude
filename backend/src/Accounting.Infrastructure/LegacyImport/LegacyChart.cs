using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;
using K = Accounting.Infrastructure.LegacyImport.LegacyWorkbookKit;

namespace Accounting.Infrastructure.LegacyImport;

internal sealed record TreeNode(Guid Id, TypeCodes? Type, string Code, string Name, bool Deleted);

/// <summary>
/// کاربرگ کدینگ مشترک (گروه/کل/معین) — یک‌بار برای همهٔ واحدها و بدون کد واحد. شمارش‌ها روی همهٔ واحدها و
/// سال‌های داده‌شده جمع زده می‌شوند. شمارهٔ ستون‌ها را <see cref="ChartMapping"/> هم می‌خواند.
/// </summary>
internal static class ChartWorkbook
{
    public const string KolSheet = "تطبیق کل", MoeinSheet = "تطبیق معین", GroupSheet = "گروه‌ها", LifeSheet = "وضعیت سند", TreeSheet = "درخت جدید";
    public const int K_Old = 1, K_Final = 10, K_Control = 12, K_DefName = 13;
    public const int M_Kol = 1, M_Moi = 2, M_Name = 4, M_Act = 5, M_Hes = 6, M_Required = 14, M_Final = 19, M_Control = 21, M_DefName = 22;
    public const int G_Code = 1, G_Name = 3, G_Control = 4;
    public const int L_Old = 1, L_New = 6, L_Control = 7;
    public const string LifeSkip = "منتقل نشود";

    public static async Task<List<TreeNode>> LoadTreeAsync(LegacyDbContext db, CancellationToken ct)
        => (await db.TB_ACCOUNTCODEs.AsNoTracking()
                .Select(a => new { a.ID, a.TYPECODE, a.ACCCODE, a.ACCCODENAME, a.ISDELETED }).ToListAsync(ct))
            .Where(a => !string.IsNullOrWhiteSpace(a.ACCCODE))
            .Select(a => new TreeNode(a.ID, a.TYPECODE, a.ACCCODE!.Trim(), a.ACCCODENAME?.Trim() ?? "", a.ISDELETED == true))
            .OrderBy(a => a.Code, StringComparer.Ordinal).ToList();

    private sealed class MoeinAgg(MainCode first)
    {
        public MainCode First { get; } = first;
        public SortedSet<string> Units { get; } = new();
        public HashSet<string> Names { get; } = new();
        public int Lines, Tafsili;
        public bool Low, Used;
        public decimal Bed, Bes;
        /// <summary>هر ارجاعی از دادهٔ منتقل‌شدنی (OldYear.UsedMoeins) — نه فقط ردیف سند.</summary>
        public bool Required => Lines > 0 || Low || Used;
    }

    public static byte[] Build(IReadOnlyList<LegacyUnit> units, List<TreeNode> fullTree)
    {
        var tree = fullTree.Where(t => !t.Deleted && !LegacyText.IsTest(t.Name)).ToList();
        var testRows = fullTree.Count(t => !t.Deleted) - tree.Count;
        var byCode = tree.GroupBy(t => t.Code).ToDictionary(g => g.Key, g => g.First());
        var k = new K();

        var kolName = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var kolUnits = new Dictionary<string, SortedSet<string>>();
        var moi = new Dictionary<(string Kol, string Moi), MoeinAgg>();
        var lifeDocs = new List<(string Life, string Year, int Lines)>();
        foreach (var u in units)
            foreach (var y in u.Years)
            {
                var totals = y.TotalsByHes();
                var withLow = y.MoeinsWithLow();
                var used = y.UsedMoeins();
                foreach (var c in y.Codes.Where(c => c.IsKol))
                {
                    kolName.TryAdd(c.Kol, c.Name);
                    (kolUnits.TryGetValue(c.Kol, out var set) ? set : kolUnits[c.Kol] = new()).Add(u.VahedCode);
                }
                foreach (var m in y.Codes.Where(c => c.IsMoein))
                {
                    var key = (m.Kol, m.Moi);
                    if (!moi.TryGetValue(key, out var a)) moi[key] = a = new MoeinAgg(m);
                    a.Units.Add(u.VahedCode);
                    a.Names.Add(LegacyText.Clean(m.Name));
                    foreach (var c in y.Codes.Where(c => c.Kol == m.Kol && c.Moi == m.Moi))
                    {
                        if (c.IsTafsili) a.Tafsili++;
                        if (totals.TryGetValue(c.Seq, out var t)) { a.Lines += t.Lines; a.Bed += t.Bed; a.Bes += t.Bes; }
                    }
                    a.Low |= withLow.Contains(key);
                    a.Used |= used.Contains(key);
                }
                var linesByDoc = y.Lines.GroupBy(l => l.DocSeq).ToDictionary(g => g.Key, g => g.Count());
                lifeDocs.AddRange(y.Docs.Select(d => (d.Life, d.DocYear, linesByDoc.GetValueOrDefault(d.Seq))));
            }

        // ----- تطبیق کل -----
        var ws = k.Sheet(KolSheet,
            ["کل قدیم", "نام کل قدیم", "تعداد معین", "تعداد ردیف سند", "جمع بدهکار (ریال)", "جمع بستانکار (ریال)",
             "کل جدید پیشنهادی (XY→X0Y0)", "نام همین کد در درخت جدید", "شباهت نام",
             "✎ کل جدید نهایی", "نام کل نهایی (خودکار)", "کنترل (خودکار)", "✎ نام برای تعریف (اگر کد جدید است)", "✎ توضیح کارشناس", "واحدها"],
            [8, 34, 9, 10, 18, 18, 14, 30, 8, 12, 30, 30, 26, 30, 20]);
        var row = 2;
        foreach (var (kol, name) in kolName)
        {
            var under = moi.Where(x => x.Key.Kol == kol).Select(x => x.Value).ToList();
            var nk = LegacyText.NewKol(kol);
            byCode.TryGetValue(nk, out var hit);
            var isKol = hit?.Type == TypeCodes.Kol;
            var sim = isKol ? LegacyText.Sim(name, hit!.Name) : (double?)null;
            ws.Cell(row, 1).SetValue(kol); ws.Cell(row, 2).Value = name; ws.Cell(row, 3).Value = under.Count;
            ws.Cell(row, 4).Value = under.Sum(a => a.Lines); ws.Cell(row, 5).Value = under.Sum(a => a.Bed); ws.Cell(row, 6).Value = under.Sum(a => a.Bes);
            ws.Cell(row, 7).SetValue(nk);
            ws.Cell(row, 8).Value = isKol ? hit!.Name : "— در درخت نیست؛ با نام قدیم ساخته می‌شود —";
            if (sim != null) ws.Cell(row, 9).Value = Math.Round(sim.Value, 2);
            ws.Cell(row, 10).Style.NumberFormat.Format = "@";
            ws.Cell(row, 10).SetValue(nk);
            ws.Cell(row, 11).FormulaA1 = $"IF(J{row}=\"\",\"\",IFERROR(INDEX('{TreeSheet}'!$C:$C,MATCH(J{row}&\"\",'{TreeSheet}'!$B:$B,0)),\"(ساخته می‌شود) \"&M{row}))";
            ws.Cell(row, 13).FormulaA1 = $"IF(J{row}=\"\",\"\",IF(COUNTIFS('{TreeSheet}'!$B:$B,J{row}&\"\",'{TreeSheet}'!$A:$A,\"کل\")=0,B{row},\"\"))";
            var conflict = sim < 0.6 ? "TRUE" : "FALSE";
            ws.Cell(row, 12).FormulaA1 =
                $"IF(J{row}=\"\",IF(D{row}>0,\"الزامی — تکمیل نشده\",\"\")," +
                $"IF(AND(J{row}=G{row},{conflict},N{row}=\"\"),\"تداخل: کد \"&J{row}&\" در برگهٔ درخت جدید با نام «\"&K{row}&\"» از قبل تعریف شده\"," +
                $"IF(COUNTIFS('{TreeSheet}'!$B:$B,J{row}&\"\",'{TreeSheet}'!$A:$A,\"کل\")=0,IF(M{row}=\"\",\"کد در درخت نیست — نام برای تعریف را بنویسید\",\"OK\"),\"OK\")))";
            ws.Cell(row, 15).Value = string.Join("، ", kolUnits.GetValueOrDefault(kol) ?? new());
            row++;
        }
        ws.Range(2, 9, Math.Max(row - 1, 2), 9).Style.NumberFormat.Format = "0%";
        K.Finish(ws, row - 1, 15, [10, 13, 14], [7, 8, 9], [5, 6], 12);
        var kolLast = Math.Max(row - 1, 2);

        // ----- تطبیق معین -----
        var wm = k.Sheet(MoeinSheet,
            ["کل قدیم", "معین قدیم", "نام کل قدیم", "نام معین قدیم", "نوع فعلیت قدیم", "نوع حساب قدیم", "—",
             "تعداد تفصیلی", "—", "تعداد ردیف سند", "جمع بدهکار (ریال)", "جمع بستانکار (ریال)", "مانده (ریال)", "الزامی؟",
             "کل جدید (از برگهٔ تطبیق کل)", "معین پیشنهادی (کل جدید + دو رقم معین قدیم)", "نام همین کد در درخت جدید", "شباهت نام",
             "✎ معین جدید نهایی", "نام معین نهایی (خودکار)", "کنترل (خودکار)", "✎ نام برای تعریف (اگر کد جدید است)", "✎ توضیح کارشناس",
             "فعالیت/مرکز هزینه دارد؟", "واحدها", "نام‌های دیگر همین معین در واحدها"],
            [7, 7, 24, 34, 8, 8, 3, 8, 3, 9, 17, 17, 17, 7, 10, 11, 30, 8, 12, 30, 30, 26, 30, 10, 20, 34]);
        row = 2;
        foreach (var ((kol, mo), a) in moi.OrderByDescending(x => x.Value.Required).ThenBy(x => x.Key.Kol).ThenBy(x => x.Key.Moi))
        {
            byCode.TryGetValue(LegacyText.NewKol(kol) + mo, out var same);
            var sim = same?.Type == TypeCodes.Moin ? LegacyText.Sim(a.First.Name, same.Name) : (double?)null;
            wm.Cell(row, 1).SetValue(kol); wm.Cell(row, 2).SetValue(mo);
            wm.Cell(row, 3).Value = kolName.GetValueOrDefault(kol, "");
            wm.Cell(row, 4).Value = a.First.Name;
            wm.Cell(row, 5).SetValue(a.First.KindAct); wm.Cell(row, 6).SetValue(a.First.KindHes);
            wm.Cell(row, 8).Value = a.Tafsili; wm.Cell(row, 10).Value = a.Lines;
            wm.Cell(row, 11).Value = a.Bed; wm.Cell(row, 12).Value = a.Bes;
            wm.Cell(row, 13).FormulaA1 = $"K{row}-L{row}";
            wm.Cell(row, 14).Value = a.Required ? "بله" : "خیر";
            wm.Cell(row, 15).FormulaA1 = $"IFERROR(INDEX('{KolSheet}'!$J:$J,MATCH(A{row}&\"\",'{KolSheet}'!$A:$A,0))&\"\",\"\")";
            wm.Cell(row, 16).FormulaA1 = $"IF(O{row}=\"\",\"\",O{row}&B{row})";
            wm.Cell(row, 17).FormulaA1 = $"IF(P{row}=\"\",\"\",IFERROR(INDEX('{TreeSheet}'!$C:$C,MATCH(P{row}&\"\",'{TreeSheet}'!$B:$B,0)),\"— در درخت نیست؛ با نام قدیم ساخته می‌شود —\"))";
            if (sim != null) wm.Cell(row, 18).Value = Math.Round(sim.Value, 2);
            wm.Cell(row, 19).Style.NumberFormat.Format = "@";
            wm.Cell(row, 19).FormulaA1 = $"P{row}";
            wm.Cell(row, 20).FormulaA1 = $"IF(S{row}=\"\",\"\",IFERROR(INDEX('{TreeSheet}'!$C:$C,MATCH(S{row}&\"\",'{TreeSheet}'!$B:$B,0)),\"(ساخته می‌شود) \"&V{row}))";
            var conflict = sim < 0.6 ? "TRUE" : "FALSE";
            wm.Cell(row, 21).FormulaA1 =
                $"IF(S{row}=\"\",IF(N{row}=\"بله\",\"الزامی — تکمیل نشده\",\"\")," +
                $"IF(AND(O{row}<>\"\",LEFT(S{row}&\"\",4)<>O{row}),\"زیر کل دیگری است — کل یا معین را اصلاح کنید\"," +
                $"IF(AND(S{row}=P{row},{conflict},W{row}=\"\"),\"تداخل: کد \"&S{row}&\" در برگهٔ درخت جدید با نام «\"&T{row}&\"» از قبل تعریف شده\"," +
                $"IF(COUNTIFS('{TreeSheet}'!$B:$B,S{row}&\"\",'{TreeSheet}'!$A:$A,\"معین\")=0,IF(V{row}=\"\",\"کد در درخت نیست — نام برای تعریف را بنویسید\",\"OK\"),\"OK\"))))";
            wm.Cell(row, 22).FormulaA1 = $"IF(S{row}=\"\",\"\",IF(COUNTIFS('{TreeSheet}'!$B:$B,S{row}&\"\",'{TreeSheet}'!$A:$A,\"معین\")=0,D{row},\"\"))";
            if (a.Low) wm.Cell(row, 24).Value = "بله";
            wm.Cell(row, 25).Value = string.Join("، ", a.Units);
            var others = a.Names.Where(n => LegacyText.Norm(n) != LegacyText.Norm(a.First.Name)).ToList();
            if (others.Count > 0) wm.Cell(row, 26).Value = string.Join(" | ", others);
            row++;
        }
        wm.Range(2, 18, Math.Max(row - 1, 2), 18).Style.NumberFormat.Format = "0%";
        K.Finish(wm, row - 1, 26, [19, 22, 23], [15, 16, 17, 18], [11, 12, 13], 21);
        var moiLast = Math.Max(row - 1, 2);

        // ----- گروه‌ها (سیستم قدیم سطح گروه ندارد) -----
        var wg = k.Sheet(GroupSheet, ["کد گروه", "نام در درخت جدید", "✎ نام گروه", "کنترل (خودکار)", "کل‌های قدیم زیر این گروه"], [8, 34, 34, 22, 50]);
        row = 2;
        foreach (var g in kolName.Keys.GroupBy(kol => LegacyText.NewKol(kol)[..2]).OrderBy(g => g.Key))
        {
            byCode.TryGetValue(g.Key, out var hit);
            var real = hit?.Type == TypeCodes.Group ? hit.Name : null;
            wg.Cell(row, 1).SetValue(g.Key);
            wg.Cell(row, 2).Value = real ?? "— در درخت نیست یا آزمایشی است —";
            if (real != null) wg.Cell(row, 3).Value = real;
            wg.Cell(row, 4).FormulaA1 = $"IF(C{row}=\"\",\"نام گروه را بنویسید\",\"OK\")";
            wg.Cell(row, 5).Value = string.Join("، ", g.Select(kol => $"{kol} {kolName[kol]}"));
            row++;
        }
        K.Finish(wg, row - 1, 5, [3], [2], [], 4);
        var grpLast = Math.Max(row - 1, 2);

        // ----- وضعیت سند -----
        var wl = k.Sheet(LifeSheet,
            ["وضعیت قدیم (DOCLIFE)", "تعداد سند", "تعداد ردیف", "سال‌ها", "حدس اولیه (تأیید نشده)", "✎ وضعیت در سیستم جدید", "کنترل (خودکار)", "✎ توضیح کارشناس"],
            [12, 10, 10, 30, 34, 24, 16, 34]);
        row = 2;
        foreach (var g in lifeDocs.GroupBy(x => x.Life).OrderBy(g => g.Key))
        {
            wl.Cell(row, 1).SetValue(g.Key);
            wl.Cell(row, 2).Value = g.Count(); wl.Cell(row, 3).Value = g.Sum(x => x.Lines);
            wl.Cell(row, 4).Value = string.Join("، ", g.GroupBy(x => x.Year).OrderBy(y => y.Key).Select(y => $"{y.Key} ({y.Count()})"));
            wl.Cell(row, 5).Value = g.Key switch { "0" => "احتمالاً یادداشت/ناقص", "2" => "احتمالاً موقت", "3" => "احتمالاً قطعی/دائم", _ => "نامشخص" };
            wl.Cell(row, 7).FormulaA1 = $"IF(F{row}=\"\",\"تکمیل نشده\",\"OK\")";
            row++;
        }
        if (row > 2) wl.Range(2, 6, row - 1, 6).CreateDataValidation().List($"\"1 - یادداشت,2 - موقت,3 - بررسی‌شده,4 - تأیید دائم,{LifeSkip}\"", true);
        K.Finish(wl, row - 1, 8, [6, 8], [5], [], 7);

        // ----- درخت جدید -----
        var wt = k.Sheet(TreeSheet, ["سطح", "کد", "نام", "کد والد"], [8, 10, 46, 10]);
        row = 2;
        foreach (var t in tree)
        {
            wt.Cell(row, 1).Value = t.Type switch { TypeCodes.Group => "گروه", TypeCodes.Kol => "کل", TypeCodes.Moin => "معین", _ => "" };
            wt.Cell(row, 2).SetValue(t.Code);
            wt.Cell(row, 3).Value = t.Name;
            wt.Cell(row, 4).SetValue(t.Type == TypeCodes.Kol && t.Code.Length >= 2 ? t.Code[..2] : t.Type == TypeCodes.Moin && t.Code.Length >= 4 ? t.Code[..4] : "");
            row++;
        }
        K.Finish(wt, row - 1, 4, [], [], [], 0);

        // ----- راهنما -----
        k.G("کدینگ مشترک (گروه / کل / معین) — یک‌بار برای همهٔ واحدها", null, true, 14);
        k.G("منبع", string.Join("، ", units.SelectMany(u => u.Years.Select(y => $"{y.Schema} ({y.Year})"))) + $" — {DateTime.Now:yyyy-MM-dd}");
        k.G("مقصد", $"{testRows} ردیف آزمایشی درخت جدید (مثل test و تست) کنار گذاشته شده و در تطبیق حساب نمی‌شود.");
        k.Gap();
        k.Legend();
        k.G("قواعد تبدیل", null, true, 11);
        k.G("کل و معین", "کل جدید = X0Y0 از کل قدیم XY؛ معین جدید = کل جدید + دو رقم معین قدیم. کد ناموجود با نام قدیم ساخته می‌شود. این کدینگ بدون کد واحد و برای همهٔ واحدها یکسان است.");
        k.Gap();
        k.G("ترتیب کار", null, true, 11);
        k.G("۱. تطبیق کل", "کل جدید از قبل پر شده. «تداخل» یعنی این کد در درخت جدید با نام دیگری هست: کد را عوض کنید یا در توضیح بنویسید که تأیید است.");
        k.G("۲. تطبیق معین", "معین جدید از قبل پر شده و با تغییر کل خودش به‌روز می‌شود. فقط ردیف‌های «تداخل» بررسی لازم دارند.");
        k.G("۳. گروه‌ها", "سیستم قدیم سطح گروه ندارد. برای گروه‌هایی که در درخت جدید نیستند (یا فقط ردیف آزمایشی دارند) نام بنویسید.");
        k.G("۴. وضعیت سند", "معادل وضعیت‌های سیستم قدیم را انتخاب کنید (برای همهٔ واحدها).");
        k.Gap();
        k.G("پیشرفت کار", null, true, 11);
        k.Progress("کل — ردیف OK", $"COUNTIF('{KolSheet}'!$L$2:$L${kolLast},\"OK\")&\" از \"&COUNTA('{KolSheet}'!$A$2:$A${kolLast})");
        k.Progress("معین الزامی — OK", $"COUNTIFS('{MoeinSheet}'!$N$2:$N${moiLast},\"بله\",'{MoeinSheet}'!$U$2:$U${moiLast},\"OK\")&\" از \"&COUNTIF('{MoeinSheet}'!$N$2:$N${moiLast},\"بله\")");
        k.Progress("گروه — OK", $"COUNTIF('{GroupSheet}'!$D$2:$D${grpLast},\"OK\")&\" از \"&COUNTA('{GroupSheet}'!$A$2:$A${grpLast})");
        k.Progress("وضعیت سند — تعیین‌شده", $"COUNTIF('{LifeSheet}'!$G:$G,\"OK\")&\" از \"&(COUNTA('{LifeSheet}'!$A:$A)-1)");
        return k.ToBytes();
    }
}

internal sealed record KolMap(string OldKol, string Final, string DefName, string OldName, string Control, int Row);

internal sealed record MoeinMap(string OldKol, string OldMoi, string OldName, string Final, string DefName,
    string KindAct, string KindHes, bool Required, string Control, int Row);

/// <summary>تصمیم‌های کاربرگ کدینگ مشترک.</summary>
internal sealed class ChartMapping
{
    public List<KolMap> Kols { get; } = new();
    public Dictionary<(string Kol, string Moi), MoeinMap> Moeins { get; } = new();
    public Dictionary<string, (string Name, string Control, int Row)> Groups { get; } = new();

    /// <summary>وضعیت قدیم ← وضعیت جدید؛ null یعنی «منتقل نشود».</summary>
    public Dictionary<string, DocLife?> DocLife { get; } = new();
    public List<string> DocLifeProblems { get; } = new();

    /// <summary>
    /// همین تصمیم‌ها، ولی فقط معین‌های <paramref name="keys"/> «الزامی»؛ برای ساختن معینی که در کاربرگ ثبت‌شده کد
    /// جدید دارد ولی چون آن زمان فقط ردیف سند ملاک الزامی‌بودن بود، ساخته نشده است.
    /// </summary>
    public ChartMapping WithRequired(ISet<(string Kol, string Moi)> keys)
    {
        var m = new ChartMapping();
        m.Kols.AddRange(Kols);
        foreach (var (k, v) in Moeins) m.Moeins[k] = v with { Required = keys.Contains(k) };
        foreach (var (k, v) in Groups) m.Groups[k] = v;
        foreach (var (k, v) in DocLife) m.DocLife[k] = v;
        return m;
    }

    public static ChartMapping Read(byte[] bytes)
    {
        using var wb = K.Open(bytes);
        var m = new ChartMapping();
        foreach (var r in K.DataRows(K.SheetOrThrow(wb, ChartWorkbook.KolSheet)))
            m.Kols.Add(new KolMap(K.V(r.Cell(ChartWorkbook.K_Old)), K.V(r.Cell(ChartWorkbook.K_Final)),
                LegacyText.Clean(K.V(r.Cell(ChartWorkbook.K_DefName))), K.V(r.Cell(2)), K.V(r.Cell(ChartWorkbook.K_Control)), r.RowNumber()));
        foreach (var r in K.DataRows(K.SheetOrThrow(wb, ChartWorkbook.MoeinSheet)))
        {
            var mm = new MoeinMap(K.V(r.Cell(ChartWorkbook.M_Kol)), K.V(r.Cell(ChartWorkbook.M_Moi)), K.V(r.Cell(ChartWorkbook.M_Name)),
                K.V(r.Cell(ChartWorkbook.M_Final)), LegacyText.Clean(K.V(r.Cell(ChartWorkbook.M_DefName))),
                K.V(r.Cell(ChartWorkbook.M_Act)), K.V(r.Cell(ChartWorkbook.M_Hes)),
                K.V(r.Cell(ChartWorkbook.M_Required)) == "بله", K.V(r.Cell(ChartWorkbook.M_Control)), r.RowNumber());
            m.Moeins[(mm.OldKol, mm.OldMoi)] = mm;
        }
        foreach (var r in K.DataRows(K.SheetOrThrow(wb, ChartWorkbook.GroupSheet)))
            m.Groups[K.V(r.Cell(ChartWorkbook.G_Code))] = (LegacyText.Clean(K.V(r.Cell(ChartWorkbook.G_Name))), K.V(r.Cell(ChartWorkbook.G_Control)), r.RowNumber());
        foreach (var r in K.DataRows(K.SheetOrThrow(wb, ChartWorkbook.LifeSheet)))
        {
            var old = K.V(r.Cell(ChartWorkbook.L_Old));
            var choice = K.V(r.Cell(ChartWorkbook.L_New));
            if (choice == "") { m.DocLifeProblems.Add($"وضعیت سند قدیم «{old}» تعیین نشده (برگهٔ {ChartWorkbook.LifeSheet})"); continue; }
            if (choice == ChartWorkbook.LifeSkip) { m.DocLife[old] = null; continue; }
            if (int.TryParse(choice.Split('-')[0].Trim(), out var v) && Enum.IsDefined(typeof(DocLife), v)) m.DocLife[old] = (DocLife)v;
            else m.DocLifeProblems.Add($"وضعیت «{choice}» برای وضعیت قدیم «{old}» نامعتبر است");
        }
        return m;
    }
}

/// <summary>
/// ساخت درخت مشترک گروه/کل/معین. تکرارپذیر: کد موجود دوباره ساخته نمی‌شود؛ ردیف آزمایشی یا حذف‌شدهٔ هم‌کد
/// بازاستفاده می‌شود (UK_ACCOUNTCODE روی ACCCODE است، حتی برای ردیف حذف‌شده).
/// </summary>
internal sealed class ChartImporter(LegacyDbContext db, ChartMapping map, string userId, ImportReport report)
{
    private readonly DateTime _now = DateTime.UtcNow;
    private readonly HashSet<string> _seen = new();
    private Dictionary<string, TB_ACCOUNTCODE> _byCode = new();
    private int _created, _reused, _existing;

    public bool Validate()
    {
        foreach (var m in Needed())
        {
            if (m.Control != "OK")
                report.Errors.Add($"برگهٔ {ChartWorkbook.MoeinSheet} ردیف {m.Row} ({m.OldKol}/{m.OldMoi}): {(m.Control == "" ? "تکمیل نشده" : m.Control)}");
            if (m.Final.Length != 6 || !m.Final.All(char.IsAsciiDigit))
                report.Errors.Add($"برگهٔ {ChartWorkbook.MoeinSheet} ردیف {m.Row}: کد معین «{m.Final}» باید ۶ رقم باشد");
        }
        var kolByFinal = KolByFinal();
        foreach (var kolCode in Needed().Where(m => m.Final.Length == 6).Select(m => m.Final[..4]).Distinct())
            if (kolByFinal.TryGetValue(kolCode, out var k) && k.Control != "OK")
                report.Errors.Add($"برگهٔ {ChartWorkbook.KolSheet} ردیف {k.Row} (کل {k.OldKol}): {(k.Control == "" ? "تکمیل نشده" : k.Control)}");
        foreach (var g in Needed().Where(m => m.Final.Length == 6).Select(m => m.Final[..2]).Distinct())
            if (map.Groups.TryGetValue(g, out var gm) && gm.Control != "OK")
                report.Errors.Add($"برگهٔ {ChartWorkbook.GroupSheet} ردیف {gm.Row} (گروه {g}): {gm.Control}");
        report.Errors.AddRange(map.DocLifeProblems);
        return report.Ok;
    }

    private List<MoeinMap> Needed() => map.Moeins.Values.Where(m => m.Final != "" && m.Required).ToList();
    private Dictionary<string, KolMap> KolByFinal() => map.Kols.Where(k => k.Final != "").GroupBy(k => k.Final).ToDictionary(g => g.Key, g => g.First());

    public async Task RunAsync(CancellationToken ct)
    {
        var needed = Needed();
        var kolByFinal = KolByFinal();
        _byCode = (await db.TB_ACCOUNTCODEs.ToListAsync(ct))
            .Where(a => !string.IsNullOrWhiteSpace(a.ACCCODE)).GroupBy(a => a.ACCCODE!.Trim()).ToDictionary(g => g.Key, g => g.First());

        foreach (var m in needed)
        {
            var kolCode = m.Final[..4];
            var groupCode = m.Final[..2];
            var groupName = map.Groups.TryGetValue(groupCode, out var gm) ? gm.Name : "";
            if (groupName == "" && !_byCode.ContainsKey(groupCode))
            {
                report.Errors.Add($"گروه {groupCode} (برای معین {m.Final}) در درخت جدید نیست؛ نامش را در برگهٔ {ChartWorkbook.GroupSheet} بنویسید");
                continue;
            }
            var groupId = Ensure(groupCode, TypeCodes.Group, null, groupName,
                MajorityKindHes(needed.Where(x => x.Final.StartsWith(groupCode, StringComparison.Ordinal))), null, groupName != "");
            if (groupId == null) continue;

            var kolRow = kolByFinal.GetValueOrDefault(kolCode);
            var kolName = kolRow == null ? "" : kolRow.DefName != "" ? kolRow.DefName : kolRow.OldName;
            if (kolName == "" && !_byCode.ContainsKey(kolCode))
            {
                report.Errors.Add($"کل {kolCode} (برای معین {m.Final}) نه در درخت جدید هست و نه در برگهٔ {ChartWorkbook.KolSheet}");
                continue;
            }
            var kolId = Ensure(kolCode, TypeCodes.Kol, groupId, kolName, null, null, kolRow?.DefName is { Length: > 0 });
            if (kolId == null) continue;

            Ensure(m.Final, TypeCodes.Moin, kolId, m.DefName != "" ? m.DefName : m.OldName, null,
                int.TryParse(m.KindAct, out var ka) && ka is >= 1 and <= 3 ? (TypeActivity)ka : null, m.DefName != "");
        }
        if (!report.Ok) return;
        await db.SaveChangesAsync(ct);
        report.Info.Add($"حساب‌ها: {_created} ساخته شد، {_reused} ردیف آزمایشی/حذف‌شده بازاستفاده شد، {_existing} از قبل وجود داشت.");
    }

    private Guid? Ensure(string code, TypeCodes type, Guid? parentId, string name, TypeAccCode? typeAccCode, TypeActivity? activity, bool isNewName)
    {
        name = LegacyText.Cut(LegacyText.Clean(name), 200);
        if (!_seen.Add(code)) return _byCode.TryGetValue(code, out var done) ? done.ID : null;
        if (_byCode.TryGetValue(code, out var e))
        {
            if (e.TYPECODE != type)
            {
                report.Errors.Add($"کد {code} در درخت جدید سطح دیگری ({e.TYPECODE}) دارد؛ نمی‌توان آن را {type} گرفت");
                return null;
            }
            if (e.ISDELETED == true || LegacyText.IsTest(e.ACCCODENAME))
            {
                report.Warnings.Add($"بازاستفادهٔ ردیف {(e.ISDELETED == true ? "حذف‌شده" : "آزمایشی")} {code} «{e.ACCCODENAME}» ← «{name}»");
                e.ACCCODENAME = name;
                e.ISDELETED = false;
                e.PARENTID = parentId;
                e.CHANGEUSERID = userId;
                e.UPDATEDDATE = _now;
                if (activity != null) e.TYPEACTIVITY = activity;
                _reused++;
            }
            else
            {
                if (e.PARENTID != parentId) report.Warnings.Add($"کد {code} در درخت جدید والد دیگری دارد؛ دست‌نخورده ماند");
                _existing++;
            }
            return e.ID;
        }

        var n = new TB_ACCOUNTCODE
        {
            ID = Guid.NewGuid(), TYPECODE = type, PARENTID = parentId, ACCCODE = code, ACCCODENAME = name,
            TYPEACTIVITY = activity,
            TYPEACCCODE = type == TypeCodes.Group ? typeAccCode : null,
            TYPEACTION = type == TypeCodes.Moin ? TypeAction.NotControlled : null,
            ADDUSERID = userId, CREATEDDATE = _now, ISDELETED = false,
        };
        db.TB_ACCOUNTCODEs.Add(n);
        _byCode[code] = n;
        _created++;
        return n.ID;
    }

    private static TypeAccCode? MajorityKindHes(IEnumerable<MoeinMap> moeins)
    {
        var top = moeins.Select(m => m.KindHes).Where(k => k is "1" or "2").GroupBy(k => k).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key;
        return top == null ? null : (TypeAccCode)int.Parse(top);
    }
}
