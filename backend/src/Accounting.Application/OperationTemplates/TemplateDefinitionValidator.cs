using System.Text.RegularExpressions;
using Accounting.Domain.OperationTemplates;

namespace Accounting.Application.OperationTemplates;

/// <summary>
/// اعتبارسنجی تعریف الگو هنگام ذخیره توسط ستاد.
/// هدف: الگوی معیوب هرگز به دست کاربر واحد نرسد. هر خطایی که موتور با
/// برچسب «خطای تعریف الگو» می‌دهد، باید ایده‌آلاً همین‌جا گرفته شده باشد.
/// </summary>
public sealed partial class TemplateDefinitionValidator
{
    private readonly ISubsidiaryAccountReader _accounts;

    public TemplateDefinitionValidator(ISubsidiaryAccountReader accounts) => _accounts = accounts;

    [GeneratedRegex("^[A-Z][A-Z0-9_]{2,49}$")] private static partial Regex CodeRx();
    [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9]{0,39}$")] private static partial Regex KeyRx();
    [GeneratedRegex(@"\{([^{}]+)\}")] private static partial Regex PlaceholderRx();

    public async Task<IReadOnlyList<string>> ValidateAsync(OperationTemplate t, CancellationToken ct)
    {
        var e = new List<string>();

        if (string.IsNullOrWhiteSpace(t.Code) || !CodeRx().IsMatch(t.Code))
            e.Add("کد الگو باید با حروف بزرگ لاتین، عدد و _ باشد (مثل RECEIVE_FROM_CUSTOMER).");
        if (string.IsNullOrWhiteSpace(t.Title)) e.Add("عنوان الگو الزامی است.");
        if (string.IsNullOrWhiteSpace(t.Description)) e.Add("توضیح الگو الزامی است (در فاز Agent استفاده می‌شود).");
        if (string.IsNullOrWhiteSpace(t.VoucherDescriptionPattern)) e.Add("الگوی شرح سند الزامی است.");
        if ((t.Keywords?.Length ?? 0) > 1000) e.Add("کلمات کلیدی حداکثر ۱۰۰۰ کاراکتر است.");
        if ((t.AllowedVahedTypes?.Length ?? 0) > 400) e.Add("فهرست نوع واحدهای مجاز طولانی‌تر از حد است.");
        if ((t.PlainPattern?.Length ?? 0) > 1000) e.Add("جملهٔ ساده حداکثر ۱۰۰۰ کاراکتر است.");
        if (t.MaxAmount is <= 0) e.Add("سقف مبلغ باید بزرگ‌تر از صفر باشد.");
        if (t.RiskBase is { } rb && !Enum.IsDefined(rb)) e.Add("ریسک پایه نامعتبر است (۱ پایین، ۲ متوسط، ۳ بالا).");
        if (t.Category is { } cat && !Enum.IsDefined(cat)) e.Add("دستهٔ الگو نامعتبر است.");

        // ── پارامترها ──
        var pByKey = new Dictionary<string, TemplateParameter>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in t.Parameters)
        {
            if (string.IsNullOrWhiteSpace(p.Key) || !KeyRx().IsMatch(p.Key))
            { e.Add($"کلید پارامتر «{p.Key}» نامعتبر است."); continue; }
            if (!pByKey.TryAdd(p.Key, p)) e.Add($"کلید پارامتر «{p.Key}» تکراری است.");
            if (string.IsNullOrWhiteSpace(p.Title)) e.Add($"عنوان پارامتر «{p.Key}» الزامی است.");
            if (string.IsNullOrWhiteSpace(p.AskPrompt)) e.Add($"سؤال پارامتر «{p.Key}» الزامی است.");
            if (p.Type == ParameterType.Detail && p.DetailGroupId is null)
                e.Add($"پارامتر تفصیلی «{p.Key}» باید گروه تفصیلی داشته باشد.");
            if (p.Type != ParameterType.Detail && p.DetailGroupId is not null)
                e.Add($"پارامتر «{p.Key}» تفصیلی نیست و نباید گروه تفصیلی داشته باشد.");
            if (p.Type == ParameterType.Choice) ValidateChoice(p, e);
            else if (!string.IsNullOrWhiteSpace(p.OptionsJson))
                e.Add($"پارامتر «{p.Key}» انتخابی نیست و نباید گزینه داشته باشد.");
        }

        // ── ردیف‌ها ── (ردیف «سهم ماهانه» تعهدی جزو سند اصلی نیست؛ قواعدش در AccrualTemplateRules)
        var voucherLines = t.Lines.Where(l => !Accrual.AccrualSchedulePlanner.IsScheduleLine(l)).ToList();
        if (!voucherLines.Any(l => l.Side == LineSide.Debit) || !voucherLines.Any(l => l.Side == LineSide.Credit))
            e.Add("الگو باید حداقل یک ردیف بدهکار و یک ردیف بستانکار داشته باشد.");

        Accrual.AccrualTemplateRules.Validate(t, pByKey, e);

        foreach (var l in t.Lines)
        {
            var name = $"ردیف {l.SortOrder}";
            if (!Accrual.AccrualSchedulePlanner.IsScheduleLine(l))
                ValidateLinePolicy(l, name, pByKey, e);
            if (!l.IsBalancingLine)
            {
                if (!pByKey.TryGetValue(l.AmountParameterKey ?? "", out var ap) || ap.Type != ParameterType.Amount)
                    e.Add($"{name}: پارامتر مبلغ «{l.AmountParameterKey}» وجود ندارد یا از نوع مبلغ نیست.");
                if (l.Percent <= 0 || l.Percent > 1000)
                    e.Add($"{name}: درصد باید بین ۰ و ۱۰۰۰ باشد.");
            }

            if (l.Details.GroupBy(d => d.Level).Any(g => g.Count() > 1))
                e.Add($"{name}: سطح تفصیلی تکراری دارد.");

            foreach (var d in l.Details)
            {
                var hasParam = !string.IsNullOrWhiteSpace(d.ParameterKey);
                if (hasParam == d.FixedDetailId.HasValue)
                    e.Add($"{name}، سطح {d.Level}: دقیقاً یکی از «پارامتر» یا «تفصیلی ثابت» باید پر باشد.");
                else if (hasParam && (!pByKey.TryGetValue(d.ParameterKey!, out var dp) || dp.Type != ParameterType.Detail))
                    e.Add($"{name}، سطح {d.Level}: پارامتر «{d.ParameterKey}» وجود ندارد یا تفصیلی نیست.");
            }
        }

        // ── تراز ساختاری برای هر ترکیب جواب‌های انتخابی (G8): ردیف‌های شرطی هر شاخه باید خودشان تراز باشند ──
        if (e.Count == 0) ValidateBalanceCombinations(voucherLines, pByKey, e);

        // ── Placeholderهای شرح ──
        foreach (var pattern in t.Lines.Select(l => l.DescriptionPattern).Append(t.VoucherDescriptionPattern).Append(t.PlainPattern))
        {
            if (string.IsNullOrEmpty(pattern)) continue;
            foreach (Match m in PlaceholderRx().Matches(pattern))
                if (!pByKey.ContainsKey(m.Groups[1].Value))
                    e.Add($"در شرح، «{{{m.Groups[1].Value}}}» به هیچ پارامتری اشاره نمی‌کند.");
        }

        if (e.Count > 0) return e; // بدون ساختار سالم، چک دیتابیسی معنا ندارد

        // ── سازگاری با تعریف معین‌ها ──
        var accounts = await _accounts.GetByIdsAsync(
            t.Lines.Select(l => l.SubsidiaryAccountId).Distinct().ToList(), ct);

        foreach (var l in t.Lines)
        {
            var name = $"ردیف {l.SortOrder}";
            if (!accounts.TryGetValue(l.SubsidiaryAccountId, out var acc))
            { e.Add($"{name}: معین {l.SubsidiaryAccountId} وجود ندارد."); continue; }
            if (!acc.IsActive) e.Add($"{name}: معین «{acc.Title}» غیرفعال است.");

            var rules = acc.DetailLevels.ToDictionary(r => r.Level);
            foreach (var d in l.Details)
            {
                if (!rules.TryGetValue(d.Level, out var rule))
                { e.Add($"{name}: معین «{acc.Title}» سطح تفصیلی {d.Level} ندارد."); continue; }

                if (d.ParameterKey is not null
                    && !(pByKey[d.ParameterKey].DetailGroupId is Guid groupId && rule.DetailGroupIds.Contains(groupId)))
                    e.Add($"{name}: گروه تفصیلی پارامتر «{d.ParameterKey}» با سطح {d.Level} معین «{acc.Title}» یکی نیست.");
            }

            foreach (var rule in acc.DetailLevels.Where(r => r.IsRequired))
            {
                var map = l.Details.FirstOrDefault(d => d.Level == rule.Level);
                if (map is null)
                    e.Add($"{name}: سطح {rule.Level} برای معین «{acc.Title}» الزامی است ولی در الگو تعیین نشده.");
                else if (map.ParameterKey is not null && !pByKey[map.ParameterKey].IsRequired)
                    e.Add($"{name}: سطح الزامی {rule.Level} به پارامتر اختیاری «{map.ParameterKey}» وصل است.");
            }
        }

        return e;
    }

    // ─────────── G8: سؤال انتخابی ───────────

    [GeneratedRegex("^[a-zA-Z0-9_]{1,30}$")] private static partial Regex OptionValueRx();

    private static void ValidateChoice(TemplateParameter p, List<string> e)
    {
        if ((p.OptionsJson?.Length ?? 0) > 2000) { e.Add($"گزینه‌های «{p.Key}» طولانی‌تر از حد است."); return; }
        var options = ChoiceOptions.Parse(p.OptionsJson);
        if (options.Count < 2) { e.Add($"سؤال انتخابی «{p.Key}» حداقل دو گزینه لازم دارد."); return; }
        if (options.Count > 20) e.Add($"سؤال انتخابی «{p.Key}» حداکثر ۲۰ گزینه دارد.");
        if (options.GroupBy(o => o.Value, StringComparer.Ordinal).Any(g => g.Count() > 1))
            e.Add($"مقدار گزینه‌های «{p.Key}» تکراری است.");
        foreach (var o in options)
        {
            if (!OptionValueRx().IsMatch(o.Value))
                e.Add($"مقدار گزینهٔ «{o.Value}» در «{p.Key}» باید لاتین/عدد/_ و حداکثر ۳۰ نویسه باشد.");
            else if (string.Equals(o.Value, ChoiceOptions.Unknown, StringComparison.OrdinalIgnoreCase))
                e.Add($"مقدار «{ChoiceOptions.Unknown}» رزرو است؛ «نمی‌دانم» خودکار اضافه می‌شود.");
            if (string.IsNullOrWhiteSpace(o.Label) || o.Label.Length > 100)
                e.Add($"عنوان گزینهٔ «{o.Value}» در «{p.Key}» الزامی و حداکثر ۱۰۰ نویسه است.");
        }
    }

    /// <summary>شرط ردیف (G8) و منبع مبلغ ارزش افزوده (G9).</summary>
    private static void ValidateLinePolicy(TemplateLine l, string name, IReadOnlyDictionary<string, TemplateParameter> pByKey, List<string> e)
    {
        var hasKey = !string.IsNullOrWhiteSpace(l.ConditionParamKey);
        var hasValue = !string.IsNullOrWhiteSpace(l.ConditionValue);
        if (hasKey != hasValue)
            e.Add($"{name}: شرط ردیف باید هم سؤال انتخابی و هم مقدار داشته باشد.");
        else if (hasKey)
        {
            if (!pByKey.TryGetValue(l.ConditionParamKey!, out var cp) || cp.Type != ParameterType.Choice)
                e.Add($"{name}: شرط ردیف باید به یک سؤال انتخابی اشاره کند («{l.ConditionParamKey}» وجود ندارد یا انتخابی نیست).");
            else if (!ChoiceOptions.Parse(cp.OptionsJson).Any(o => o.Value == l.ConditionValue))
                e.Add($"{name}: مقدار شرط «{l.ConditionValue}» جزو گزینه‌های «{cp.Key}» نیست.");
        }

        if (!Enum.IsDefined(l.AmountSource))
            e.Add($"{name}: منبع مبلغ نامعتبر است.");
        else if (l.AmountSource == LineAmountSource.Vat && l.IsBalancingLine)
            e.Add($"{name}: ردیف تراز‌کننده نمی‌تواند ارزش افزوده باشد.");
    }

    private const int MaxCombinations = 512;

    /// <summary>
    /// برای هر ترکیب جواب‌های سؤال‌های انتخابیِ مورد استفاده در شرط‌ها (به‌علاوهٔ «نمی‌دانم»)، ردیف‌های ساخته‌شده
    /// باید حداقل یک بدهکار و یک بستانکار داشته باشند و یا تراز باشند (جمع درصدها) یا دقیقاً یک ردیف تراز‌کننده داشته باشند.
    /// </summary>
    private static void ValidateBalanceCombinations(IReadOnlyList<TemplateLine> lines, IReadOnlyDictionary<string, TemplateParameter> pByKey, List<string> e)
    {
        var keys = lines.Where(l => !string.IsNullOrWhiteSpace(l.ConditionParamKey))
            .Select(l => l.ConditionParamKey!).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var domains = keys.Select(k => ChoiceOptions.Parse(pByKey[k].OptionsJson).Select(o => o.Value).Append(ChoiceOptions.Unknown).ToList()).ToList();

        long total = 1;
        foreach (var d in domains) total *= d.Count;
        if (total > MaxCombinations)
        {
            e.Add($"ترکیب جواب‌های سؤال‌های انتخابیِ شرط‌ها بیش از {MaxCombinations} حالت است؛ شرط‌ها را ساده‌تر کنید.");
            return;
        }

        var seen = new HashSet<string>();
        void Add(string msg) { if (seen.Add(msg)) e.Add(msg); }

        var answers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        void Walk(int i)
        {
            if (i < keys.Count)
            {
                foreach (var v in domains[i]) { answers[keys[i]] = v; Walk(i + 1); }
                return;
            }
            var suffix = keys.Count == 0 ? "" : " (وقتی " + string.Join("، ", keys.Select(k => $"«{pByKey[k].Title}» = {Label(pByKey[k], answers[k])}")) + ")";
            var active = lines.Where(l => TemplateLogic.LineApplies(l, k => answers.GetValueOrDefault(k))).ToList();

            if (!active.Any(l => l.Side == LineSide.Debit) || !active.Any(l => l.Side == LineSide.Credit))
            { Add("سند باید حداقل یک ردیف بدهکار و یک ردیف بستانکار داشته باشد" + suffix + "."); return; }

            var balancing = active.Count(l => l.IsBalancingLine);
            if (balancing > 1) { Add("حداکثر یک ردیف تراز‌کننده مجاز است" + suffix + "."); return; }
            if (balancing == 1) return;

            if (active.Any(l => l.AmountSource == LineAmountSource.Vat))
            { Add("الگویی که ردیف ارزش افزوده دارد باید ردیف تراز‌کننده هم داشته باشد (مبلغ با مالیات تراز می‌شود)" + suffix + "."); return; }

            if (active.Any(l => l.Percent != 100m))
            {
                Add("الگویی که ردیف درصدی (غیر از ۱۰۰٪) دارد باید ردیف تراز‌کننده داشته باشد؛ " +
                    "وگرنه گرد کردن ممکن است سند را یک ریال نامتراز کند" + suffix + ".");
                return;
            }

            foreach (var g in active.GroupBy(l => l.AmountParameterKey ?? "", StringComparer.OrdinalIgnoreCase))
            {
                var dr = g.Where(l => l.Side == LineSide.Debit).Sum(l => l.Percent);
                var cr = g.Where(l => l.Side == LineSide.Credit).Sum(l => l.Percent);
                if (dr != cr)
                    Add($"برای مبلغ «{g.Key}» جمع درصد بدهکار ({dr}) با بستانکار ({cr}) برابر نیست" + suffix + "؛ " +
                        "یا درصدها را اصلاح کنید یا یک ردیف تراز‌کننده تعریف کنید.");
            }
        }
        Walk(0);
    }

    private static string Label(TemplateParameter p, string value) =>
        value == ChoiceOptions.Unknown ? ChoiceOptions.UnknownLabel
            : ChoiceOptions.Parse(p.OptionsJson).FirstOrDefault(o => o.Value == value)?.Label ?? value;
}
