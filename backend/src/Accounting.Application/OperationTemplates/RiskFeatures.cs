using System.Globalization;
using System.Text.Json;
using FluentValidation;
using Accounting.Domain.OperationTemplates;

namespace Accounting.Application.OperationTemplates;

// ═══════════════════════ فاز ۶۲ — سیاست الگو، سؤال انتخابی، ریسک و هشدار (DDL 084) ═══════════════════════

/// <summary>یک گزینهٔ سؤال انتخابی.</summary>
public sealed record ChoiceOptionDto(string Value, string Label);

public static class ChoiceOptions
{
    /// <summary>جواب «نمی‌دانم» برای همهٔ سؤال‌های انتخابی همیشه مجاز است و گزینهٔ تعریف‌شده نیست.</summary>
    public const string Unknown = "unknown";
    public const string UnknownLabel = "نمی‌دانم";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<ChoiceOptionDto> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<ChoiceOptionDto>>(json, Json)?
                .Where(o => !string.IsNullOrEmpty(o?.Value)).ToList() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string? Serialize(IEnumerable<ChoiceOptionDto>? options)
    {
        var list = (options ?? []).Where(o => o is not null)
            .Select(o => new ChoiceOptionDto(o.Value?.Trim() ?? "", o.Label?.Trim() ?? "")).ToList();
        return list.Count == 0 ? null : JsonSerializer.Serialize(list, Json);
    }
}

public static class TemplateLogic
{
    /// <summary>G8: ردیف بدون شرط همیشه؛ ردیف شرطی فقط وقتی جواب سؤال انتخابی برابر مقدار شرط باشد.</summary>
    public static bool LineApplies(TemplateLine line, Func<string, string?> answerOf) =>
        string.IsNullOrEmpty(line.ConditionParamKey)
        || string.Equals(answerOf(line.ConditionParamKey), line.ConditionValue, StringComparison.Ordinal);
}

/// <summary>آستانه‌های ریسک؛ از پیکربندی <c>Assistant:Risk</c> (قابل تنظیم، بدون ساخت مجدد).</summary>
public sealed class AssistantRiskOptions
{
    public const string Section = "Assistant:Risk";

    /// <summary>مبلغ بیش از این مضرب میانهٔ اجراهای قبلی همان الگو در واحد ⇒ ناهنجاری (ریسک متوسط).</summary>
    public double AnomalyFactor { get; set; } = 2;

    /// <summary>بازهٔ مرور اجراهای قبلی (ماه).</summary>
    public int LookbackMonths { get; set; } = 12;

    /// <summary>حداقل تعداد اجرای قبلی برای محاسبهٔ میانه؛ کمتر ⇒ ناهنجاری سنجیده نمی‌شود.</summary>
    public int MinSamples { get; set; } = 3;
}

public sealed record OperationRiskDto(string Level, IReadOnlyList<string> Reasons);

public sealed record ControlDto(string Key, string Label, bool Passed);

public sealed record OperationBasisDto(string? TemplateTitle, int UsageCountInUnit);

/// <summary>سابقهٔ یک اجرای قبلی همین الگو در واحد (فقط سندهای حذف‌نشده).</summary>
public sealed record RiskHistoryRow(string InputJson, long? TotalAmount, DateTime CreatedAtUtc);

public sealed record RiskRequest(
    OperationTemplate? Template,
    string VahedCode,
    VoucherDraft Draft,
    IReadOnlyDictionary<string, string?> Values,
    bool IsCompose,
    bool HasAttachment,
    IReadOnlyDictionary<string, string?>? AnswerSources,
    bool AiConfirmed);

public sealed record RiskAssessment(
    RiskLevel Level,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<ControlDto> Controls,
    OperationBasisDto Basis,
    long TotalAmount)
{
    public OperationRiskDto ToDto() => new(RiskText.Level(Level), Reasons);
}

public static class RiskText
{
    public static string Level(RiskLevel l) => l switch { RiskLevel.High => "high", RiskLevel.Medium => "medium", _ => "low" };
    public static string Level(RiskLevel? l) => Level(l ?? RiskLevel.Medium);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>فهرست رشته‌ها ⇒ JSON در حد ستون (۲۰۰۰ نویسه)؛ مواردِ آخرِ بیش از حد حذف می‌شوند.</summary>
    public static string? SerializeList(IReadOnlyList<string> items, int max = 2000)
    {
        if (items.Count == 0) return null;
        var list = items.ToList();
        while (list.Count > 0)
        {
            var s = JsonSerializer.Serialize(list, Json);
            if (s.Length <= max) return s;
            list.RemoveAt(list.Count - 1);
        }
        return null;
    }

    public static IReadOnlyList<string> ParseList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<string>>(json, Json) ?? []; }
        catch (JsonException) { return []; }
    }

    public static IReadOnlyDictionary<string, string> ParseSources(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, string>();
        try { return JsonSerializer.Deserialize<Dictionary<string, string>>(json, Json) ?? new(); }
        catch (JsonException) { return new Dictionary<string, string>(); }
    }

    public static string? SerializeSources(IReadOnlyDictionary<string, string?>? sources)
    {
        if (sources is null) return null;
        var clean = sources.Where(kv => !string.IsNullOrWhiteSpace(kv.Value)).ToDictionary(kv => kv.Key, kv => kv.Value!);
        return clean.Count == 0 ? null : JsonSerializer.Serialize(clean, Json);
    }
}

/// <summary>
/// ارزیاب قطعی ریسک و هشدار — یک ارزیاب برای Preview و Execute (ورودی یکسان ⇒ نتیجهٔ یکسان). هوش مصنوعی ندارد.
/// ریسک = بیشینهٔ عامل‌ها: کف RISK_BASE الگو؛ مبلغ &gt; MAX_AMOUNT ⇒ بالا؛ مدرک الزامی بدون پیوست، ناهنجاری مبلغ،
/// تکرار محتوایی در همان ماه، «نمی‌دانم»، ویرایش کامل (Compose) و جواب پرشدهٔ هوش مصنوعی بدون تأیید صریح ⇒ متوسط.
/// </summary>
public sealed class OperationRiskEvaluator
{
    /// <summary>منبع‌های مجاز جواب (answerSources).</summary>
    public static readonly string[] SourceKinds = ["sentence", "ai", "recent", "quick", "user"];

    public const string AttachmentReason = "مدرک الزامی است و پیوست نشده است.";
    public const string AttachmentWarning = "برای این عملیات مدرک (فاکتور یا رسید) لازم است و هنوز پیوست نشده؛ لطفاً آن را پیوست کنید.";

    private readonly IOperationTemplateRepository _repo;
    private readonly AssistantRiskOptions _options;

    public OperationRiskEvaluator(IOperationTemplateRepository repo, AssistantRiskOptions? options = null)
    {
        _repo = repo;
        _options = options ?? new AssistantRiskOptions();
    }

    public async Task<RiskAssessment> EvaluateAsync(RiskRequest r, CancellationToken ct)
    {
        var t = r.Template;
        var amount = r.Draft.TotalDebit;
        var level = RiskLevel.Low;
        var reasons = new List<string>();
        var warnings = new List<string>();

        void Raise(RiskLevel l, string reason, string? warning = null)
        {
            if (l > level) level = l;
            if (!reasons.Contains(reason)) reasons.Add(reason);
            if (warning is not null && !warnings.Contains(warning)) warnings.Add(warning);
        }

        if (t is not null)
        {
            // ۱. کف ریسک الگو
            if (t.RiskBase is { } baseRisk && baseRisk > RiskLevel.Low)
                Raise(baseRisk, "ریسک ذاتی این نوع عملیات بالاتر از معمول است.");

            // ۲. سقف مبلغ
            if (t.MaxAmount is { } max && amount > max)
                Raise(RiskLevel.High, $"مبلغ از سقف {max.ToString("N0", CultureInfo.InvariantCulture)} ریال این عملیات بیشتر است.",
                    "مبلغ از سقف تعریف‌شده برای این عملیات بیشتر است؛ علاوه بر حسابدار، تأیید مدیر مالی هم لازم است.");

            // ۳. مدرک
            if (t.RequireAttachment && !r.HasAttachment)
                Raise(RiskLevel.Medium, AttachmentReason, AttachmentWarning);

            // ۴. «نمی‌دانم»
            foreach (var p in t.Parameters.Where(p => p.Type == ParameterType.Choice))
                if (r.Values.TryGetValue(p.Key, out var v) && string.Equals(v?.Trim(), ChoiceOptions.Unknown, StringComparison.OrdinalIgnoreCase))
                    Raise(RiskLevel.Medium, $"پاسخ «{p.Title}» نامعلوم است.",
                        $"«{p.Title}» را نمی‌دانید؛ سند با یادداشت برای حسابدار می‌رود.");
        }

        // ۵. ویرایش کامل
        if (r.IsCompose)
            Raise(RiskLevel.Medium, "سند با «ویرایش کامل» ساخته یا تغییر داده شده است.");

        // ۶. جواب هوش مصنوعی بدون تأیید صریح کاربر
        if (!r.AiConfirmed && r.AnswerSources is not null
            && r.AnswerSources.Any(kv => string.Equals(kv.Value, "ai", StringComparison.OrdinalIgnoreCase)))
            Raise(RiskLevel.Medium, "بعضی جواب‌ها را هوش مصنوعی پر کرده و کاربر صریحاً تأیید نکرده است.",
                "بعضی جواب‌ها را هوش مصنوعی پر کرده است؛ پیش از ثبت یک بار مرورشان کنید.");

        // ۷/۸. ناهنجاری و تکرار محتوایی از سابقهٔ همین الگو در همین واحد
        var usage = 0;
        if (t is not null && !string.IsNullOrWhiteSpace(r.VahedCode))
        {
            var since = DateTime.UtcNow.AddMonths(-Math.Clamp(_options.LookbackMonths, 1, 120));
            var history = await _repo.ListRiskHistoryAsync(r.VahedCode, t.Id, since, 1000, ct);
            usage = (await _repo.UsageAsync(r.VahedCode, ct)).FirstOrDefault(u => u.TemplateId == t.Id)?.Count ?? 0;

            // ناهنجاری: مبلغ بیش از «ضریب × میانه»
            var amounts = history.Select(h => HistoryAmount(h, t)).Where(a => a > 0).OrderBy(a => a).ToList();
            if (amounts.Count >= Math.Max(1, _options.MinSamples) && amount > 0)
            {
                var median = Median(amounts);
                if (median > 0 && amount > _options.AnomalyFactor * median)
                {
                    var ratio = (double)amount / median;
                    Raise(RiskLevel.Medium, "مبلغ بسیار بیشتر از معمول این عملیات در واحد است.",
                        $"این مبلغ حدود {ratio.ToString("0.#", CultureInfo.InvariantCulture)} برابر میانهٔ «{t.Title}» قبلی این واحد " +
                        $"({median.ToString("N0", CultureInfo.InvariantCulture)} ریال) است؛ لطفاً مبلغ را دوباره بررسی کنید.");
                }
            }

            // تکرار محتوایی: همان تفصیلی‌ها و همان مبلغ در همین ماه شمسی
            if (!r.IsCompose && amount > 0)
            {
                var monthStart = JalaliMonthStartUtc();
                var mine = DetailAnswers(r.Values, t);
                var duplicate = history.Any(h => h.CreatedAtUtc >= monthStart
                    && HistoryAmount(h, t) == amount
                    && DetailAnswers(ListMyRecentOperationsHandler.ParseValues(h.InputJson), t).SequenceEqual(mine));
                if (duplicate)
                    Raise(RiskLevel.Medium, "سندی با همین طرف حساب و همین مبلغ در همین ماه قبلاً ثبت شده است.",
                        "در این ماه یک سند با همین طرف حساب و همین مبلغ قبلاً ثبت شده است؛ مطمئن باشید تکراری نیست.");
            }
        }

        var controls = new List<ControlDto>
        {
            new("balanced", "دو طرف سند برابر است", r.Draft.TotalDebit == r.Draft.TotalCredit && r.Draft.TotalDebit > 0),
            new("periodOpen", $"دورهٔ سال مالی {FiscalYearGuard.JalaliYear(r.Draft.VoucherDate)} برای این واحد باز است", true),
        };
        if (r.Draft.Lines.Any(l => l.Details.Count > 0))
            controls.Add(new("counterpartyAllowed", "طرف حساب برای این واحد مجاز است", true));

        return new RiskAssessment(level, reasons, warnings, controls, new OperationBasisDto(t?.Title, usage), amount);
    }

    /// <summary>مبلغ یک اجرای قبلی: جمع بدهکار ذخیره‌شده، وگرنه (پیش از DDL 084) اولین پارامتر مبلغ ورودی.</summary>
    private static long HistoryAmount(RiskHistoryRow h, OperationTemplate t)
    {
        if (h.TotalAmount is { } total && total > 0) return total;
        var first = t.Parameters.Where(p => p.Type == ParameterType.Amount).OrderBy(p => p.SortOrder).FirstOrDefault();
        if (first is null) return 0;
        var values = ListMyRecentOperationsHandler.ParseValues(h.InputJson);
        return values.TryGetValue(first.Key, out var raw) && long.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var a) ? a : 0;
    }

    private static List<string> DetailAnswers(IReadOnlyDictionary<string, string?> values, OperationTemplate t) =>
        t.Parameters.Where(p => p.Type == ParameterType.Detail).OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => $"{p.Key}={(values.TryGetValue(p.Key, out var v) ? v?.Trim().ToLowerInvariant() : null)}").ToList();

    private static long Median(List<long> sorted)
    {
        var n = sorted.Count;
        return n % 2 == 1 ? sorted[n / 2] : (long)Math.Round((sorted[n / 2 - 1] + sorted[n / 2]) / 2.0, MidpointRounding.AwayFromZero);
    }

    private static DateTime JalaliMonthStartUtc()
    {
        var pc = new PersianCalendar();
        var now = DateTime.Now;
        var start = pc.ToDateTime(pc.GetYear(now), pc.GetMonth(now), 1, 0, 0, 0, 0);
        return DateTime.SpecifyKind(start, DateTimeKind.Local).ToUniversalTime();
    }
}

/// <summary>
/// هم‌گام‌سازی ریسک ذخیره‌شدهٔ اجرا با پیوست واقعی (فاز ۶۵): فایل پس از ثبت بارگذاری می‌شود، پس ریسک لحظهٔ ثبت
/// ممکن است «مدرک پیوست نشده» داشته باشد. چون هر دلیل ریسک دست‌کم «متوسط» است و نبود مدرک فقط «متوسط» می‌سازد،
/// بدون اجرای دوبارهٔ ارزیاب دقیق محاسبه می‌شود: با حذف این دلیل، سطح فقط وقتی «متوسط» بود و دلیل دیگری نماند ⇒ «پایین».
/// </summary>
public static class AttachmentRiskSync
{
    /// <returns>true اگر ریسک/هشدار اجرا تغییر کرد.</returns>
    public static bool Apply(OperationExecution e, bool attachmentRequired, bool hasAttachment)
    {
        if (e.RiskLevel is null) return false; // اجرای قدیمی بدون ریسک: «نیازمند توجه» می‌ماند
        var reasons = RiskText.ParseList(e.RiskReasonsJson).ToList();
        var warnings = RiskText.ParseList(e.WarningsJson).ToList();
        var flagged = reasons.Contains(OperationRiskEvaluator.AttachmentReason);

        if (hasAttachment && flagged)
        {
            reasons.Remove(OperationRiskEvaluator.AttachmentReason);
            warnings.Remove(OperationRiskEvaluator.AttachmentWarning);
            if (e.RiskLevel == RiskLevel.Medium && reasons.Count == 0) e.RiskLevel = RiskLevel.Low;
        }
        else if (!hasAttachment && !flagged && attachmentRequired)
        {
            reasons.Add(OperationRiskEvaluator.AttachmentReason);
            if (!warnings.Contains(OperationRiskEvaluator.AttachmentWarning)) warnings.Add(OperationRiskEvaluator.AttachmentWarning);
            if (e.RiskLevel < RiskLevel.Medium) e.RiskLevel = RiskLevel.Medium;
        }
        else return false;

        e.RiskReasonsJson = RiskText.SerializeList(reasons);
        e.WarningsJson = RiskText.SerializeList(warnings);
        return true;
    }
}

/// <summary>ثبت منشأ و ریسک روی ردیف اجرا (مشترک بین الگو و سند کامل).</summary>
public static class ExecutionOrigin
{
    public static void Apply(OperationExecution e, string? userSentence, IReadOnlyDictionary<string, string?>? sources,
        bool aiConfirmed, RiskAssessment? assessment, long totalAmount)
    {
        var sentence = userSentence?.Trim();
        e.UserSentence = string.IsNullOrEmpty(sentence) ? null : sentence;
        e.AnswerSourcesJson = RiskText.SerializeSources(sources);
        e.AiConfirmed = sources is { Count: > 0 } ? aiConfirmed : null;
        e.TotalAmount = totalAmount;
        if (assessment is null) return;
        e.RiskLevel = assessment.Level;
        e.RiskReasonsJson = RiskText.SerializeList(assessment.Reasons);
        e.WarningsJson = RiskText.SerializeList(assessment.Warnings);
    }

    /// <summary>خطای ورودی منشأ (جمله بلندتر از ۱۰۰۰، منبع ناشناخته، کلید طولانی)، یا null.</summary>
    public static string? Validate(string? userSentence, IReadOnlyDictionary<string, string?>? sources)
    {
        if ((userSentence?.Length ?? 0) > 1000) return "جملهٔ کاربر حداکثر ۱۰۰۰ نویسه است.";
        if (sources is null) return null;
        if (sources.Count > 30) return "منبع جواب برای بیش از ۳۰ سؤال پذیرفته نمی‌شود.";
        foreach (var (k, v) in sources)
        {
            if (string.IsNullOrWhiteSpace(k) || k.Length > 40) return "کلید منبع جواب نامعتبر است.";
            if (string.IsNullOrWhiteSpace(v)) continue;
            if (!OperationRiskEvaluator.SourceKinds.Contains(v.Trim().ToLowerInvariant()))
                return $"منبع جواب «{v}» نامعتبر است (sentence | ai | recent | quick | user).";
        }
        return null;
    }
}

public sealed class PreviewOperationValidator : AbstractValidator<PreviewOperationQuery>
{
    public PreviewOperationValidator() =>
        RuleFor(x => x.AnswerSources).Must(s => ExecutionOrigin.Validate(null, s) is null)
            .WithMessage(x => ExecutionOrigin.Validate(null, x.AnswerSources));
}

public sealed class ExecuteOperationValidator : AbstractValidator<ExecuteOperationCommand>
{
    public ExecuteOperationValidator() =>
        RuleFor(x => x).Must(x => ExecutionOrigin.Validate(x.UserSentence, x.AnswerSources) is null)
            .WithMessage(x => ExecutionOrigin.Validate(x.UserSentence, x.AnswerSources));
}

public sealed class ExecuteComposedVoucherValidator : AbstractValidator<ExecuteComposedVoucherCommand>
{
    public ExecuteComposedVoucherValidator() =>
        RuleFor(x => x).Must(x => ExecutionOrigin.Validate(x.UserSentence, x.AnswerSources) is null)
            .WithMessage(x => ExecutionOrigin.Validate(x.UserSentence, x.AnswerSources));
}
