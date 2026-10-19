using Accounting.Domain.OperationTemplates;

namespace Accounting.Application.OperationTemplates.History;

/// <param name="VahedCode">
/// واحد کاربر ⇒ حالت اجرا: فقط سطح‌هایی که واحد تفصیلی قابل‌انتخاب دارد، گروه پارامتر خالی (هر گروه مجاز همان سطح)،
/// عنوان «از سوابق: …». <c>null</c> ⇒ حالت طراحی (ستاد): گروه انتخاب‌شده روی پارامتر، برای پیش‌پر کردن طراح الگو.
/// </param>
public sealed record HistoryBuildContext(string? VahedCode, string? Description = null, string? UserSentence = null);

public sealed record HistoryBuildResult(
    OperationTemplate? Template, IReadOnlyList<string> Problems, IReadOnlyList<string> Notes, IReadOnlyList<KeywordCount> Keywords);

/// <summary>
/// از یک الگوی سابقه، <b>الگوی موقت (ad-hoc)</b> در حافظه می‌سازد: پارامتر مبلغ، یک پارامتر تفصیلی برای هر سطح
/// تفصیلی هر ردیف، تاریخ و توضیح اختیاری؛ ردیف‌ها با درصد سهم و یک ردیف تراز‌کننده. همیشه از روی <b>تعریف فعلی کدینگ</b>
/// ساخته می‌شود (معین غیرفعال یا سطح بی‌طرف‌حساب ⇒ مشکل)، نه ورودی کلاینت. موتور همان <c>VoucherGenerationEngine</c> است.
/// </summary>
public sealed class HistoryTemplateFactory
{
    public const string AmountKey = "amount";
    public const string DateKey = "date1";
    public const string NoteKey = "note1";

    private readonly ISubsidiaryAccountReader _accounts;
    private readonly IHistoryPatternStore _store;

    public HistoryTemplateFactory(ISubsidiaryAccountReader accounts, IHistoryPatternStore store)
    { _accounts = accounts; _store = store; }

    public async Task<HistoryBuildResult> BuildAsync(HistoryPattern pattern, HistoryBuildContext ctx, CancellationToken ct)
    {
        var unitMode = !string.IsNullOrWhiteSpace(ctx.VahedCode);
        var problems = new List<string>();
        var notes = new List<string>();
        var keywords = HistoryJson.Keywords(pattern.KeywordsJson);

        var stored = HistoryJson.Lines(pattern.LinesJson)
            .OrderBy(l => l.Side).ThenByDescending(l => l.Share).ToList();
        if (stored.Count < 2 || !stored.Any(l => l.Side == 1) || !stored.Any(l => l.Side == 2))
        {
            problems.Add("الگوی سابقه ردیف بدهکار و بستانکار کامل ندارد؛ مدیر ستاد «بازسازی» را بزند.");
            return new(null, problems, notes, keywords);
        }

        var accounts = await _accounts.GetByIdsAsync(stored.Select(l => l.MoinId).Distinct().ToList(), ct);
        foreach (var l in stored)
        {
            if (!accounts.TryGetValue(l.MoinId, out var a)) problems.Add($"معین {l.MoinId} دیگر وجود ندارد.");
            else if (!a.IsActive) problems.Add($"معین «{a.Title}» غیرفعال شده است.");
        }
        if (problems.Count > 0) return new(null, problems, notes, keywords);

        var allGroups = stored.SelectMany(l => accounts[l.MoinId].DetailLevels).SelectMany(r => r.DetailGroupIds).Distinct().ToList();
        var titles = allGroups.Count == 0
            ? new Dictionary<Guid, string>()
            : new Dictionary<Guid, string>(await _store.GroupTitlesAsync(allGroups, ct));
        IReadOnlySet<Guid>? fillable = unitMode && allGroups.Count > 0
            ? await _store.FillableGroupsAsync(allGroups, ctx.VahedCode!, ct)
            : null;

        // ── ردیف‌ها و پارامترهای تفصیلی ──
        var tplId = pattern.Id;
        var percents = stored.Select(l => l.Side).Distinct().ToDictionary(
            side => side,
            side => Percents(stored.Where(l => l.Side == side).Select(l => l.Share).ToList()));
        var sideIndex = new Dictionary<int, int>();

        var detailCount = stored.Sum(l => accounts[l.MoinId].DetailLevels.Count);
        var parameters = new List<TemplateParameter>
        {
            new() { Id = Guid.NewGuid(), OperationTemplateId = tplId, Key = AmountKey, Title = "مبلغ کل سند (ریال)", Type = ParameterType.Amount,
                IsRequired = true, AskPrompt = "مبلغ چقدر بود؟ (به ریال)", SortOrder = 1 },
        };
        var lines = new List<TemplateLine>();
        var detailNo = 0;
        var order = 0;

        foreach (var l in stored)
        {
            var acc = accounts[l.MoinId];
            var idx = sideIndex[l.Side] = sideIndex.GetValueOrDefault(l.Side, -1) + 1;
            var isBalancing = l.Side == 2 && idx == 0;   // بزرگ‌ترین ردیف بستانکار تراز می‌کند
            var line = new TemplateLine
            {
                Id = Guid.NewGuid(), OperationTemplateId = tplId, Side = (LineSide)l.Side, SubsidiaryAccountId = l.MoinId,
                AmountParameterKey = isBalancing ? "" : AmountKey, Percent = isBalancing ? 100m : percents[l.Side][idx],
                IsBalancingLine = isBalancing, SortOrder = ++order, AmountSource = LineAmountSource.Parameter,
            };

            foreach (var rule in acc.DetailLevels.OrderBy(r => r.Level))
            {
                var groups = rule.DetailGroupIds.OrderBy(g => g).ToList();
                var usable = unitMode ? groups.Where(g => fillable?.Contains(g) == true).ToList() : groups;
                if (usable.Count == 0)
                {
                    problems.Add(unitMode
                        ? $"برای سطح {rule.Level} معین «{acc.Title}» تفصیلی قابل‌انتخاب برای واحد شما تعریف نشده است."
                        : $"سطح {rule.Level} معین «{acc.Title}» هیچ گروه تفصیلی ندارد.");
                    continue;
                }

                var chosen = usable[0];
                var groupTitle = titles.GetValueOrDefault(chosen);
                if (!unitMode && usable.Count > 1)
                    notes.Add($"سطح {rule.Level} معین «{acc.Title}» چند گروه تفصیلی دارد؛ گروه «{groupTitle}» انتخاب شد — در صورت نیاز تغییر دهید.");

                var key = $"detail{++detailNo}";
                var who = string.IsNullOrWhiteSpace(groupTitle) || usable.Count > 1 && unitMode ? "طرف حساب" : groupTitle;
                parameters.Add(new TemplateParameter
                {
                    Id = Guid.NewGuid(), OperationTemplateId = tplId, Key = key,
                    Title = detailCount > 1 ? $"{who} ({acc.Title})" : who,
                    Type = ParameterType.Detail,
                    // حالت اجرا: گروه خالی؛ هر تفصیلیِ مجازِ همان سطح معین پذیرفته می‌شود (بررسی سطح در موتور).
                    DetailGroupId = unitMode ? null : chosen,
                    IsRequired = true,
                    AskPrompt = detailCount > 1 ? $"{who} برای «{acc.Title}» کیست؟" : $"طرف حساب ({who}) کیست؟",
                    SortOrder = 1 + detailNo,
                });
                line.Details.Add(new TemplateLineDetail { Id = Guid.NewGuid(), TemplateLineId = line.Id, Level = rule.Level, ParameterKey = key });
            }
            lines.Add(line);
        }
        if (problems.Count > 0) return new(null, problems, notes, keywords);

        parameters.Add(new TemplateParameter
        {
            Id = Guid.NewGuid(), OperationTemplateId = tplId, Key = DateKey, Title = "تاریخ سند", Type = ParameterType.Date,
            IsRequired = false, AskPrompt = "سند به چه تاریخی ثبت شود؟", SortOrder = parameters.Count + 1,
        });
        parameters.Add(new TemplateParameter
        {
            Id = Guid.NewGuid(), OperationTemplateId = tplId, Key = NoteKey, Title = "توضیح", Type = ParameterType.Text,
            IsRequired = false, AskPrompt = "توضیح اضافه‌ای دارید؟", SortOrder = parameters.Count + 1,
        });

        // ── عنوان و شرح‌ها ──
        var topWords = string.Join(' ', keywords.OrderByDescending(k => k.Count).Take(3).Select(k => k.Word));
        string title;
        if (unitMode)
        {
            var typed = Sanitize(string.IsNullOrWhiteSpace(ctx.Description) ? ctx.UserSentence : ctx.Description, 180);
            title = typed.Length > 0 ? typed : topWords.Length > 0 ? topWords : "سند مشابه سوابق";
        }
        else
        {
            title = topWords.Length > 0 ? topWords : "الگوی سوابق";
        }
        var firstDetail = parameters.FirstOrDefault(p => p.Type == ParameterType.Detail)?.Key;
        var plain = $"ثبت «{title}» به مبلغ {{{AmountKey}}}" + (firstDetail is null ? "" : $" برای {{{firstDetail}}}");

        var years = pattern.Years is { Length: > 0 } y ? $" ({y})" : "";
        var template = new OperationTemplate
        {
            Id = tplId,
            Code = unitMode ? HistoryDefaults.TemplateCode(pattern.Id) : "HISTORY_" + pattern.Id.ToString("N")[..8].ToUpperInvariant(),
            Title = unitMode ? HistoryDefaults.Title(title) : title,
            Description = $"ساخته‌شده از {pattern.VoucherCount ?? 0} سند مشابه در {pattern.UnitCount ?? 0} واحد{years}.",
            VoucherDescriptionPattern = $"{title} - {{{NoteKey}}}",
            PlainPattern = plain,
            Keywords = unitMode ? null : KeywordLines(keywords),
            AllowedVahedTypes = unitMode ? null : pattern.VahedTypes,
            IsActive = true,
            CreatedBy = "",
            CreatedAtUtc = DateTime.UtcNow,
            Parameters = parameters,
            Lines = lines,
        };
        return new(template, problems, notes, keywords);
    }

    /// <summary>پرسش‌ها با همان DTO پارامتر الگوها (به‌همراه معین/سطح انتخابگر تفصیلی).</summary>
    public static List<TemplateParameterDto> Questions(OperationTemplate t) =>
        t.Parameters.OrderBy(p => p.SortOrder).Select(p =>
        {
            Guid? accountId = null;
            int? level = null;
            if (p.Type == ParameterType.Detail)
                foreach (var l in t.Lines.OrderBy(l => l.SortOrder))
                    if (l.Details.FirstOrDefault(d => string.Equals(d.ParameterKey, p.Key, StringComparison.OrdinalIgnoreCase)) is { } d)
                    { accountId = l.SubsidiaryAccountId; level = d.Level; break; }
            return new TemplateParameterDto(p.Key, p.Title, p.Type, p.DetailGroupId, p.IsRequired, p.AskPrompt, p.SortOrder,
                accountId, level);
        }).ToList();

    public static List<TemplateLineDto> LineDtos(OperationTemplate t) =>
        t.Lines.OrderBy(l => l.SortOrder).Select(l => new TemplateLineDto(l.Side, l.SubsidiaryAccountId,
            l.IsBalancingLine ? null : l.AmountParameterKey, l.Percent, l.IsBalancingLine, l.DescriptionPattern, l.SortOrder,
            l.Details.OrderBy(d => d.Level).Select(d => new TemplateLineDetailDto(d.Level, d.ParameterKey, d.FixedDetailId)).ToList())).ToList();

    private static string KeywordLines(IReadOnlyList<KeywordCount> keywords)
    {
        var words = new List<string>();
        var len = 0;
        foreach (var k in keywords.OrderByDescending(k => k.Count).Take(10))
        {
            if (len + k.Word.Length + 1 > 900) break;
            words.Add(k.Word);
            len += k.Word.Length + 1;
        }
        return string.Join('\n', words);
    }

    /// <summary>آکولاد از متن آزاد برداشته می‌شود تا با جای‌نگه‌دار الگو قاطی نشود.</summary>
    public static string Sanitize(string? text, int max)
    {
        var s = (text ?? "").Replace("{", "").Replace("}", "").Trim();
        return s.Length <= max ? s : s[..max].TrimEnd();
    }

    /// <summary>
    /// سهم‌های میانه (که جمعشان ۱۰۰ نیست) را به درصدهای دو رقمی با جمع دقیق ۱۰۰ می‌رساند؛ بزرگ‌ترین ردیف باقی‌مانده را می‌گیرد.
    /// </summary>
    public static List<decimal> Percents(IReadOnlyList<decimal> shares)
    {
        var n = shares.Count;
        if (n == 1) return [100m];
        var sum = shares.Sum();
        var raw = sum <= 0 ? shares.Select(_ => 100m / n).ToList() : shares.Select(s => s / sum * 100m).ToList();
        var r = raw.Select(x => Math.Max(0.01m, Math.Round(x, 2, MidpointRounding.AwayFromZero))).ToList();
        var big = raw.IndexOf(raw.Max());
        r[big] = 0;
        r[big] = Math.Round(100m - r.Sum(), 2, MidpointRounding.AwayFromZero);
        if (r[big] > 0) return r;

        var equal = Math.Round(100m / n, 2, MidpointRounding.AwayFromZero);
        r = Enumerable.Repeat(equal, n).ToList();
        r[0] = Math.Round(100m - r.Skip(1).Sum(), 2, MidpointRounding.AwayFromZero);
        return r;
    }
}
