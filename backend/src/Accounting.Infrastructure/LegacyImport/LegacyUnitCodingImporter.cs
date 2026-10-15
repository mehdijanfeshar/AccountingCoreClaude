using Accounting.Application.Common.Validation;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;
using W = Accounting.Infrastructure.LegacyImport.LegacyUnitWorkbook;

namespace Accounting.Infrastructure.LegacyImport;

/// <summary>
/// تعریف کدینگ یک واحد در سیستم جدید از روی کاربرگ پرشده: تفصیلی‌ها (اشخاص با کد ملی/شناسهٔ ملی؛ شخصی که در واحد
/// دیگری تعریف شده دوباره ساخته نمی‌شود)، فعالیت/مرکز هزینه، عضویت در گروه تفصیلی برای همین واحد، و اتصال سطح‌های
/// شناور معین‌ها به گروه‌ها. کدینگ گروه/کل/معین باید قبلاً (کدینگ مشترک) ساخته شده باشد.
/// </summary>
internal sealed class LegacyUnitCodingImporter(
    LegacyDbContext db, LegacyUnit u, ChartMapping chart, UnitMapping map, LegacyImportOptions opt, string userId, ImportReport report)
{
    private readonly DateTime _now = DateTime.UtcNow;
    private readonly string _v = u.VahedCode;

    private readonly Dictionary<string, string> _masterOf = new();   // کلید تفصیلی ← کلید ردیف اصلی (بعد از ادغام و یکی‌شدن کد ملی)
    private readonly Dictionary<(string, string), Guid> _accountOf = new();

    /// <summary>کلید ← کد نهایی تفصیلی؛ بعد از اجرا پر می‌شود و در کاربرگ ذخیره‌شده نوشته می‌شود.</summary>
    public Dictionary<string, string> TafsiliCodes { get; } = new();
    public Dictionary<string, string> LowCodes { get; } = new();

    // ===================================================================== اعتبارسنجی
    public async Task<bool> ValidateAsync(CancellationToken ct)
    {
        // کاربرگ‌های پیش از اصلاح فرمول: «~» در کلید برای MATCH اکسل نویسهٔ ویژه است و کلید مقصدِ موجود را «نیست»
        // نشان می‌داد؛ این‌جا خود سرور وجود کلید را می‌سنجد.
        static bool MergeTargetOk(TafMap t, UnitMapping m) =>
            t.Act == W.ActMerge && t.Control == "کلید مقصد در این فهرست نیست" && t.MergeKey != t.Key && m.Tafsili.ContainsKey(t.MergeKey);
        foreach (var t in map.Tafsili.Values.Where(t => t.Control != "OK" && !MergeTargetOk(t, map)))
            report.Errors.Add($"برگهٔ {W.TafSheet} ردیف {t.Row} ({t.Key}): {(t.Control == "" ? "تکمیل نشده" : t.Control)}");
        foreach (var l in map.Lows.Values.Where(l => l.Control != "OK"))
            report.Errors.Add($"برگهٔ {W.LowSheet} ردیف {l.Row} ({l.Key}): {(l.Control == "" ? "تکمیل نشده" : l.Control)}");
        foreach (var (key, x) in map.Unbalanced.Where(x => x.Value.Control != "OK"))
            report.Errors.Add($"برگهٔ {W.UnbalSheet} ردیف {x.Row} (سند {key}): تکمیل نشده");
        foreach (var (key, x) in map.OutOfYear.Where(x => x.Value.Control != "OK"))
            report.Errors.Add($"برگهٔ {W.OutSheet} ردیف {x.Row} (سند {key}): تکمیل نشده");
        report.Errors.AddRange(chart.DocLifeProblems);

        // اشخاص: دوباره در سرور (فرمول Excel را می‌شود دور زد)
        foreach (var t in map.Tafsili.Values.Where(t => t.Act == W.ActNew))
        {
            if (t.Person == W.Natural && !NationalIdentifier.IsValidNationalCode(t.NationalId))
                report.Errors.Add($"برگهٔ {W.TafSheet} ردیف {t.Row}: کد ملی «{t.NationalId}» نامعتبر است");
            else if (t.Person == W.Legal && !NationalIdentifier.IsValidLegalId(t.NationalId))
                report.Errors.Add($"برگهٔ {W.TafSheet} ردیف {t.Row}: شناسهٔ ملی «{t.NationalId}» نامعتبر است");
            else if (t.Person is not ("" or W.Natural or W.Legal or W.Other))
                report.Errors.Add($"برگهٔ {W.TafSheet} ردیف {t.Row}: نوع شخص «{t.Person}» نامعتبر است");
            if (t.NewName == "" && LegacyText.Norm(t.OldName).Length < 3)
                report.Errors.Add($"برگهٔ {W.TafSheet} ردیف {t.Row}: نام تفصیلی خالی است؛ ستون نام جدید را پر کنید");
        }

        // ادغام‌ها ← ردیف اصلی؛ سپس ردیف‌های اصلی هم‌کد ملی ← یک ردیف
        foreach (var t in map.Tafsili.Values.Where(t => t.Act is W.ActNew or W.ActMerge))
        {
            var cur = t;
            var last = t.Key;
            for (var guard = 0; cur is { Act: W.ActMerge } && guard < 50; guard++)
            {
                last = cur.MergeKey;
                cur = map.Tafsili.GetValueOrDefault(cur.MergeKey);
            }
            if (cur is { Act: W.ActNew }) { _masterOf[t.Key] = cur.Key; continue; }

            // کجا قطع شد: مقصدِ نیست / «بدون تفصیلی» (000000 یا اقدام بدون تفصیلی) / حلقه
            var why = cur == null ? $"کلید «{last}» در برگه نیست"
                : cur.Act == W.ActNone || cur.Key.Split('/').ElementAtOrDefault(2)?.StartsWith("000000") == true
                    ? $"مقصد «{cur.Key}» بدون تفصیلی (خود معین) است؛ اگر همین منظور است اقدام این ردیف را «{W.ActNone}» بگذارید، وگرنه «{W.ActNew}»"
                : cur.Act == W.ActMerge ? "ادغام‌ها حلقه ساخته‌اند"
                : $"اقدام مقصد «{cur.Key}» خالی است";
            report.Errors.Add($"برگهٔ {W.TafSheet} ردیف {t.Row} ({t.Key}): مقصد ادغام به یک «{W.ActNew}» نمی‌رسد — {why}");
        }
        var byNationalId = map.Tafsili.Values.Where(t => t.Act == W.ActNew && IsPerson(t))
            .GroupBy(t => t.NationalId).ToDictionary(g => g.Key, g => g.First().Key);
        foreach (var (key, master) in _masterOf.ToList())
        {
            var m = map.Tafsili[master];
            if (IsPerson(m) && byNationalId.TryGetValue(m.NationalId, out var canon) && canon != master)
            {
                _masterOf[key] = canon;
                if (key == master)
                    report.Warnings.Add($"«{m.OldName}» ({key}) و «{map.Tafsili[canon].OldName}» کد ملی یکسان {m.NationalId} دارند و یکی می‌شوند");
            }
        }

        // کاربرگ باید با دادهٔ فعلی سیستم قدیم بخواند
        foreach (var y in u.Years)
            foreach (var hes in y.Lines.Select(l => l.HesSeq).Distinct())
                if (y.CodeBySeq.TryGetValue(hes, out var c) && c.IsTafsili && !map.Tafsili.ContainsKey(y.TafsiliKey[hes]))
                    report.Errors.Add($"تفصیلی {y.TafsiliKey[hes]} (سال {y.Year}) در کاربرگ نیست؛ کاربرگ را دوباره دریافت کنید");

        // معین‌ها باید در کدینگ مشترک ساخته شده باشند
        var accounts = (await db.TB_ACCOUNTCODEs.AsNoTracking()
                .Where(a => a.TYPECODE == TypeCodes.Moin && a.ISDELETED != true)
                .Select(a => new { a.ID, a.ACCCODE }).ToListAsync(ct))
            .Where(a => a.ACCCODE != null).GroupBy(a => a.ACCCODE!.Trim()).ToDictionary(g => g.Key, g => g.First().ID);
        var needed = u.Years.SelectMany(y => y.Lines.Select(l => y.MoeinOf(l.HesSeq))
                .Concat(y.LowLinks.Select(l => y.MoeinOf(l.HesSeq))))
            .Where(m => m != null).Select(m => m!.Value).Distinct();
        foreach (var key in needed)
        {
            if (!chart.Moeins.TryGetValue(key, out var mm) || mm.Final == "")
                report.Errors.Add($"معین قدیم {key.Item1}/{key.Item2} در کدینگ مشترک نیست؛ مدیر ستاد باید آن را به کدینگ مشترک اضافه کند");
            else if (!accounts.TryGetValue(mm.Final, out var id))
                report.Errors.Add($"معین {mm.Final} (قدیم {key.Item1}/{key.Item2}) هنوز در سیستم جدید ساخته نشده؛ کدینگ مشترک را کامل کنید");
            else _accountOf[key] = id;
        }
        return report.Ok;
    }

    private static bool IsPerson(TafMap t) => t.Person is W.Natural or W.Legal;

    // ===================================================================== اجرا
    public async Task RunAsync(CancellationToken ct)
    {
        var levels = (await db.TB_LEVEL_TAFSILs.AsNoTracking().Where(l => !l.ISDELETED).ToListAsync(ct))
            .GroupBy(l => l.LEVEL_CODE.Trim()).ToDictionary(g => g.Key, g => g.First().ID);
        foreach (var lv in map.Tafsili.Values.Where(t => t.Level != null).Select(t => t.Level!.Value.ToString()).Append("1").Append("2").Distinct())
            if (!levels.ContainsKey(lv)) report.Errors.Add($"سطح شناور {lv} در «سطوح تفصیلی» تعریف نشده");
        if (!report.Ok) return;

        // گروه‌های تفصیلی (سراسری؛ بر اساس نام)
        var groups = await db.TB_TAFSIL_GROUPs.ToListAsync(ct);
        var usedGroupCodes = groups.Select(g => g.TAFSILGROUP_CODE.Trim()).ToHashSet();
        var groupByName = groups.Where(g => !g.ISDELETED).GroupBy(g => LegacyText.Norm(g.TAFSILGROUP_NAME)).ToDictionary(g => g.Key, g => g.First().ID);
        Guid Group(string name)
        {
            var key = LegacyText.Norm(name);
            if (groupByName.TryGetValue(key, out var id)) return id;
            var code = Enumerable.Range(100, 900).Select(i => i.ToString()).First(c => !usedGroupCodes.Contains(c));
            usedGroupCodes.Add(code);
            var g = new TB_TAFSIL_GROUP
            {
                ID = Guid.NewGuid(), TAFSILGROUP_CODE = code, TAFSILGROUP_NAME = LegacyText.Cut(name, 100),
                ADDUSERID = userId, CREATEDDATE = _now, ISDELETED = false,
            };
            db.TB_TAFSIL_GROUPs.Add(g);
            groupByName[key] = g.ID;
            report.Info.Add($"گروه تفصیلی تازه: {code} «{name}»");
            return g.ID;
        }
        var activityGroup = Group(opt.ActivityGroupName);
        var costGroup = Group(opt.CostCenterGroupName);

        // تفصیلی‌های موجود (کد سراسری یکتاست — UK_TASILI، حتی برای حذف‌شده)
        var existing = (await db.TB_TAFSILIs.Select(t => new { t.ID, t.TAFSILI_CODE, t.ISDELETED }).ToListAsync(ct))
            .Where(t => t.TAFSILI_CODE != null).GroupBy(t => t.TAFSILI_CODE!.Trim()).ToDictionary(g => g.Key, g => g.First());
        long seq = existing.Keys.Where(c => c.Length == _v.Length + 6 && c.StartsWith(_v, StringComparison.Ordinal) && long.TryParse(c[_v.Length..], out _))
            .Select(c => long.Parse(c[_v.Length..])).DefaultIfEmpty(0).Max();
        string NextCode()
        {
            string c;
            do c = _v + (++seq).ToString("D6"); while (existing.ContainsKey(c));
            return c;
        }

        Guid NewTafsili(string code, string name, PersonTypes personType)
        {
            var t = new TB_TAFSILI
            {
                ID = Guid.NewGuid(), TAFSILI_CODE = code, TAFSILI_NAME = LegacyText.Cut(name, 200),
                ISACTIVE = TafsiliActiveState.IsActive, PERSONTYPE = personType,
                OWNER = Owners.Unit, VAHEDTYPE = (VahedCategory)opt.TafsiliVahedType, VAHEDCODE = _v,
                ADDUSERID = userId, CREATEDDATE = _now, ISDELETED = false,
            };
            db.TB_TAFSILIs.Add(t);
            return t.ID;
        }

        // ردیف‌های اصلی
        var idOfMaster = new Dictionary<string, Guid>();
        var codeOfMaster = new Dictionary<string, string>();
        var memberships = new HashSet<(Guid Tafsil, Guid Group)>();
        int created = 0, reused = 0;
        foreach (var key in _masterOf.Values.Distinct())
        {
            var t = map.Tafsili[key];
            var name = t.NewName != "" ? t.NewName : LegacyText.Clean(t.OldName);
            Guid id;
            string code;
            if (IsPerson(t) && existing.TryGetValue(t.NationalId, out var ex))
            {
                id = ex.ID; code = t.NationalId; reused++;
                if (ex.ISDELETED == true) report.Warnings.Add($"تفصیلی حذف‌شدهٔ {code} دوباره استفاده شد؛ وضعیت آن را بررسی کنید");
            }
            else
            {
                code = IsPerson(t) ? t.NationalId : NextCode();
                id = NewTafsili(code, name, t.Person == W.Natural ? PersonTypes.Person : t.Person == W.Legal ? PersonTypes.Legal : PersonTypes.Other);
                existing[code] = new { ID = id, TAFSILI_CODE = (string?)code, ISDELETED = (bool?)false };
                created++;
            }
            idOfMaster[key] = id;
            codeOfMaster[key] = code;
            memberships.Add((id, Group(t.Group)));
        }
        foreach (var (key, master) in _masterOf) TafsiliCodes[key] = codeOfMaster[master];
        if (reused > 0) report.Info.Add($"{reused} شخص از قبل (در واحد دیگری) تعریف شده بود و همان تفصیلی برای این واحد هم فعال شد.");

        // فعالیت و مرکز هزینه
        var lowId = new Dictionary<string, Guid>();
        foreach (var l in map.Lows.Values.Where(l => l.Act == W.LowMove))
        {
            var isCost = !l.Key.EndsWith('/');
            var code = NextCode();
            var id = NewTafsili(code, l.NewName != "" ? l.NewName : LegacyText.Clean(l.OldName), PersonTypes.Other);
            existing[code] = new { ID = id, TAFSILI_CODE = (string?)code, ISDELETED = (bool?)false };
            lowId[l.Key] = id;
            LowCodes[l.Key] = code;
            memberships.Add((id, isCost ? costGroup : activityGroup));
        }

        // عضویت در گروه — مخصوص همین واحد (برای شخص بازاستفاده‌شده هم لازم است)
        var tafsilIds = memberships.Select(m => m.Tafsil).ToList();
        var have = (await db.TB_TAFSIL_LINK_TAFSILGROUPs.AsNoTracking()
                .Where(x => tafsilIds.Contains(x.TAFSIL_ID) && x.VAHEDCODE == _v && !x.ISDELETED)
                .Select(x => new { x.TAFSIL_ID, x.TAFSILGROUP_ID }).ToListAsync(ct))
            .Select(x => (x.TAFSIL_ID, x.TAFSILGROUP_ID)).ToHashSet();
        foreach (var (tafsil, group) in memberships.Where(m => !have.Contains(m)))
            db.TB_TAFSIL_LINK_TAFSILGROUPs.Add(new TB_TAFSIL_LINK_TAFSILGROUP
            {
                ID = Guid.NewGuid(), TAFSIL_ID = tafsil, TAFSILGROUP_ID = group, VAHEDCODE = _v,
                ADDUSERID = userId, CREATEDDATE = _now, ISDELETED = false,
            });

        // سطح‌های شناور معین ← گروه (سراسری؛ اگر هست دوباره ساخته نمی‌شود)
        var need = new HashSet<(Guid Account, Guid Level, Guid Group)>();
        foreach (var (key, master) in _masterOf)
        {
            var t = map.Tafsili[key];
            if (_accountOf.TryGetValue((t.Kol, t.Moi), out var acc) && t.Level is { } lv)
                need.Add((acc, levels[lv.ToString()], Group(map.Tafsili[master].Group)));
        }
        foreach (var y in u.Years)
            foreach (var (hes, jr) in y.LowLinks)
                if (y.LowBySeq.TryGetValue(jr, out var low) && lowId.ContainsKey(low.Key)
                    && y.MoeinOf(hes) is { } m && _accountOf.TryGetValue(m, out var acc))
                    need.Add(low.IsCostCenter ? (acc, levels["2"], costGroup) : (acc, levels["1"], activityGroup));

        var accIds = need.Select(x => x.Account).Distinct().ToList();
        var groupLinks = await db.TB_ACCOUNT_LINK_TAFSILGROUPs.Where(x => accIds.Contains(x.ACCOUNT_ID)).ToListAsync(ct);
        var levelLinks = await db.TB_ACCOUNT_LINK_LEVELs.Where(x => accIds.Contains(x.ACCOUNT_ID)).ToListAsync(ct);
        int addedGroupLinks = 0, addedLevelLinks = 0;
        foreach (var (acc, level, group) in need)
        {
            var gl = groupLinks.FirstOrDefault(x => x.ACCOUNT_ID == acc && x.LEVEL_ID == level && x.TAFSILGROUP_ID == group);
            if (gl == null)
            {
                db.TB_ACCOUNT_LINK_TAFSILGROUPs.Add(new TB_ACCOUNT_LINK_TAFSILGROUP
                {
                    ID = Guid.NewGuid(), ACCOUNT_ID = acc, LEVEL_ID = level, TAFSILGROUP_ID = group,
                    ADDUSERID = userId, CREATEDDATE = _now, ISDELETED = false,
                });
                addedGroupLinks++;
            }
            else if (gl.ISDELETED) { gl.ISDELETED = false; gl.CHANGEUSERID = userId; gl.UPDATEDDATE = _now; addedGroupLinks++; }
        }
        foreach (var (acc, level) in need.Select(x => (x.Account, x.Level)).Distinct())
        {
            var ll = levelLinks.Where(x => x.ACCOUNT_ID == acc && x.LEVEL_ID == level).ToList();
            if (ll.Count == 0)
            {
                db.TB_ACCOUNT_LINK_LEVELs.Add(new TB_ACCOUNT_LINK_LEVEL
                {
                    ID = Guid.NewGuid(), ACCOUNT_ID = acc, LEVEL_ID = level, ADDUSERID = userId, CREATEDDATE = _now, ISDELETED = false,
                });
                addedLevelLinks++;
            }
            else if (ll.All(x => x.ISDELETED)) { ll[0].ISDELETED = false; ll[0].CHANGEUSERID = userId; ll[0].UPDATEDDATE = _now; addedLevelLinks++; }
        }

        await db.SaveChangesAsync(ct);
        report.Info.Add($"تفصیلی: {created} تعریف شد ({map.Tafsili.Count} ردیف کاربرگ پس از ادغام و یکی‌شدن کد ملی)، " +
                        $"فعالیت/مرکز هزینه: {lowId.Count}، اتصال تازهٔ معین↔گروه: {addedGroupLinks}، معین↔سطح: {addedLevelLinks}.");
    }
}
