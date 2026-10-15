using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.LegacyImport;

/// <summary>
/// اطلاعات بانکی یک واحد از همهٔ سال‌های سیستم قدیم (یک‌بار برای واحد، تکرارپذیر): حساب جاری (HESABJARI ← TB_ACCOUNT)،
/// نوع چک (CHEQUE_TYPE)، دسته‌چک (CHECKBOOKSINHESABJARI) و برگ چک (PAPEROFBOOKS ← TB_CHECK). شناسه‌های قدیم در هر
/// اسکیمای سالانه فرق دارند، پس حساب با شمارهٔ حساب، دسته‌چک با (حساب، اولین شماره) و چک با (دسته‌چک، شمارهٔ چک) یکی
/// می‌شوند؛ وضعیت (ابطال/چاپ/وصول) از آخرین سال. موجود دوباره ساخته نمی‌شود، فقط به‌روز می‌شود.
/// </summary>
internal sealed class LegacyBankImporter(LegacyDbContext db, LegacyUnit u, ChartMapping chart, UnitMapping? unitMap, string userId, ImportReport report)
{
    private readonly DateTime _now = DateTime.UtcNow;
    private readonly string _v = u.VahedCode;

    /// <summary>حساب جاری در سیستم قدیم = «حساب جاری ریالی».</summary>
    private const string CurrentAccountType = "حساب جاري ريالي";

    public async Task<bool> RunAsync(CancellationToken ct)
    {
        var years = u.Years.OrderBy(y => y.Year).ToList();

        // ----- پایه: بانک، شعبه، نوع حساب، معین‌ها -----
        var banks = (await db.TB_BANK_LISTs.AsNoTracking().Select(b => new { b.ID, b.BANKCODE }).ToListAsync(ct))
            .GroupBy(b => b.BANKCODE.Trim()).ToDictionary(g => g.Key, g => g.First().ID);
        var branches = (await db.TB_BANKBRANCH_LISTs.AsNoTracking().Select(b => new { b.ID, b.BANK_ID, b.BRANCHCODE }).ToListAsync(ct))
            .GroupBy(b => (b.BANK_ID, b.BRANCHCODE.Trim())).ToDictionary(g => g.Key, g => g.First().ID); // کد شعبه حساس به حروف است
        var accountType = (await db.TB_ACCOUNT_TYPEs.AsNoTracking().Select(t => new { t.ID, t.TITLE }).ToListAsync(ct))
            .FirstOrDefault(t => LegacyText.Norm(t.TITLE) == LegacyText.Norm(CurrentAccountType))?.ID;
        var moeinIds = (await db.TB_ACCOUNTCODEs.AsNoTracking().Where(a => a.TYPECODE == TypeCodes.Moin && a.ISDELETED != true)
                .Select(a => new { a.ID, a.ACCCODE }).ToListAsync(ct))
            .Where(a => a.ACCCODE != null).GroupBy(a => a.ACCCODE!.Trim()).ToDictionary(g => g.Key, g => g.First().ID);

        // ----- حساب‌ها (آخرین سال برای نام/معین، اولین سال برای مانده اول) -----
        var existingAccounts = await db.TB_ACCOUNTs.Where(a => a.VAHEDCODE == _v && a.ISDELETED != true).ToListAsync(ct);
        var accountId = new Dictionary<string, Guid>();
        int newAccounts = 0, newBooks = 0, newChecks = 0, updatedChecks = 0, newTypes = 0;
        foreach (var g in years.SelectMany(y => y.BankAccounts.Select(a => (Year: y, Acc: a))).GroupBy(x => x.Acc.AccountNo))
        {
            var (latestYear, latest) = g.Last();
            var first = g.First().Acc;
            Guid? moein = latestYear.MoeinOf(latest.HesSeq) is { } m && chart.Moeins.TryGetValue(m, out var mm)
                          && moeinIds.TryGetValue(mm.Final, out var mid) ? mid : null;
            if (moein == null)
            {
                report.Errors.Add($"حساب جاری {g.Key} «{latest.Title}»: معین آن ({latestYear.MoeinOf(latest.HesSeq)}) در کدینگ جدید پیدا نشد");
                continue;
            }
            Guid? bank = banks.TryGetValue(latest.BankCode, out var bid) ? bid : null;
            Guid? branch = bank != null && branches.TryGetValue((bank.Value, latest.BranchCode), out var brid) ? brid : null;
            if (bank == null) report.Warnings.Add($"حساب {g.Key}: بانک با کد {latest.BankCode} در فهرست بانک‌ها نیست");
            else if (branch == null) report.Warnings.Add($"حساب {g.Key}: شعبهٔ {latest.BranchCode} بانک {latest.BankCode} در فهرست شعب نیست");

            var acc = existingAccounts.FirstOrDefault(a => a.ACCOUNTNUMBER.Trim() == g.Key);
            if (acc == null)
            {
                if (existingAccounts.FirstOrDefault(a => a.ACCOUNTCODE_ID == moein) is { } clash)
                {
                    report.Errors.Add($"حساب {g.Key}: معین آن در این واحد از قبل به حساب {clash.ACCOUNTNUMBER} وصل است (هر معین یک حساب بانکی)");
                    continue;
                }
                acc = new TB_ACCOUNT
                {
                    ID = Guid.NewGuid(), ACCOUNTNUMBER = LegacyText.Cut(g.Key, 80), ACCOUNTHOLDER = LegacyText.Cut(latest.Title, 100),
                    FIRSTAMOUNT = first.FirstValue, BANK_ID = bank, BRANCH_ID = branch, ACCOUNTTYPE_ID = accountType, ACCOUNTCODE_ID = moein,
                    VAHEDCODE = _v, ADDUSERID = userId, CREATEDDATE = _now, ISDELETED = false,
                };
                db.TB_ACCOUNTs.Add(acc);
                existingAccounts.Add(acc);
                newAccounts++;
            }
            accountId[g.Key] = acc.ID;
        }
        if (!report.Ok) return false;

        // ----- نوع چک (از آخرین سالی که دارد؛ با عنوان یکی می‌شود) -----
        var typeId = new Dictionary<long, Guid>();
        if (years.LastOrDefault(y => y.ChequeTypes.Count > 0) is { } typeYear)
        {
            var existingTypes = await db.TB_CHECK_TYPEs.Where(t => t.VAHEDCODE == _v && !t.ISDELETED).ToListAsync(ct);
            foreach (var row in typeYear.ChequeTypes)
            {
                var no = Convert.ToInt64(row["CHEQUE_TYPE"]);
                var title = LegacyText.Clean(Convert.ToString(row.GetValueOrDefault("CHEQUE_TYPE_TITLE")) ?? "");
                var t = existingTypes.FirstOrDefault(x => LegacyText.Norm(x.CHEQUE_TYPE_TITLE ?? "") == LegacyText.Norm(title));
                if (t == null)
                {
                    t = new TB_CHECK_TYPE
                    {
                        ID = Guid.NewGuid(), YEAR = typeYear.Year.ToString(), VAHEDCODE = _v, ADDUSERID = userId, CREATEDDATE = _now, ISDELETED = false,
                    };
                    CopyColumns(row, t);
                    db.TB_CHECK_TYPEs.Add(t);
                    existingTypes.Add(t);
                    newTypes++;
                }
                typeId[no] = t.ID;
            }
        }

        // ----- دسته‌چک‌ها -----
        var accIds = accountId.Values.ToList();
        var existingBooks = await db.TB_CHECKBOOKs.Where(b => accIds.Contains(b.ACCOUNT_ID) && b.VAHEDCODE == _v).ToListAsync(ct);
        var bookId = new Dictionary<(string, long), Guid>();
        foreach (var g in years.SelectMany(y => y.CheckBooks.Select(b => (Year: y, Book: b)))
                     .Select(x => (x.Year, x.Book, Acc: x.Year.BankAccounts.FirstOrDefault(a => a.Seq == x.Book.AccountSeq)))
                     .Where(x => x.Acc != null && accountId.ContainsKey(x.Acc.AccountNo))
                     .GroupBy(x => (x.Acc!.AccountNo, x.Book.From)))
        {
            var latest = g.Last().Book;
            var acc = accountId[g.Key.AccountNo];
            var (from, to) = (latest.From.ToString(), latest.To.ToString());
            var book = existingBooks.FirstOrDefault(b => b.ACCOUNT_ID == acc && b.FROMCHECKNUMBER.Trim() == from && b.TOCHECKNUMBER.Trim() == to);
            if (book == null)
            {
                book = new TB_CHECKBOOK
                {
                    ID = Guid.NewGuid(), ACCOUNT_ID = acc, FROMCHECKNUMBER = from, TOCHECKNUMBER = to, VAHEDCODE = _v,
                    ADDUSERID = userId, CREATEDDATE = _now, ISDELETED = false,
                };
                db.TB_CHECKBOOKs.Add(book);
                existingBooks.Add(book);
                newBooks++;
            }
            book.CHECKBOOK_TITLE = latest.Title == "" ? book.CHECKBOOK_TITLE : LegacyText.Cut(latest.Title, 100);
            book.CHECKBOOK_DATE = latest.Date;
            book.CHECKBOOK_TYPE = latest.Kind == "1" ? CheckType.Sori : CheckType.Real;
            if (latest.ChequeType is { } ty && typeId.TryGetValue(ty, out var tid)) book.CHECKTYPE_ID = tid;
            bookId[g.Key] = book.ID;
        }

        // ----- برگ چک‌ها (وضعیت از آخرین سال) -----
        var bookIds = bookId.Values.ToList();
        var existingChecks = (await db.TB_CHECKs.Where(c => bookIds.Contains(c.CHECKBOOK_ID) && c.VAHEDCODE == _v).ToListAsync(ct))
            .GroupBy(c => (c.CHECKBOOK_ID, c.CHEQ_NO.Trim())).ToDictionary(g => g.Key, g => g.First());
        foreach (var g in years.SelectMany(y => y.Checks.Select(c => (Year: y, Check: c)))
                     .Select(x => (x.Check, Key: x.Year.CheckKey(x.Check.Seq)))
                     .Where(x => x.Key != null && bookId.ContainsKey((x.Key.Value.AccountNo, x.Key.Value.BookFrom)))
                     .GroupBy(x => (Book: bookId[(x.Key!.Value.AccountNo, x.Key.Value.BookFrom)], x.Check.No)))
        {
            var latest = g.Last().Check;
            if (!existingChecks.TryGetValue(g.Key, out var c))
            {
                c = new TB_CHECK
                {
                    ID = Guid.NewGuid(), CHECKBOOK_ID = g.Key.Book, CHEQ_NO = LegacyText.Cut(g.Key.No, 10), VAHEDCODE = _v,
                    ADDUSERID = userId, CREATEDDATE = _now, ISDELETED = false,
                };
                db.TB_CHECKs.Add(c);
                existingChecks[g.Key] = c;
                newChecks++;
            }
            else updatedChecks++;
            c.CHEQ_DATE = Null(latest.Date);
            c.PAPER_DESC = Null(LegacyText.Cut(latest.Desc, 800));
            c.DATE_RSID = Null(latest.DateRsid);
            c.PAYTO = Null(LegacyText.Cut(latest.PayTo, 200));
            c.EBTAL = latest.Ebtal == "1" ? CheckCancelStatus.Canceled : CheckCancelStatus.NotCanceled;
            c.PRINT = latest.Print == "1" ? CheckPrintStatus.Printed : CheckPrintStatus.None;
        }

        // ----- کارگاه‌ها (ACC_BRCH ← TB_WORKSHOP؛ با کد کارگاه یکی می‌شوند، آخرین سال برای نام/وضعیت/معین) -----
        var vahedIds = (await db.TB_VAHED_INFOs.AsNoTracking().Select(x => new { x.ID, x.VAHEDCODE }).ToListAsync(ct))
            .Where(x => x.VAHEDCODE != null).GroupBy(x => x.VAHEDCODE!.Trim()).ToDictionary(g => g.Key, g => g.First().ID);
        var existingWorkshops = await db.TB_WORKSHOPs.Where(w => w.VAHEDCODE == _v && w.ISDELETED != true).ToListAsync(ct);
        int workshops = 0, newWorkshops = 0;
        foreach (var g in years.SelectMany(y => y.Workshops.Select(w => (Year: y, W: w))).Where(x => x.W.Code != "").GroupBy(x => x.W.Code))
        {
            var (wy, w) = g.Last();
            Guid? moein = chart.Moeins.TryGetValue((w.Kol, w.Moi), out var mm) && moeinIds.TryGetValue(mm.Final, out var mid) ? mid : null;
            if (moein == null)
            {
                report.Errors.Add($"کارگاه {w.Code} «{w.Name}»: معین {w.Kol}/{w.Moi} در کدینگ جدید پیدا نشد");
                continue;
            }
            var ws = existingWorkshops.FirstOrDefault(x => x.WORKSHOPCODE.Trim() == w.Code);
            if (ws == null)
            {
                ws = new TB_WORKSHOP
                {
                    ID = Guid.NewGuid(), WORKSHOPCODE = LegacyText.Cut(w.Code, 10), VAHEDCODE = _v,
                    ADDUSERID = userId, CREATEDDATE = _now, ISDELETED = false,
                };
                db.TB_WORKSHOPs.Add(ws);
                existingWorkshops.Add(ws);
                newWorkshops++;
            }
            ws.WORKSHOPNAME = LegacyText.Cut(w.Name, 100);
            ws.ACCOUNTCODE_ID = moein.Value;
            ws.BRANCH_ID = vahedIds.TryGetValue(w.BranchCode, out var br) ? br : null;
            ws.ISACTIVE = w.Stat == "1";
            if (ws.BRANCH_ID == null && w.BranchCode != "") report.Warnings.Add($"کارگاه {w.Code}: شعبهٔ {w.BranchCode} در فهرست واحدها نیست");
            workshops++;

            // تفصیلی کارگاه (اگر روی تفصیلی تعریف شده باشد؛ 000000 = خود معین)
            if (unitMap != null && w.HesSeq is { } hs && wy.TafsiliKey.TryGetValue(hs, out var key)
                && unitMap.TafsiliFinal.TryGetValue(key, out var final))
            {
                var tafsili = await db.TB_TAFSILIs.AsNoTracking().Where(t => t.TAFSILI_CODE == final).Select(t => t.ID).ToListAsync(ct);
                var lv = (unitMap.Tafsili.GetValueOrDefault(key)?.Level ?? 1).ToString();
                var level = await db.TB_LEVEL_TAFSILs.AsNoTracking().Where(l => l.LEVEL_CODE == lv && !l.ISDELETED).Select(l => l.ID).ToListAsync(ct);
                if (tafsili.Count > 0 && level.Count > 0
                    && (await db.TB_WORKSHOP_LINK_TAFSILIs.Where(x => x.WORKSHOP_ID == ws.ID && !x.ISDELETED).Select(x => x.ID).ToListAsync(ct)).Count == 0)
                    db.TB_WORKSHOP_LINK_TAFSILIs.Add(new TB_WORKSHOP_LINK_TAFSILI
                    {
                        ID = Guid.NewGuid(), WORKSHOP_ID = ws.ID, TAFSILI_ID = tafsili[0], LEVEL_ID = level[0], VAHEDCODE = _v,
                        ADDUSERID = userId, CREATEDDATE = _now, ISDELETED = false,
                    });
            }
        }
        if (!report.Ok) return false;

        db.BulkAuditLabel = $"انتقال اطلاعات بانکی سیستم قدیم — واحد {_v}";
        await db.SaveChangesAsync(ct);
        db.BulkAuditLabel = null;
        report.Info.Add($"حساب بانکی: {accountId.Count} ({newAccounts} تازه)، نوع چک: {typeId.Count} ({newTypes} تازه)، " +
                        $"دسته‌چک: {bookId.Count} ({newBooks} تازه)، برگ چک: {newChecks} تازه و {updatedChecks} به‌روز، کارگاه: {workshops} ({newWorkshops} تازه).");
        if (accountType == null) report.Warnings.Add($"نوع حساب «{CurrentAccountType}» در «انواع حساب» نبود؛ نوع حساب‌ها خالی ماند.");
        return true;
    }

    private static string? Null(string s) => s == "" ? null : s;

    /// <summary>ستون‌های هم‌نام CHEQUE_TYPE قدیم ← TB_CHECK_TYPE (اندازه‌ها، قلم‌ها، تصویر).</summary>
    private static void CopyColumns(Dictionary<string, object?> row, TB_CHECK_TYPE t)
    {
        foreach (var p in typeof(TB_CHECK_TYPE).GetProperties())
        {
            if (p.Name is "ID" or "YEAR" or "VAHEDCODE" || !p.CanWrite || !row.TryGetValue(p.Name, out var v) || v == null) continue;
            var target = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
            try
            {
                object? value = target == typeof(string) ? LegacyText.Cut(Convert.ToString(v)!, 200)
                    : target == typeof(byte[]) ? v as byte[]
                    : target == typeof(short) ? (object)Convert.ToInt16(v)
                    : null;
                if (value != null) p.SetValue(t, value);
            }
            catch (OverflowException) { }
        }
        t.CHEQUE_TYPE_TITLE = LegacyText.Cut(t.CHEQUE_TYPE_TITLE ?? "", 25);
    }
}
