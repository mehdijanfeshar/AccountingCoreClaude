using System.Text.Json;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;
using W = Accounting.Infrastructure.LegacyImport.LegacyUnitWorkbook;

namespace Accounting.Infrastructure.LegacyImport;

/// <summary>سند منتقل‌شده: کلید سند قدیم (سال اسکیما:شناسه) ← سرسند جدید.</summary>
internal sealed record MigratedVoucher(string OldKey, string OldSchema, string OldNo, string Date, Guid HeadId, string NewNo, int Lines);

/// <summary>اسناد منتقل‌شدهٔ یک سال (فایل کنار کاربرگ‌ها) — برای انتقال دوباره و فهرست شمارهٔ قدیم ← جدید.</summary>
internal sealed record MigratedYear(int Year, DateTime At, List<MigratedVoucher> Vouchers,
    List<Guid>? Receipts = null, List<Guid>? BankCards = null, List<Guid>? Incorrents = null,
    List<Guid>? Elams = null, List<Guid>? Attribs = null, Dictionary<string, IssueDecision>? Decisions = null);

/// <summary>
/// انتقال اسناد یک سال مالی از سیستم قدیم (تصمیم‌های صاحب پروژه ۲۰۲۶-۱۰-۱۴):
/// <list type="bullet">
/// <item>شمارهٔ سند تازه به ترتیب تاریخ؛ شمارهٔ قدیم در فهرست انتقال (فایل) می‌ماند.</item>
/// <item>افتتاحیه/اختتامیهٔ قدیم مثل بقیهٔ اسناد عیناً منتقل می‌شود.</item>
/// <item>انتقال دوبارهٔ یک سال: فقط اسنادی که قبلاً از انتقال آمده‌اند حذف و از نو منتقل می‌شوند.</item>
/// </list>
/// معین از کاربرگ مشترک (کد نهایی معین)، تفصیلی و فعالیت/مرکز هزینه از کاربرگ ثبت‌شدهٔ واحد (کد نهایی)، وضعیت سند از
/// برگهٔ «وضعیت سند»، و تکلیف اسناد ناتراز/بیرون از سال از کاربرگ واحد. چک و اعتبار ردیف‌ها منتقل نمی‌شود.
/// <para>
/// کار در بخش‌های جدا (<see cref="Sections.Voucher"/>) گزارش می‌شود. هر ابهامی که کاربر واحد بتواند از همین صفحه رفع
/// کند (کد جایگزین، ردیف بدون اتصال چک/فیش، کنار گذاشتن اعلامیهٔ تکراری…) مشکلِ <see cref="IssueBook"/> است، نه خطا؛
/// بررسی تا آخر ادامه می‌یابد تا همهٔ مشکلات یک‌جا دیده شوند. ثبت نهایی همهٔ بخش‌ها در یک تراکنش است و فقط وقتی هیچ
/// مشکلی باز نیست. حذف یک ردیف از سند هیچ‌وقت گزینه نیست (تراز سند)؛ کد ناموجود یا تعریف می‌شود یا جایگزین.
/// </para>
/// </summary>
internal sealed class LegacyVoucherImporter(
    LegacyDbContext db, LegacyUnit u, int year, ChartMapping chart, UnitMapping map, MigratedYear? previous, string userId,
    ImportReport report, IssueBook book)
{
    private readonly DateTime _now = DateTime.UtcNow;
    private readonly string _v = u.VahedCode;
    private readonly string _y = year.ToString();

    public MigratedYear? Result { get; private set; }

    private sealed record Plan(OldYear Year, OldDoc Doc, List<OldLine> Lines, DocLife Life);

    public async Task<bool> RunAsync(CancellationToken ct)
    {
        if ((await db.TB_YEARs.AsNoTracking().Where(y => y.WORKING_YEAR == year).Select(y => y.WORKING_YEAR).ToListAsync(ct)).Count == 0)
        {
            report.Errors.Add($"سال مالی {year} در سیستم جدید تعریف نشده است (اطلاعات پایه ← سال‌های مالی).");
            return false;
        }

        var plans = SelectDocuments();
        if (report.Errors.Count > 0) return false;

        // ----- نگاشت‌ها -----
        var accountByCode = (await db.TB_ACCOUNTCODEs.AsNoTracking()
                .Where(a => a.TYPECODE == TypeCodes.Moin && a.ISDELETED != true).Select(a => new { a.ID, a.ACCCODE }).ToListAsync(ct))
            .Where(a => a.ACCCODE != null).GroupBy(a => a.ACCCODE!.Trim()).ToDictionary(g => g.Key, g => g.First().ID);
        var tafsiliByCode = (await db.TB_TAFSILIs.AsNoTracking().Select(t => new { t.ID, t.TAFSILI_CODE }).ToListAsync(ct))
            .Where(t => t.TAFSILI_CODE != null).GroupBy(t => t.TAFSILI_CODE!.Trim()).ToDictionary(g => g.Key, g => g.First().ID);
        var levelByCode = (await db.TB_LEVEL_TAFSILs.AsNoTracking().Where(l => !l.ISDELETED).ToListAsync(ct))
            .GroupBy(l => l.LEVEL_CODE.Trim()).ToDictionary(g => g.Key, g => g.First().ID);
        var sysTypeByCode = (await db.TB_SYSTYPEs.AsNoTracking().Select(s => new { s.ID, s.SYS_COD }).ToListAsync(ct))
            .GroupBy(s => s.SYS_COD.Trim()).ToDictionary(g => g.Key, g => g.First().ID);

        // سطح شناور هر کد نهایی تفصیلی: سطح خود ردیف، وگرنه سطح ردیف اصلی (ایجاد) با همان کد
        var levelOfCode = map.Tafsili.Values.Where(t => t.Act == W.ActNew && t.Level != null && map.TafsiliFinal.ContainsKey(t.Key))
            .GroupBy(t => map.TafsiliFinal[t.Key]).ToDictionary(g => g.Key, g => g.First().Level!.Value);

        // کد ناموجود: یا تعریف می‌شود (و بررسی دوباره) یا کاربر کد موجودی را جایگزین می‌کند — تصمیم صاحب پروژه ۲۰۲۶-۱۰-۱۵
        const string defineHint = "پیش از انتقال این سال این کد باید در سیستم جدید تعریف شود: آن را تعریف کنید و دوباره «بررسی» بزنید، "
                                  + "یا کد جایگزین را از فهرست انتخاب کنید.";
        Guid? ReplaceWith(string issueId, IssueDecision? d, Dictionary<string, Guid> byCode, string what)
        {
            if (d?.Option != IssueOptions.Replace) return null;
            if (d.Value != null && byCode.TryGetValue(d.Value.Trim(), out var id)) { book.Resolve(issueId); return id; }
            book.Reject(issueId, $"{what} جایگزین «{d.Value}» در سیستم جدید پیدا نشد.");
            return null;
        }
        Guid? Account((string Kol, string Moi) m, string? sample)
        {
            if (chart.Moeins.TryGetValue(m, out var mm) && mm.Final != "" && accountByCode.TryGetValue(mm.Final, out var id)) return id;
            var issue = $"moein:{m.Kol}/{m.Moi}";
            var name = u.Years.Select(y => y.MoeinName(m.Kol, m.Moi)).FirstOrDefault(n => n != "") ?? "";
            var why = mm == null || mm.Final == "" ? "در کاربرگ کدینگ مشترک کد جدید ندارد" : $"(کد جدید {mm.Final}) در کدینگ جدید نیست";
            var d = book.Raise(issue, Sections.Coding, $"معین قدیم {m.Kol}/{m.Moi} «{name}» {why}", defineHint, [IssueOptions.ReplaceMoein], sample);
            return ReplaceWith(issue, d, accountByCode, "معین");
        }
        Guid? Level(int lv)
        {
            if (levelByCode.TryGetValue(lv.ToString(), out var id)) return id;
            book.Raise($"level:{lv}", Sections.Coding, $"سطح شناور {lv} در «سطوح تفصیلی» تعریف نشده",
                $"در اطلاعات پایه ← سطوح تفصیلی، سطح {lv} را تعریف کنید و دوباره «بررسی» بزنید.", []);
            return null;
        }
        Guid? Tafsili(string code, string what, string? sample)
        {
            if (tafsiliByCode.TryGetValue(code, out var id)) return id;
            var issue = $"taf:{code}";
            var d = book.Raise(issue, Sections.Coding, $"تفصیلی {code} ({what}) در سیستم جدید نیست", defineHint, [IssueOptions.ReplaceTafsili], sample);
            return ReplaceWith(issue, d, tafsiliByCode, "تفصیلی");
        }
        Guid? NoFinal(string issue, string message, string? sample)
        {
            var d = book.Raise(issue, Sections.Coding, message,
                "کدینگ واحد برای این مورد کد نهایی ندارد: کاربرگ کدینگ واحد را اصلاح کنید، یا تفصیلی جایگزین را از فهرست انتخاب کنید.",
                [IssueOptions.ReplaceTafsili], sample);
            return ReplaceWith(issue, d, tafsiliByCode, "تفصیلی");
        }

        // ----- حذف انتقال قبلی همین سال -----
        // اعلامیه‌ها به سند ارجاع دارند ⇒ پیش از اسناد
        foreach (var chunk in (previous?.Elams ?? new()).Chunk(900).Select(c => c.ToList()))
        {
            var dets = await db.TB_ELAMDETAILs.Where(d => d.ELAMHEAD_ID != null && chunk.Contains(d.ELAMHEAD_ID.Value)).Select(d => d.ID).ToListAsync(ct);
            foreach (var dc in dets.Chunk(900).Select(c => c.ToList()))
            {
                await db.TB_ELAMDETAIL_LINK_TAFSILIs.Where(x => x.ELAMDETAIL_ID != null && dc.Contains(x.ELAMDETAIL_ID.Value)).ExecuteDeleteAsync(ct);
                await db.TB_ELAMDETAILs.Where(x => dc.Contains(x.ID)).ExecuteDeleteAsync(ct);
            }
            await db.TB_ELAMHEADs.Where(x => chunk.Contains(x.ID)).ExecuteDeleteAsync(ct);
        }
        var oldIds = previous?.Vouchers.Select(v => v.HeadId).ToList() ?? new();
        if (oldIds.Count > 0)
        {
            await DeleteAsync(oldIds, ct);
            report.Info.Add($"{oldIds.Count} سند انتقال قبلی سال {year} حذف شد تا از نو منتقل شود.");
        }
        foreach (var chunk in (previous?.Attribs ?? new()).Chunk(900).Select(c => c.ToList()))
            await db.TB_ATTRIBFORACCOUNTCODEs.Where(x => chunk.Contains(x.ID)).ExecuteDeleteAsync(ct);
        // کارت بانک ← (فیش، چک در جریان)؛ پس از اسناد که به فیش ارجاع دارند
        foreach (var chunk in (previous?.BankCards ?? new()).Chunk(900).Select(c => c.ToList()))
            await db.TB_BANKCARTDETAILs.Where(x => chunk.Contains(x.ID)).ExecuteDeleteAsync(ct);
        foreach (var chunk in (previous?.Incorrents ?? new()).Chunk(900).Select(c => c.ToList()))
            await db.TB_CHEQUES_INCORRENTs.Where(x => chunk.Contains(x.ID)).ExecuteDeleteAsync(ct);
        foreach (var chunk in (previous?.Receipts ?? new()).Chunk(900).Select(c => c.ToList()))
            await db.TB_RECEIPs.Where(x => chunk.Contains(x.ID)).ExecuteDeleteAsync(ct);

        // ----- اطلاعات بانکی سال: چک (از مرحلهٔ اطلاعات بانکی)، فیش، چک در جریان وصول، کارت بانک -----
        var bank = await BuildBankAsync(plans, ct);

        // شمارهٔ تازه بعد از بزرگ‌ترین شمارهٔ اسناد موجود (دستی) همین واحد و سال
        var taken = await db.TB_VOUCHERSHEADs.AsNoTracking().Where(h => h.VAHEDCODE == _v && h.YEAR == _y)
            .Select(h => h.DOC_NUM).ToListAsync(ct);
        var next = taken.Select(n => long.TryParse(n?.Trim(), out var x) ? x : 0).DefaultIfEmpty(0).Max();
        if (next > 0) report.Info.Add($"سال {year} از قبل {taken.Count} سند دارد؛ شمارهٔ اسناد منتقل‌شده از {next + 1} شروع می‌شود.");

        // تفصیلی (000000 = خود معین)، فعالیت (جزء، سطح ۱) و مرکز هزینه (ریز، سطح ۲) یک ردیف سند یا اعلامیه؛
        // مرکز هزینه فعالیتِ خودش را هم دارد
        List<(Guid Tafsili, Guid Level)> LinksOf(OldYear y, long hes, long? jr, string sample)
        {
            var links = new List<(Guid, Guid)>();
            void Add(Guid? t, Guid? lv) { if (t != null && lv != null) links.Add((t.Value, lv.Value)); }
            if (y.CodeBySeq.TryGetValue(hes, out var code) && code.IsTafsili && y.TafsiliKey.TryGetValue(hes, out var key)
                && map.Tafsili.TryGetValue(key, out var t) && t.Act != W.ActNone)
            {
                if (!map.TafsiliFinal.TryGetValue(key, out var final))
                    Add(NoFinal($"tafkey:{key}", $"تفصیلی قدیم {key} «{code.Name}» کد نهایی ندارد (کدینگ واحد کامل ثبت نشده)", sample),
                        Level(t.Level ?? (jr != null ? 3 : 1)));
                else
                    Add(Tafsili(final, $"{key} «{code.Name}»", sample), Level(t.Level ?? levelOfCode.GetValueOrDefault(final, jr != null ? 3 : 1)));
            }
            if (jr is { } j && y.LowBySeq.TryGetValue(j, out var low))
            {
                void Low(string lowKey, int lv)
                {
                    if (map.Lows.TryGetValue(lowKey, out var lm) && lm.Act == W.LowSkip) return;
                    if (map.LowFinal.TryGetValue(lowKey, out var lf)) Add(Tafsili(lf, lowKey, sample), Level(lv));
                    else Add(NoFinal($"low:{lowKey}", $"فعالیت/مرکز هزینهٔ قدیم {lowKey} «{low.Name}» کد نهایی ندارد", sample), Level(lv));
                }
                Low($"{low.Joz}/", 1);
                if (low.IsCostCenter) Low(low.Key, 2);
            }
            return links;
        }

        // ویژگی معین (ATTRIBFORMAINCODE ← TB_ATTRIBFORACCOUNTCODE): برای هر معین در این سال یک تعریف (یکتایی جدول)
        var attribValuesByLine = u.Years.SelectMany(y => y.AttribValues.Select(a => (y.Year, a)))
            .GroupBy(x => (x.Year, x.a.LineSeq)).ToDictionary(g => g.Key, g => g.Select(x => x.a).ToList());
        var attribByAccount = (await db.TB_ATTRIBFORACCOUNTCODEs.Where(a => a.VAHEDCODE == _v && a.YEAR == _y).ToListAsync(ct))
            .GroupBy(a => a.ACCOUNTCODE_ID).ToDictionary(g => g.Key, g => g.First().ID);
        var newAttribs = new List<Guid>();
        var skippedAttribs = new SortedSet<string>();
        int attribCount = 0;
        Guid? AttribDef(OldYear y, long defSeq)
        {
            var d = y.AttribDefs.FirstOrDefault(x => x.Seq == defSeq);
            if (d == null || y.MoeinOf(d.HesSeq) is not { } m) return null;
            // معینی که در کدینگ جدید نیست (مثلاً بی‌سند و بیرون از کدینگ مشترک): ویژگی کنار می‌رود، خطا نیست
            if (!chart.Moeins.TryGetValue(m, out var mm) || mm.Final == "" || !accountByCode.TryGetValue(mm.Final, out var acc))
            {
                skippedAttribs.Add($"{m.Kol}/{m.Moi}");
                return null;
            }
            if (attribByAccount.TryGetValue(acc, out var id)) return id;
            var def = new TB_ATTRIBFORACCOUNTCODE
            {
                ID = Guid.NewGuid(), ACCOUNTCODE_ID = acc, ATTRIBBOXNO = (short)d.BoxNo,
                FLAG = d.Flag == "1" ? AttribFlag.Date : AttribFlag.Number, LENATR = (byte)Math.Clamp(d.Length, 0, 255),
                ATTRIBSUM = d.Sum == "2" ? AttribSum.UnSummable : AttribSum.Summable,
                CONTROLID = d.Control switch { "1" => AttribControl.NotZero, "2" => AttribControl.IsDate, _ => null },
                VAHEDCODE = _v, YEAR = _y, ADDUSERID = userId, CREATEDDATE = _now, ISDELETED = false,
            };
            db.TB_ATTRIBFORACCOUNTCODEs.Add(def);
            book.Count(Sections.AttribDefs);
            attribByAccount[acc] = def.ID;
            newAttribs.Add(def.ID);
            return def.ID;
        }

        // ----- ساخت اسناد -----
        var headOf = new Dictionary<(int Year, long DocSeq), Guid>();
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        var migrated = new List<MigratedVoucher>();
        int lineCount = 0, tafLinks = 0, chequeLines = 0, noSysType = 0, closings = 0;
        foreach (var p in plans)
        {
            var docNo = (++next).ToString("D6"); // مثل بقیهٔ سامانه: ۶ رقم با صفر چپ
            if (docNo.Length > 6) { report.Errors.Add($"شمارهٔ سند از ۶ رقم بیشتر شد (سال {year})"); break; }
            var sample = $"سند {p.Doc.No} ({LegacyText.Date8(p.Doc.Date)}) سال {p.Year.Year}";
            // سیستم قدیم اختتامیه را در ماه‌های ۱۳ تا ۱۵ ثبت می‌کرد؛ سیستم جدید: آخرین روز سال + علامت اختتامیه
            // (تصمیم صاحب پروژه ۲۰۲۶-۱۰-۱۴). تاریخ قدیم در فهرست شمارهٔ قدیم ← جدید می‌ماند.
            var closing = IsClosingMonth(p.Doc.Date);
            if (closing) closings++;
            var head = new TB_VOUCHERSHEAD
            {
                ID = Guid.NewGuid(), DOC_NUM = docNo, DATE_DOC = closing ? p.Doc.DocYear + "1229" : p.Doc.Date, DOCLIFE = p.Life,
                FLAG_STATE = closing ? Accounting.Application.Vouchers.YearEnd.YearEndKinds.ClosingFlag : null,
                HEAD_DESC = LegacyText.Cut(p.Doc.Desc, 250), APENDIX = LegacyText.Cut(p.Doc.Apendix, 800),
                SYSTEM_TYPE = p.Doc.SysType != "" && sysTypeByCode.TryGetValue(p.Doc.SysType, out var st) ? st : null,
                VAHEDCODE = _v, YEAR = _y, ISAUTOMATIC = false, ADDUSERID = userId, CREATEDDATE = _now, ISDELETED = false,
            };
            if (head.SYSTEM_TYPE == null && p.Doc.SysType != "") noSysType++;
            db.TB_VOUCHERSHEADs.Add(head);
            book.Count(Sections.Vouchers);

            var radif = 0;
            foreach (var l in p.Lines.OrderBy(l => l.Radif).ThenBy(l => l.Seq))
            {
                var code = p.Year.CodeBySeq.GetValueOrDefault(l.HesSeq);
                var moein = p.Year.MoeinOf(l.HesSeq);
                Guid? account;
                if (code == null || moein == null)
                {
                    // ردیف روی کد کل یا کدی که در کدینگ قدیم نیست: فقط با معین جایگزین منتقل می‌شود
                    var issue = code == null ? $"hes:{p.Year.Year}:{l.HesSeq}" : $"kol:{code.Kol}";
                    var d = book.Raise(issue, Sections.Coding,
                        code == null ? $"ردیف سند روی کدی (شناسهٔ {l.HesSeq} سال {p.Year.Year}) است که در کدینگ قدیم نیست"
                                     : $"ردیف سند روی کد کل {code.Kol} «{code.Name}» است (ردیف سند باید معین داشته باشد)",
                        "معینی را که این ردیف‌ها باید روی آن بنشینند از فهرست انتخاب کنید.", [IssueOptions.ReplaceMoein], sample);
                    account = ReplaceWith(issue, d, accountByCode, "معین");
                }
                else account = Account(moein.Value, sample);
                if (account == null) continue;

                var detail = new TB_VOUCHERSDETAIL
                {
                    ID = Guid.NewGuid(), VOUCHERSHEAD_ID = head.ID, ACCOUNT_ID = account, RADIF = ++radif,
                    DESCRIPTION = LegacyText.Cut(l.Desc, 200), DEBTOR = l.Bed, CREDITOR = l.Bes,
                    RECEIP_ID = l.CidSeq is { } cid ? bank.ReceiptOf(p.Year, cid) : null,
                    CHECK_ID = l.PobSeq is { } pob ? bank.CheckOf(p.Year, pob, Sections.Vouchers, sample) : null,
                    VAHEDCODE = _v, YEAR = _y, ADDUSERID = userId, CREATEDDATE = _now, ISDELETED = false,
                };
                db.TB_VOUCHERSDETAILs.Add(detail);
                lineCount++;
                if (l.HasEtebar) chequeLines++;

                foreach (var (tafsili, level) in LinksOf(p.Year, l.HesSeq, l.JrSeq, sample))
                {
                    db.TB_VOUCHERDETAIL_LINK_TAFSILIs.Add(new TB_VOUCHERDETAIL_LINK_TAFSILI
                    {
                        ID = Guid.NewGuid(), VOUCHERSDETAIL_ID = detail.ID, TAFSILI_ID = tafsili, LEVEL_ID = level,
                        VAHEDCODE = _v, YEAR = _y, ADDUSERID = userId, CREATEDDATE = _now, ISDELETED = false,
                    });
                    tafLinks++;
                }

                // ویژگی‌های ردیف (ATTRIBSINDOCS)؛ یکتایی (ردیف، تعریف) ⇒ هر تعریف یک مقدار
                var usedDefs = new HashSet<Guid>();
                foreach (var av in attribValuesByLine.GetValueOrDefault((p.Year.Year, l.Seq)) ?? new())
                    if (AttribDef(p.Year, av.DefSeq) is { } defId && usedDefs.Add(defId))
                    {
                        db.TB_ATTRIBSINVOUCHERs.Add(new TB_ATTRIBSINVOUCHER
                        {
                            ID = Guid.NewGuid(), VOUCHERSDETAIL_ID = detail.ID, ATTRIBFORACCOUNTCODE_ID = defId,
                            ATTRIBUTEVALUE = LegacyText.Cut(av.Value, 20), VAHEDCODE = _v, YEAR = _y,
                            ADDUSERID = userId, CREATEDDATE = _now, ISDELETED = false,
                        });
                        attribCount++;
                        book.Count(Sections.LineAttribs);
                    }
            }
            migrated.Add(new MigratedVoucher(W.DocKey(p.Year, p.Doc), p.Year.Schema, p.Doc.No, p.Doc.Date, head.ID, docNo, radif));
            headOf[(p.Year.Year, p.Doc.Seq)] = head.ID;
        }
        var elams = await BuildElamsAsync(headOf, Account, LinksOf, ct);
        db.ChangeTracker.AutoDetectChangesEnabled = true;
        if (report.Errors.Count > 0) return false;

        // با مشکل باز هم ذخیره (در تراکنشی که برگردانده می‌شود) اجرا می‌شود تا خطاهای پایگاه داده هم در همین بررسی دیده
        // شوند؛ ثبت واقعی را InTransactionAsync فقط وقتی report.Ok (بدون مشکل باز) است انجام می‌دهد.
        db.BulkAuditLabel = $"انتقال اسناد سیستم قدیم — واحد {_v} سال {year}: {migrated.Count} سند"
                            + (book.Applied().Count > 0 ? $"، با {book.Applied().Count} تصمیم کاربر" : "");
        await db.SaveChangesAsync(ct);
        db.BulkAuditLabel = null;

        Result = new MigratedYear(year, _now, migrated, bank.Receipts, bank.BankCards, bank.Incorrents, elams, newAttribs, book.Applied());
        book.Note(Sections.LineAttribs, $"ویژگی ردیف: {attribCount}.");
        book.Note(Sections.AttribDefs, $"تعریف تازهٔ ویژگی معین: {newAttribs.Count}.");
        if (skippedAttribs.Count > 0)
            report.Warnings.Add($"ویژگی معین‌های {string.Join("، ", skippedAttribs)} منتقل نشد: این معین‌ها در کدینگ جدید نیستند.");
        book.Note(Sections.Vouchers, $"{migrated.Count} سند، {lineCount} ردیف، {tafLinks} اتصال تفصیلی/فعالیت/مرکز هزینه، "
                                     + $"{bank.LinkedChecks} ردیف متصل به چک" +
                                     (migrated.Count > 0 ? $"؛ شمارهٔ {migrated[0].NewNo} تا {migrated[^1].NewNo}." : "."));
        if (closings > 0)
            book.Note(Sections.Vouchers, $"{closings} سند اختتامیه (ماه ۱۳ تا ۱۵ سیستم قدیم) با تاریخ {_y}/12/29 و علامت اختتامیه منتقل می‌شود.");
        if (chequeLines > 0)
            report.Warnings.Add($"{chequeLines} ردیف در سیستم قدیم اعتبار (ETEBAR) داشت؛ اثر حسابداری‌اش منتقل شد ولی خود اعتبار منتقل نمی‌شود.");
        if (noSysType > 0)
            report.Warnings.Add($"نوع سیستم {noSysType} سند در «انواع سیستم» جدید پیدا نشد و خالی ماند.");
        return true;
    }

    /// <summary>
    /// اعلامیه‌های اسکیمای همین سال (ELAMHEAD/ELAMDETAIL ← TB_ELAMHEAD/TB_ELAMDETAIL): سریال = واحد + سال + سریال قدیم (قالب
    /// سیستم جدید، یکتا بین سال‌ها)، کد = کد معین جدیدِ رابط قدیم (کل+معین)، سند = سند منتقل‌شدهٔ همان DOC_SEQ.
    /// </summary>
    private async Task<List<Guid>> BuildElamsAsync(Dictionary<(int, long), Guid> headOf, Func<(string, string), string?, Guid?> account,
        Func<OldYear, long, long?, string, List<(Guid Tafsili, Guid Level)>> linksOf, CancellationToken ct)
    {
        var ids = new List<Guid>();
        if (u.Years.FirstOrDefault(y => y.Year == year) is not { } own || own.Elams.Count == 0) return ids;

        var workshops = (await db.TB_WORKSHOPs.AsNoTracking().Where(w => w.VAHEDCODE == _v && w.ISDELETED != true)
                .Select(w => new { w.ID, w.WORKSHOPCODE }).ToListAsync(ct))
            .GroupBy(w => w.WORKSHOPCODE.Trim()).ToDictionary(g => g.Key, g => g.First().ID);
        var taken = (await db.TB_ELAMHEADs.AsNoTracking().Where(h => h.VAHEDCODE == _v)
            .Select(h => new { h.ELAMH_SERIALNO, h.ELAMH_CODE }).ToListAsync(ct)).Select(h => (h.ELAMH_SERIALNO, h.ELAMH_CODE)).ToHashSet();
        var linesByElam = own.ElamLines.GroupBy(l => l.ElamSeq).ToDictionary(g => g.Key, g => g.ToList());
        int lines = 0, noVoucher = 0;
        foreach (var e in own.Elams.OrderBy(e => e.Date).ThenBy(e => e.SerialNo))
        {
            var code = e.Code.Length == 4 && chart.Moeins.TryGetValue((e.Code[..2], e.Code[2..]), out var mm) && mm.Final != "" ? mm.Final : e.Code;
            var serial = _v + _y + (e.SerialNo.Length < 6 ? e.SerialNo.PadLeft(6, '0') : e.SerialNo[^6..]);
            var sample = $"اعلامیه {e.SerialNo} ({LegacyText.Date8(e.Date)})";
            if (!taken.Add((serial, code)))
            {
                // اعلامیهٔ هم‌سریال از قبل (دستی یا دو اعلامیهٔ قدیم با یک سریال): فقط با تأیید کاربر کنار می‌رود
                var issue = $"elamdup:{serial}/{code}";
                var d = book.Raise(issue, Sections.Elams,
                    $"اعلامیه {e.SerialNo} (کد {e.Code}) سال {year} با سریال {serial} در سیستم جدید از قبل هست",
                    "اگر همان اعلامیه است کنار بگذارید؛ وگرنه اعلامیهٔ موجود را در سیستم جدید اصلاح کنید و دوباره «بررسی» بزنید.",
                    [new(IssueOptions.Skip, "این اعلامیه منتقل نشود")], sample);
                if (d != null) book.Resolve(issue);
                continue;
            }
            // کارگاه: ستون کد/نام کارگاه اعلامیه، یا (اسکیماهای قدیمی‌تر بدون آن ستون‌ها) از BNH_SEQ در ACC_BRCH
            var wsRow = e.WorkshopSeq is { } bs ? own.Workshops.FirstOrDefault(w => w.Seq == bs) : null;
            var wsCode = e.WorkshopCode != "" ? e.WorkshopCode : wsRow?.Code ?? "";
            var wsName = e.WorkshopName != "" ? e.WorkshopName : wsRow?.Name ?? "";
            Guid? voucher = e.DocSeq is { } ds && headOf.TryGetValue((own.Year, ds), out var hid) ? hid : null;
            if (e.DocSeq != null && voucher == null) noVoucher++;
            var head = new TB_ELAMHEAD
            {
                ID = Guid.NewGuid(), VOUCHERSHEAD_ID = voucher, ELAMH_SERIALNO = serial, ELAMH_CODE = LegacyText.Cut(code, 6),
                ELAMH_DABIRNO = Null(e.DabirNo, 10), ELAMH_DABIRDATE = Null(e.DabirDate, 8),
                ELAMH_PRINTNO = e.PrintNo is { } pn and >= short.MinValue and <= short.MaxValue ? (short)pn : null,
                ELAMH_CASE = e.Case switch { "1" => ElamCase.Debtor, "2" => ElamCase.Creditor, _ => null },
                SERIALNO_INPUT = Null(e.SerialInput, 6), WEB_STAT = byte.TryParse(e.WebStat, out var ws) ? ws : null,
                ELAMH_DATE = Null(e.Date, 8), ELAMH_DESC = Null(e.Desc, 300),
                WORKSHOP_ID = wsCode != "" && workshops.TryGetValue(wsCode, out var wid) ? wid : null,
                ELAMH_RCVNO = Null(e.RcvNo, 14), ELAMH_RCVDT = Null(e.RcvDate, 8), ELAMH_LSTMON = Null(e.LastMonth, 2), PAY_NO = Null(e.PayNo, 15),
                ELAMHDRAMAD_TYPE = e.ZFlag switch
                {
                    "1" => DaramElamhType.PersonalDramadElam, "2" => DaramElamhType.ZeeDramadElam, "3" => DaramElamhType.OtherDramadElam, _ => null,
                },
                PEIMAN_NO = Null(e.PeimanNo, 12), ELAMH_WORKSHOPCODE = Null(wsCode, 10), ELAMH_WORKSHOPNAME = Null(wsName, 100),
                ELAMH_SENDRCVVAHED = Null(e.SendRcvVahed, 4), ELAMH_YEAR = Null(e.Yy, 4),
                VAHEDCODE = _v, YEAR = _y, ADDUSERID = userId, CREATEDDATE = _now, ISDELETED = false,
            };
            db.TB_ELAMHEADs.Add(head);
            ids.Add(head.ID);
            book.Count(Sections.Elams);

            foreach (var l in linesByElam.GetValueOrDefault(e.Seq) ?? new())
            {
                Guid? acc = l.HesSeq is { } h && own.MoeinOf(h) is { } m ? account(m, sample) : null;
                var det = new TB_ELAMDETAIL
                {
                    ID = Guid.NewGuid(), ELAMHEAD_ID = head.ID, ACCOUNTCODE_ID = acc, DEBTOR = l.Bed, CREDITOR = l.Bes,
                    ELAMD_DESC = Null(l.Desc, 800), ELAM_ATRIBNO = Null(l.AttribNo, 10),
                    VAHEDCODE = _v, YEAR = _y, ADDUSERID = userId, CREATEDDATE = _now, ISDELETED = false,
                };
                db.TB_ELAMDETAILs.Add(det);
                lines++;
                if (l.HesSeq is { } hs)
                    foreach (var (tafsili, level) in linksOf(own, hs, l.JrSeq, sample))
                        db.TB_ELAMDETAIL_LINK_TAFSILIs.Add(new TB_ELAMDETAIL_LINK_TAFSILI
                        {
                            ID = Guid.NewGuid(), ELAMDETAIL_ID = det.ID, TAFSILI_ID = tafsili, LEVEL_ID = level,
                            VAHEDCODE = _v, YEAR = _y, ADDUSERID = userId, CREATEDDATE = _now, ISDELETED = false,
                        });
            }
        }
        book.Note(Sections.Elams, $"{ids.Count} اعلامیه با {lines} ردیف.");
        if (noVoucher > 0) report.Warnings.Add($"سند {noVoucher} اعلامیه منتقل نشده (مثلاً «منتقل نشود» بوده)؛ اعلامیه بدون سند منتقل شد.");
        return ids;
    }

    private static string? Null(string s, int max) => s == "" ? null : LegacyText.Cut(s, max);

    /// <summary>اطلاعات بانکی سال هدف که ردیف‌های سند و کارت بانک به آن ارجاع دارند.</summary>
    private sealed class YearBank(IssueBook book)
    {
        public Dictionary<(int Year, long Cid), Guid> ReceiptIds { get; } = new();
        public Dictionary<(string Acc, long From, string No), Guid> CheckIds { get; init; } = new();
        public List<Guid> Receipts { get; } = new();
        public List<Guid> BankCards { get; } = new();
        public List<Guid> Incorrents { get; } = new();
        public int LinkedChecks { get; set; }

        public Guid? ReceiptOf(OldYear y, long cid) => ReceiptIds.TryGetValue((y.Year, cid), out var id) ? id : null;

        /// <summary>چک پیدا نشده: یا اطلاعات بانکی دوباره منتقل می‌شود، یا با تأیید کاربر ردیف/کارت بدون اتصال چک (اثر مالی دست نمی‌خورد).</summary>
        public Guid? CheckOf(OldYear y, long pob, string section, string sample, bool countLine = true)
        {
            var k = y.CheckKey(pob);
            if (k is { } key && CheckIds.TryGetValue(key, out var id)) { if (countLine) LinkedChecks++; return id; }
            var issue = k is { } c ? $"cheque:{c.AccountNo}/{c.ChequeNo}" : $"cheque:{y.Year}:{pob}";
            var d = book.Raise(issue, section,
                k is { } c2 ? $"چک {c2.ChequeNo} حساب {c2.AccountNo} (سال {y.Year}) در سیستم جدید نیست"
                            : $"چک شناسهٔ {pob} سال {y.Year} در برگ چک‌های سیستم قدیم نیست",
                "«انتقال اطلاعات بانکی» را (دوباره) انجام دهید و دوباره «بررسی» بزنید؛ یا بدون اتصال به چک منتقل شود (مبلغ و حساب ردیف دست نمی‌خورد).",
                [new(IssueOptions.NoLink, "بدون اتصال به چک منتقل شود")], sample);
            if (d != null) book.Resolve(issue);
            return null;
        }
    }

    /// <summary>
    /// فیش/حواله (CHECKSINDOCS) ردیف‌های اسناد سال و کارت بانک، کارت بانک (BANKCARTDETAIL) و چک‌های در جریان وصول
    /// (CHEQUES_INCORRENT) اسکیمای همین سال؛ چک‌ها از مرحلهٔ «اطلاعات بانکی» (با شمارهٔ حساب/دسته‌چک/چک) پیدا می‌شوند.
    /// </summary>
    private async Task<YearBank> BuildBankAsync(List<Plan> plans, CancellationToken ct)
    {
        var checks = await (from c in db.TB_CHECKs.AsNoTracking()
                            join b in db.TB_CHECKBOOKs.AsNoTracking() on c.CHECKBOOK_ID equals b.ID
                            join a in db.TB_ACCOUNTs.AsNoTracking() on b.ACCOUNT_ID equals a.ID
                            where c.VAHEDCODE == _v && !c.ISDELETED
                            select new { c.ID, a.ACCOUNTNUMBER, b.FROMCHECKNUMBER, c.CHEQ_NO }).ToListAsync(ct);
        var yb = new YearBank(book)
        {
            CheckIds = checks.Where(c => long.TryParse(c.FROMCHECKNUMBER, out _))
                .GroupBy(c => (c.ACCOUNTNUMBER.Trim(), long.Parse(c.FROMCHECKNUMBER), c.CHEQ_NO.Trim()))
                .ToDictionary(g => g.Key, g => g.First().ID),
        };
        var checkByNo = checks.GroupBy(c => (c.ACCOUNTNUMBER.Trim(), c.CHEQ_NO.Trim()))
            .Where(g => g.Count() == 1).ToDictionary(g => g.Key, g => g.First().ID);

        // فیشی که ردیف به آن ارجاع دارد ولی در CHECKSINDOCS نیست: فیش خالی ساخته نمی‌شود؛ با تأیید کاربر ردیف بدون فیش
        Guid? NewReceipt(OldYear y, long cid, string sample)
        {
            if (yb.ReceiptIds.TryGetValue((y.Year, cid), out var id)) return id;
            var r = y.Receipts.FirstOrDefault(x => x.Seq == cid);
            if (r == null)
            {
                var issue = $"receipt:{y.Year}:{cid}";
                var d = book.Raise(issue, Sections.Receipts, $"فیش/حوالهٔ شناسهٔ {cid} سال {y.Year} در CHECKSINDOCS سیستم قدیم نیست",
                    "اطلاعات این فیش در سیستم قدیم از دست رفته است؛ ردیف (یا کارت بانک) بدون فیش منتقل شود؟ مبلغ و حساب دست نمی‌خورد.",
                    [new(IssueOptions.NoLink, "بدون فیش منتقل شود")], sample);
                if (d != null) book.Resolve(issue);
                return null;
            }
            var rec = new TB_RECEIP
            {
                ID = Guid.NewGuid(), RECEIPT_KIND = r.Kind is "2" or "4" ? ReceiptType.Havale : ReceiptType.Fish, // قدیم: ۲ = حواله، ۳ = فیش
                RECEIPT_DATE = r.Date, RECEIPT_NO = LegacyText.Cut(r.No, 10), DATE_RSID = r.DateRsid is { Length: > 0 } dr ? dr : null,
                VAHEDCODE = _v, YEAR = _y, ADDUSERID = userId, CREATEDDATE = _now, ISDELETED = false,
            };
            db.TB_RECEIPs.Add(rec);
            book.Count(Sections.Receipts);
            yb.ReceiptIds[(y.Year, cid)] = rec.ID;
            yb.Receipts.Add(rec.ID);
            return rec.ID;
        }
        foreach (var p in plans)
            foreach (var l in p.Lines.Where(l => l.CidSeq != null))
                NewReceipt(p.Year, l.CidSeq!.Value, $"سند {p.Doc.No} ({LegacyText.Date8(p.Doc.Date)}) سال {p.Year.Year}");

        // کارت بانک و چک در جریان وصول: فقط اسکیمای خودِ این سال
        if (u.Years.FirstOrDefault(y => y.Year == year) is { } own)
        {
            var banks = (await db.TB_BANK_LISTs.AsNoTracking().Select(b => new { b.ID, b.BANKCODE }).ToListAsync(ct))
                .GroupBy(b => b.BANKCODE.Trim()).ToDictionary(g => g.Key, g => g.First().ID);
            var branches = (await db.TB_BANKBRANCH_LISTs.AsNoTracking().Select(b => new { b.ID, b.BANK_ID, b.BRANCHCODE }).ToListAsync(ct))
                .GroupBy(b => (b.BANK_ID, b.BRANCHCODE.Trim())).ToDictionary(g => g.Key, g => g.First().ID);

            var incorrentIds = new Dictionary<long, Guid>();
            foreach (var i in own.Incorrents)
            {
                var inc = new TB_CHEQUES_INCORRENT
                {
                    ID = Guid.NewGuid(), DOC_NUM = LegacyText.Cut(i.DocNo, 6), DOC_DATE = i.DocDate, CHEQ_NO = i.ChequeNo, CHEQ_DATE = i.ChequeDate,
                    PAPER_DESC = i.Desc == "" ? null : LegacyText.Cut(i.Desc, 200), PAYTO = i.PayTo == "" ? null : LegacyText.Cut(i.PayTo, 200),
                    RECIVDATE = i.DateRsid == "" ? null : i.DateRsid, ACCOUNTNUMBER = i.AccountNo, CREDITOR = i.Bes,
                    CHECK_ID = checkByNo.TryGetValue((i.AccountNo, i.ChequeNo), out var cid) ? cid : null,
                    VAHEDCODE = _v, YEAR = _y, ADDUSERID = userId, CREATEDDATE = _now, ISDELETED = false,
                };
                db.TB_CHEQUES_INCORRENTs.Add(inc);
                book.Count(Sections.Incorrents);
                incorrentIds[i.Seq] = inc.ID;
                yb.Incorrents.Add(inc.ID);
            }

            var seen = new HashSet<string>();
            int duplicates = 0;
            foreach (var c in own.BankCards)
            {
                Guid? bankId = banks.TryGetValue(c.BankCode, out var bid) ? bid : null;
                Guid? branchId = bankId != null && branches.TryGetValue((bankId.Value, c.BranchCode), out var brid) ? brid : null;
                var sample = $"کارت بانک {c.AccountNo} ماه {c.Month}، شمارهٔ {c.ChequeNo}";
                var type = CardType(c.Type);
                if (type == null)
                {
                    // نوع ناشناخته: کاربر نوع را تعیین می‌کند یا آن ردیف‌ها کنار می‌روند (کارت بانک اثر حسابداری ندارد)
                    var issue = $"cardtype:{c.Type}";
                    var d = book.Raise(issue, Sections.BankCards, $"نوع کارت بانک «{c.Type}» در سیستم قدیم شناخته نیست",
                        "نوع درست این ردیف‌ها را انتخاب کنید، یا این ردیف‌های کارت بانک منتقل نشوند.",
                        [new("0", "چک صوری"), new("1", "چک واقعی"), new("2", "حواله"), new("3", "فیش"), new(IssueOptions.Skip, "این ردیف‌ها منتقل نشوند")],
                        sample);
                    if (d == null) continue;
                    book.Resolve(issue);
                    if (d.Option == IssueOptions.Skip) continue;
                    type = CardType(d.Option);
                }
                // یکتایی جدول: همان بانک/شعبه/حساب/ماه/شماره/تاریخ/نوع/مبلغ فقط یک بار
                if (!seen.Add($"{bankId}|{branchId}|{c.AccountNo}|{c.Month}|{c.ChequeNo}|{c.ReceiveDate}|{type}|{c.Bed}|{c.Bes}")) { duplicates++; continue; }
                var card = new TB_BANKCARTDETAIL
                {
                    ID = Guid.NewGuid(), BANK_ID = bankId, BRANCH_ID = branchId, ACCOUNTNUMBER = c.AccountNo, MONTH = c.Month,
                    CHEQNO = c.ChequeNo, RECIVDATE = c.ReceiveDate, CHECKRECEIPTTYPE = type, DEBTOR = c.Bed, CREDITOR = c.Bes,
                    RECEIP_ID = c.CidSeq is { } rc ? NewReceipt(own, rc, sample) : null,
                    CHECK_ID = c.PobSeq is { } pc ? yb.CheckOf(own, pc, Sections.BankCards, sample, countLine: false) : null,
                    CHECK_INCORRENT_ID = c.IncorrentSeq is { } ic && incorrentIds.TryGetValue(ic, out var iid) ? iid : null,
                    VAHEDCODE = _v, YEAR = _y, ADDUSERID = userId, CREATEDDATE = _now, ISDELETED = false,
                };
                db.TB_BANKCARTDETAILs.Add(card);
                book.Count(Sections.BankCards);
                yb.BankCards.Add(card.ID);
            }
            if (duplicates > 0) book.Note(Sections.BankCards, $"{duplicates} ردیف تکراری کارت بانک یک بار منتقل شد.");
        }

        return yb;
    }

    /// <summary>BCD_TYPE قدیم: ۰ صوری، ۱ واقعی، ۲ (و ۴) حواله، ۳ فیش.</summary>
    private static CheckReceiptType? CardType(string t) => t switch
    {
        "0" => CheckReceiptType.SoriCheck, "1" => CheckReceiptType.RealCheck,
        "2" or "4" => CheckReceiptType.Havale, "3" => CheckReceiptType.Fish, _ => null,
    };

    /// <summary>ماه ۱۳، ۱۴ یا ۱۵ = سند اختتامیهٔ سیستم قدیم.</summary>
    private static bool IsClosingMonth(string date) => date.Length == 8 && int.TryParse(date.Substring(4, 2), out var m) && m >= 13;

    /// <summary>اسناد سال هدف: اسناد هم‌سال هر اسکیما + اسناد بیرون از سالی که «در سال تاریخ خودش» منتقل می‌شوند.</summary>
    private List<Plan> SelectDocuments()
    {
        var plans = new List<Plan>();
        int skippedLife = 0, skippedUnbal = 0, skippedOut = 0, empty = 0, drafts = 0;
        foreach (var y in u.Years)
        {
            var byDoc = y.Lines.GroupBy(l => l.DocSeq).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var d in y.Docs)
            {
                var key = W.DocKey(y, d);
                if (d.DocYear != _y)
                    continue;
                if (d.DocYear != y.Year.ToString())
                {
                    // بیرون از سال اسکیما؛ پیش‌فرض کاربرگ «در سال تاریخ خودش منتقل شود» است
                    if (map.OutOfYear.TryGetValue(key, out var o) && o.Act == W.OutSkip) { skippedOut++; continue; }
                }

                var lines = byDoc.GetValueOrDefault(d.Seq) ?? new();
                if (lines.Count == 0) { empty++; continue; }

                var sample = $"سند {d.No} ({LegacyText.Date8(d.Date)}) سال {y.Year}";
                if (!chart.DocLife.TryGetValue(d.Life, out var life))
                {
                    // کدینگ مشترک مال ستاد است؛ کاربر واحد این را از این صفحه رفع نمی‌کند
                    book.Raise($"life:{d.Life}", Sections.Vouchers, $"وضعیت سند قدیم «{d.Life}» در برگهٔ «وضعیت سند» کدینگ مشترک تعیین نشده",
                        "ستاد باید تکلیف این وضعیت را در کاربرگ کدینگ مشترک تعیین و دوباره ثبت کند؛ سپس دوباره «بررسی» بزنید.", [], sample);
                    continue;
                }
                if (life == null) { skippedLife++; continue; }

                if (lines.Sum(l => l.Bed) != lines.Sum(l => l.Bes))
                {
                    var act = map.Unbalanced.TryGetValue(key, out var ub) ? ub.Act : "";
                    if (act == W.USkip) { skippedUnbal++; continue; }
                    if (act == W.UFix)
                    {
                        // قرار بود در سیستم قدیم اصلاح شود ولی هنوز ناتراز است
                        var issue = $"unbal:{key}";
                        var dec = book.Raise(issue, Sections.Vouchers,
                            $"سند {d.No} ({LegacyText.Date8(d.Date)}) سال {y.Year} هنوز ناتراز است (در کاربرگ «در سیستم قدیم اصلاح می‌شود» بود)",
                            "سند را در سیستم قدیم اصلاح کنید و دوباره «بررسی» بزنید، یا تکلیفش را همین‌جا تعیین کنید.",
                            [new(IssueOptions.Draft, "به‌صورت یادداشت (ناتراز) منتقل شود"), new(IssueOptions.Skip, "این سند منتقل نشود")], sample);
                        if (dec == null) continue;
                        book.Resolve(issue);
                        if (dec.Option == IssueOptions.Skip) { skippedUnbal++; continue; }
                    }
                    if (act == "") report.Warnings.Add($"سند ناتراز {d.No} ({d.Date}) سال {y.Year} در کاربرگ نبود؛ به‌صورت یادداشت منتقل می‌شود");
                    life = DocLife.Draft;
                    drafts++;
                }
                plans.Add(new Plan(y, d, lines, life.Value));
            }
        }

        if (skippedLife > 0) book.Note(Sections.Vouchers, $"{skippedLife} سند به‌خاطر وضعیتشان («منتقل نشود» در برگهٔ وضعیت سند) منتقل نمی‌شوند.");
        if (skippedUnbal > 0) book.Note(Sections.Vouchers, $"{skippedUnbal} سند ناتراز طبق کاربرگ منتقل نمی‌شوند.");
        if (skippedOut > 0) book.Note(Sections.Vouchers, $"{skippedOut} سند بیرون از سال طبق کاربرگ منتقل نمی‌شوند.");
        if (empty > 0) book.Note(Sections.Vouchers, $"{empty} سند بدون ردیف کنار گذاشته شد.");
        if (drafts > 0) book.Note(Sections.Vouchers, $"{drafts} سند ناتراز به‌صورت یادداشت منتقل می‌شوند.");
        if (plans.Count == 0 && report.Ok) report.Warnings.Add($"برای سال {year} سندی برای انتقال پیدا نشد.");

        // ترتیب تاریخ، سپس شمارهٔ قدیم
        return plans.OrderBy(p => p.Doc.Date, StringComparer.Ordinal)
            .ThenBy(p => long.TryParse(p.Doc.No, out var n) ? n : long.MaxValue)
            .ThenBy(p => p.Year.Year).ThenBy(p => p.Doc.Seq).ToList();
    }

    /// <summary>حذف واقعی اسناد انتقال قبلی (خودشان مصنوع انتقال‌اند) با ردیف‌ها و وابسته‌های ردیف.</summary>
    private async Task DeleteAsync(List<Guid> headIds, CancellationToken ct)
    {
        foreach (var chunk in headIds.Chunk(900))
        {
            var heads = chunk.ToList();
            var detailIds = await db.TB_VOUCHERSDETAILs.Where(d => d.VOUCHERSHEAD_ID != null && heads.Contains(d.VOUCHERSHEAD_ID.Value))
                .Select(d => d.ID).ToListAsync(ct);
            foreach (var dc in detailIds.Chunk(900))
            {
                var details = dc.ToList();
                await db.TB_VOUCHERDETAIL_LINK_TAFSILIs.Where(x => details.Contains(x.VOUCHERSDETAIL_ID)).ExecuteDeleteAsync(ct);
                await db.TB_ATTRIBSINVOUCHERs.Where(x => details.Contains(x.VOUCHERSDETAIL_ID)).ExecuteDeleteAsync(ct);
                await db.TB_IDENTITYDETAILs.Where(x => details.Contains(x.VOUCHERSDETAIL_ID)).ExecuteDeleteAsync(ct);
                await db.TB_VOUCHERSDETAILs.Where(x => details.Contains(x.ID)).ExecuteDeleteAsync(ct);
            }
            await db.TB_VOUCHERSHEADs.Where(h => heads.Contains(h.ID)).ExecuteDeleteAsync(ct);
        }
    }

    public static MigratedYear? Read(byte[]? json)
    {
        if (json == null) return null;
        try { return JsonSerializer.Deserialize<MigratedYear>(json); }
        catch (JsonException) { return null; }
    }
}
