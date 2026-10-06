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
        }

        // ── ردیف‌ها ──
        if (!t.Lines.Any(l => l.Side == LineSide.Debit) || !t.Lines.Any(l => l.Side == LineSide.Credit))
            e.Add("الگو باید حداقل یک ردیف بدهکار و یک ردیف بستانکار داشته باشد.");

        var balancingCount = t.Lines.Count(l => l.IsBalancingLine);
        if (balancingCount > 1) e.Add("حداکثر یک ردیف تراز‌کننده مجاز است.");

        foreach (var l in t.Lines)
        {
            var name = $"ردیف {l.SortOrder}";
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

        // ── تراز ساختاری: بدون ردیف تراز‌کننده، برای هر پارامتر مبلغ جمع درصدها باید برابر باشد ──
        if (balancingCount == 0 && t.Lines.Any(l => l.Percent != 100m))
            e.Add("الگویی که ردیف درصدی (غیر از ۱۰۰٪) دارد باید ردیف تراز‌کننده داشته باشد؛ " +
                  "وگرنه گرد کردن ممکن است سند را یک ریال نامتراز کند.");

        if (balancingCount == 0)
        {
            foreach (var g in t.Lines.GroupBy(l => l.AmountParameterKey, StringComparer.OrdinalIgnoreCase))
            {
                var dr = g.Where(l => l.Side == LineSide.Debit).Sum(l => l.Percent);
                var cr = g.Where(l => l.Side == LineSide.Credit).Sum(l => l.Percent);
                if (dr != cr)
                    e.Add($"برای مبلغ «{g.Key}» جمع درصد بدهکار ({dr}) با بستانکار ({cr}) برابر نیست؛ " +
                          "یا درصدها را اصلاح کنید یا یک ردیف تراز‌کننده تعریف کنید.");
            }
        }

        // ── Placeholderهای شرح ──
        foreach (var pattern in t.Lines.Select(l => l.DescriptionPattern).Append(t.VoucherDescriptionPattern))
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
}
