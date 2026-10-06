using System.Globalization;
using Accounting.Domain.OperationTemplates;

namespace Accounting.Application.OperationTemplates;

public interface IVoucherGenerationEngine
{
    Task<EngineResult> GenerateAsync(
        OperationTemplate template,
        string vahedCode,
        DateOnly voucherDate,
        IReadOnlyDictionary<string, string?> values,
        CancellationToken ct);
}

/// <summary>
/// موتور قطعی تولید سند از الگو. هیچ هوش مصنوعی اینجا نیست:
/// ورودی یکسان ⇐ همیشه سند یکسان. همهٔ کنترل‌های حسابداری (تراز بودن،
/// تفصیلی‌های الزامی، گروه تفصیلی، تعلق تفصیلی به واحد) همین‌جا انجام می‌شود.
/// </summary>
public sealed class VoucherGenerationEngine : IVoucherGenerationEngine
{
    private readonly ISubsidiaryAccountReader _accounts;
    private readonly IDetailReader _details;

    public VoucherGenerationEngine(ISubsidiaryAccountReader accounts, IDetailReader details)
    {
        _accounts = accounts;
        _details = details;
    }

    public async Task<EngineResult> GenerateAsync(
        OperationTemplate template,
        string vahedCode,
        DateOnly voucherDate,
        IReadOnlyDictionary<string, string?> values,
        CancellationToken ct)
    {
        if (!template.IsActive)
            return EngineResult.Fail(new EngineError(EngineErrorCode.TemplateInactive,
                $"الگوی «{template.Title}» غیرفعال است."));

        // ── ۱. خواندن و تبدیل پارامترها ──
        var errors = new List<EngineError>();
        var paramByKey = template.Parameters.ToDictionary(p => p.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var key in values.Keys.Where(k => !paramByKey.ContainsKey(k)))
            errors.Add(new EngineError(EngineErrorCode.UnknownParameter,
                $"پارامتر «{key}» در این الگو تعریف نشده است.", key));

        var amounts = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        var detailParams = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var texts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in template.Parameters.OrderBy(p => p.SortOrder))
        {
            values.TryGetValue(p.Key, out var raw);
            if (string.IsNullOrWhiteSpace(raw))
            {
                if (p.IsRequired)
                    errors.Add(new EngineError(EngineErrorCode.MissingParameter,
                        $"«{p.Title}» مشخص نشده است.", p.Key, p.AskPrompt));
                continue;
            }

            raw = raw.Trim();
            switch (p.Type)
            {
                case ParameterType.Amount:
                    if (!long.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var amt))
                        errors.Add(Invalid(p, "باید عدد صحیح به ریال باشد."));
                    else if (amt <= 0)
                        errors.Add(new EngineError(EngineErrorCode.NonPositiveAmount,
                            $"«{p.Title}» باید بزرگ‌تر از صفر باشد.", p.Key, p.AskPrompt));
                    else
                        amounts[p.Key] = amt;
                    break;

                case ParameterType.Detail:
                    if (!Guid.TryParse(raw, out var did))
                        errors.Add(Invalid(p, "شناسهٔ تفصیلی نامعتبر است."));
                    else
                        detailParams[p.Key] = did;
                    break;

                case ParameterType.Date:
                    if (!DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out var d))
                        errors.Add(Invalid(p, "تاریخ باید به قالب yyyy-MM-dd باشد."));
                    else
                        // در شرح سند تاریخ شمسی نوشته می‌شود (ورودی API میلادی است).
                        texts[p.Key] = FiscalYearGuard.Jalali(d);
                    break;

                default:
                    texts[p.Key] = raw;
                    break;
            }
        }

        // پارامترهای ناقص را یک‌جا برگردان تا همه با هم پرسیده شوند
        if (errors.Count > 0) return EngineResult.Fail(errors);

        // ── ۲. بارگذاری حساب‌ها و تفصیلی‌ها (یک رفت‌وبرگشت برای هر کدام) ──
        var lines = template.Lines.OrderBy(l => l.SortOrder).ToList();

        var accountIds = lines.Select(l => l.SubsidiaryAccountId).Distinct().ToList();
        var detailIds = detailParams.Values
            .Concat(lines.SelectMany(l => l.Details).Where(d => d.FixedDetailId.HasValue)
                         .Select(d => d.FixedDetailId!.Value))
            .Distinct().ToList();

        var accounts = await _accounts.GetByIdsAsync(accountIds, ct);
        var details = await _details.GetByIdsAsync(detailIds, vahedCode, ct);

        // ── ۳. اعتبارسنجی تفصیلی‌های ورودی کاربر ──
        foreach (var (key, detailId) in detailParams)
        {
            var p = paramByKey[key];
            if (!details.TryGetValue(detailId, out var det))
            {
                errors.Add(new EngineError(EngineErrorCode.DetailNotFound,
                    $"«{p.Title}» انتخاب‌شده یافت نشد.", key, p.AskPrompt));
                continue;
            }
            CheckDetailUsable(det, errors, key, p.AskPrompt);
            if (p.DetailGroupId.HasValue && det.IsVisibleToUnit && !det.DetailGroupIds.Contains(p.DetailGroupId.Value))
                errors.Add(new EngineError(EngineErrorCode.DetailWrongGroup,
                    $"«{det.Title}» از نوع مورد انتظار برای «{p.Title}» نیست.", key, p.AskPrompt));
        }
        if (errors.Count > 0) return EngineResult.Fail(errors);

        // ── ۴. ساخت ردیف‌ها ──
        var built = new List<(TemplateLine Tpl, SubsidiaryAccountInfo Acc, List<VoucherDraftDetail> Dets, long Amount)>();

        foreach (var line in lines)
        {
            if (!accounts.TryGetValue(line.SubsidiaryAccountId, out var acc))
            {
                errors.Add(new EngineError(EngineErrorCode.AccountNotFound,
                    $"معین شمارهٔ {line.SubsidiaryAccountId} در الگو یافت نشد (خطای تعریف الگو)."));
                continue;
            }
            if (!acc.IsActive)
            {
                errors.Add(new EngineError(EngineErrorCode.AccountInactive,
                    $"معین «{acc.Title}» غیرفعال است (خطای تعریف الگو)."));
                continue;
            }

            var lineDetails = ResolveLineDetails(line, acc, detailParams, details, errors);

            long amount = 0;
            if (!line.IsBalancingLine)
            {
                if (!amounts.TryGetValue(line.AmountParameterKey, out var baseAmount))
                {
                    // پارامتر مبلغ اختیاری و خالی ⇐ این ردیف تولید نمی‌شود
                    if (paramByKey.TryGetValue(line.AmountParameterKey, out var ap) && !ap.IsRequired)
                        continue;
                    errors.Add(new EngineError(EngineErrorCode.InvalidParameterValue,
                        $"مبلغ ردیف «{acc.Title}» قابل محاسبه نیست (خطای تعریف الگو)."));
                    continue;
                }
                amount = (long)Math.Round(baseAmount * line.Percent / 100m, MidpointRounding.AwayFromZero);
                if (amount <= 0)
                {
                    errors.Add(new EngineError(EngineErrorCode.NonPositiveAmount,
                        $"مبلغ ردیف «{acc.Title}» صفر شد."));
                    continue;
                }
            }

            built.Add((line, acc, lineDetails, amount));
        }
        if (errors.Count > 0) return EngineResult.Fail(errors);

        // ── ۵. ردیف تراز‌کننده ──
        long debit = built.Where(b => !b.Tpl.IsBalancingLine && b.Tpl.Side == LineSide.Debit).Sum(b => b.Amount);
        long credit = built.Where(b => !b.Tpl.IsBalancingLine && b.Tpl.Side == LineSide.Credit).Sum(b => b.Amount);

        for (int i = 0; i < built.Count; i++)
        {
            if (!built[i].Tpl.IsBalancingLine) continue;
            long gap = built[i].Tpl.Side == LineSide.Debit ? credit - debit : debit - credit;
            if (gap <= 0)
                return EngineResult.Fail(new EngineError(EngineErrorCode.Unbalanced,
                    "ردیف تراز‌کننده مبلغ مثبت پیدا نکرد (خطای تعریف الگو)."));
            built[i] = built[i] with { Amount = gap };
            if (built[i].Tpl.Side == LineSide.Debit) debit += gap; else credit += gap;
        }

        // ── ۶. کنترل نهایی تراز ── (در کد فعلی پروژه Debit==Credit تضمین نشده؛ اینجا قطعی است)
        if (debit != credit || debit == 0)
            return EngineResult.Fail(new EngineError(EngineErrorCode.Unbalanced,
                $"سند تراز نیست: بدهکار {debit:N0} ≠ بستانکار {credit:N0} (خطای تعریف الگو)."));

        // ── ۷. شرح‌ها ──
        var placeholders = BuildPlaceholders(template, amounts, detailParams, details, texts);
        var voucherDesc = Render(template.VoucherDescriptionPattern, placeholders);

        var draftLines = built.Select(b => new VoucherDraftLine(
            b.Acc.Id,
            b.Acc.Title,
            b.Dets,
            b.Tpl.Side == LineSide.Debit ? b.Amount : 0,
            b.Tpl.Side == LineSide.Credit ? b.Amount : 0,
            string.IsNullOrWhiteSpace(b.Tpl.DescriptionPattern)
                ? voucherDesc
                : Render(b.Tpl.DescriptionPattern!, placeholders),
            SubsidiaryAccountCode: b.Acc.Code)).ToList();

        return EngineResult.Ok(new VoucherDraft(vahedCode, voucherDate, voucherDesc, template.Code, draftLines,
            SystemTypeId: template.SystemTypeId));
    }

    private static List<VoucherDraftDetail> ResolveLineDetails(
        TemplateLine line,
        SubsidiaryAccountInfo acc,
        IReadOnlyDictionary<string, Guid> detailParams,
        IReadOnlyDictionary<Guid, DetailInfo> details,
        List<EngineError> errors)
    {
        var result = new List<VoucherDraftDetail>();
        var rulesByLevel = acc.DetailLevels.ToDictionary(r => r.Level);

        foreach (var map in line.Details.OrderBy(d => d.Level))
        {
            if (!rulesByLevel.TryGetValue(map.Level, out var rule))
            {
                errors.Add(new EngineError(EngineErrorCode.DetailLevelNotDefined,
                    $"معین «{acc.Title}» سطح تفصیلی {map.Level} ندارد (خطای تعریف الگو)."));
                continue;
            }

            Guid? detailId = map.FixedDetailId
                ?? (map.ParameterKey is not null && detailParams.TryGetValue(map.ParameterKey, out var v)
                        ? (Guid?)v
                        : null);

            if (detailId is null) continue; // پارامتر اختیاری خالی؛ اگر سطح الزامی باشد پایین‌تر خطا می‌گیرد

            if (!details.TryGetValue(detailId.Value, out var det))
            {
                errors.Add(new EngineError(EngineErrorCode.DetailNotFound,
                    $"تفصیلی {detailId} یافت نشد.", map.ParameterKey));
                continue;
            }
            if (map.FixedDetailId.HasValue) CheckDetailUsable(det, errors, null, null);

            if (!det.DetailGroupIds.Overlaps(rule.DetailGroupIds))
            {
                errors.Add(new EngineError(EngineErrorCode.DetailWrongGroup,
                    $"«{det.Title}» برای سطح {map.Level} معین «{acc.Title}» مجاز نیست.", map.ParameterKey));
                continue;
            }
            result.Add(new VoucherDraftDetail(map.Level, rule.LevelId, det.Id, det.Title));
        }

        foreach (var rule in acc.DetailLevels.Where(r => r.IsRequired))
            if (result.All(r => r.Level != rule.Level))
                errors.Add(new EngineError(EngineErrorCode.MissingRequiredDetailLevel,
                    $"تفصیلی سطح {rule.Level} برای معین «{acc.Title}» الزامی است."));

        return result;
    }

    private static void CheckDetailUsable(DetailInfo det, List<EngineError> errors,
        string? key, string? ask)
    {
        if (!det.IsActive)
            errors.Add(new EngineError(EngineErrorCode.DetailInactive, $"«{det.Title}» غیرفعال است.", key, ask));
        // قاعدهٔ B را خوانندهٔ تفصیلی با واحد کاربر حساب کرده است
        if (!det.IsVisibleToUnit)
            errors.Add(new EngineError(EngineErrorCode.DetailNotInUnit,
                $"«{det.Title}» متعلق به واحد شما نیست.", key, ask));
    }

    private static Dictionary<string, string> BuildPlaceholders(
        OperationTemplate t,
        IReadOnlyDictionary<string, long> amounts,
        IReadOnlyDictionary<string, Guid> detailParams,
        IReadOnlyDictionary<Guid, DetailInfo> details,
        IReadOnlyDictionary<string, string> texts)
    {
        var ph = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in t.Parameters)
        {
            if (amounts.TryGetValue(p.Key, out var a)) ph[p.Key] = a.ToString("N0", CultureInfo.InvariantCulture) + " ریال";
            else if (detailParams.TryGetValue(p.Key, out var d) && details.TryGetValue(d, out var di)) ph[p.Key] = di.Title;
            else if (texts.TryGetValue(p.Key, out var s)) ph[p.Key] = s;
            else ph[p.Key] = string.Empty;
        }
        return ph;
    }

    private static string Render(string pattern, IReadOnlyDictionary<string, string> ph)
    {
        var s = pattern;
        foreach (var (k, v) in ph) s = s.Replace("{" + k + "}", v, StringComparison.OrdinalIgnoreCase);
        // جداکنندهٔ اضافی ناشی از پارامتر خالی را تمیز کن: «دریافت از علی - »
        return RemoveRepeatedWords(s.Trim().TrimEnd('-', '،', ',').Trim());
    }

    /// <summary>کلمه‌های ربط که تکرارشان طبیعی است و حذف نمی‌شوند («از … از …»).</summary>
    private static readonly HashSet<string> Connectors = new(StringComparer.Ordinal)
    {
        "از", "به", "با", "و", "در", "برای", "بابت", "جهت", "تا", "را", "که", "-", "،", ",", "/",
    };

    /// <summary>
    /// شرح بدون کلمهٔ تکراری: الگوی «خرید دارو {note1}» + توضیح «خرید دارو از شرکت هجرت» ⇒
    /// «خرید دارو از شرکت هجرت». فقط تکرارهای بعدی کلمه‌های معنادار حذف می‌شوند؛ کلمه‌های ربط و
    /// هر چیزی که رقم دارد (مبلغ، تاریخ، شماره) دست نمی‌خورد. ی/ک عربی و نیم‌فاصله یکسان دیده می‌شوند.
    /// </summary>
    internal static string RemoveRepeatedWords(string text)
    {
        // جداکننده‌ها توکن جدا: «شرکت-خرید» ⇒ «شرکت - خرید» تا «خرید» تکراری دیده شود.
        // بین دو رقم (تاریخ 1404-07-06، مبلغ 12,000) دست نمی‌خورد؛ «/» هم هرگز (تاریخ 1404/07/06).
        // هر نوع خط تیره (‐ – — و کشیدهٔ «ـ»)، ویرگول فارسی/لاتین، «؛» و «|».
        var spaced = System.Text.RegularExpressions.Regex.Replace(text, @"\s*([\p{Pd}ـ،,؛;|])\s*", m =>
        {
            var before = m.Index > 0 ? text[m.Index - 1] : ' ';
            var after = m.Index + m.Length < text.Length ? text[m.Index + m.Length] : ' ';
            return char.IsDigit(before) && char.IsDigit(after) ? m.Value : $" {m.Groups[1].Value} ";
        });
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var kept = new List<string>();
        foreach (var word in spaced.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var key = word.Replace('ي', 'ی').Replace('ك', 'ک').Replace("‌", "").Trim('،', ',', '.', '(', ')');
            var meaningful = key.Length >= 2 && !Connectors.Contains(key) && !key.Any(char.IsDigit);
            if (meaningful && !seen.Add(key))
                continue;
            // دو جداکنندهٔ پشت‌سرهم (کلمهٔ بینشان حذف شد) یکی شوند.
            if (IsSeparator(word) && kept.Count > 0 && IsSeparator(kept[^1]))
                continue;
            kept.Add(word);
        }
        // «از» یا «-» که کلمهٔ بعدش حذف شد و حالا تنها مانده هم نماند.
        while (kept.Count > 0 && (Connectors.Contains(kept[^1]) || IsSeparator(kept[^1])))
            kept.RemoveAt(kept.Count - 1);
        return string.Join(' ', kept);
    }

    private static bool IsSeparator(string w) =>
        w.Length == 1 && (char.GetUnicodeCategory(w[0]) == System.Globalization.UnicodeCategory.DashPunctuation || "ـ،,؛;|".Contains(w[0]));

    private static EngineError Invalid(TemplateParameter p, string why) =>
        new(EngineErrorCode.InvalidParameterValue, $"«{p.Title}» نامعتبر است: {why}", p.Key, p.AskPrompt);
}
