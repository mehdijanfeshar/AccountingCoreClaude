using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.OperationTemplates;
using MediatR;

namespace Accounting.Application.OperationTemplates.Agent;

// ═══════════════════════ Agent-UX فاز ۲ — فهم جملهٔ کاربر با هوش مصنوعی ═══════════════════════
//
// هوش مصنوعی فقط «می‌فهمد»: کدام الگو، و جواب سؤال‌های آن از روی جمله. هرگز سند نمی‌سازد و ثبت نمی‌کند —
// همان موتور سرور سند را می‌سازد و کاربر «ثبت» را می‌زند. خروجی مدل اینجا دوباره اعتبارسنجی می‌شود
// (الگو باید در فهرست باشد، کلید سؤال باید مال همان الگو باشد، مبلغ/تاریخ معتبر).
//
// سیاست داده (تصمیم صاحب پروژه، ۲۰۲۶-۱۰-۰۶): فقط جملهٔ کاربر + فهرست الگوها (عنوان، توضیح، کلمات
// کلیدی، سؤال‌ها) به مدل فرستاده می‌شود. نام طرف حساب را مدل فقط به‌صورت «متن نوشته‌شده» برمی‌گرداند
// و جستجوی تفصیلی (با قاعدهٔ B واحد) را خودِ سیستم انجام می‌دهد — هیچ دادهٔ تفصیلی/مانده‌ای به مدل نمی‌رود.

/// <summary>درخواست به مدل: دستور سیستم، پیام کاربر و شِمای JSON خروجی.</summary>
public sealed record IntentPrompt(string System, string User, string JsonSchema);

/// <summary>سرویس هوش مصنوعی (Claude، Ollama، یا خاموش) — پیاده‌سازی در Infrastructure.</summary>
public interface IIntentInterpreter
{
    bool IsEnabled { get; }

    /// <summary>نام سرویس برای نمایش («Claude» / «Ollama»).</summary>
    string ProviderName { get; }

    /// <summary>نام مدل برای ردپای هوش مصنوعی (G4).</summary>
    string? ModelName => null;

    /// <summary>
    /// true = جمله پیش از ارسال با <see cref="PiiMasker"/> پوشانده و پاسخ در سرور برگردانده می‌شود (G5).
    /// پیش‌فرض true (امن)؛ Claude همیشه true است، Ollama داخلی با <c>MaskPii</c> تنظیم می‌شود.
    /// </summary>
    bool RequiresPiiMasking => true;

    /// <summary>
    /// false = فقط انتخاب الگو از مدل پذیرفته می‌شود و جواب‌ها (مبلغ، تاریخ، طرف حساب) را روش قاعده‌ای فرانت برمی‌دارد —
    /// برای مدل کوچک داخلی که الگو را درست می‌گیرد ولی عدد فارسی را اشتباه می‌فهمد (فاز ۶۵). سنجش همیشه خام است.
    /// </summary>
    bool TrustAnswers => true;

    /// <summary>
    /// false = «فهم جمله» در صفحهٔ ثبت سند خاموش است (روش قاعده‌ای قبلی) ولی همین مدل برای گفتگو و سنجش در دسترس است.
    /// </summary>
    bool InterpretEnabled => true;

    /// <summary>متن JSON یک شیء مطابق <see cref="IntentPrompt.JsonSchema"/>.</summary>
    Task<string> CompleteJsonAsync(IntentPrompt prompt, CancellationToken ct);
}

public sealed record InterpretSentenceQuery(string Sentence) : IRequest<InterpretResultDto>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;

    /// <summary>سال مالی جاری کاربر — از query (<c>?year=</c>).</summary>
    [JsonIgnore]
    public string? Year { get; set; }
}

/// <param name="Value">مبلغ: ریال (رقم لاتین)؛ تاریخ: شمسی <c>yyyyMMdd</c>؛ متن: متن؛ تفصیلی: نام همان‌طور که در جمله آمده (سیستم جستجو می‌کند).</param>
public sealed record InterpretedAnswerDto(string Key, string Value, ParameterType Type);

/// <param name="Enabled">هوش مصنوعی در تنظیمات سرور روشن است؛ false ⇒ فرانت با روش قاعده‌ای ادامه می‌دهد.</param>
/// <param name="Succeeded">پاسخ مدل گرفته و معتبر بود (حتی اگر الگویی پیدا نکرد).</param>
public sealed record InterpretResultDto(
    bool Enabled,
    string? Provider,
    bool Succeeded,
    string? Message,
    Guid? TemplateId,
    double? Confidence,
    string? VoucherDate,
    IReadOnlyList<InterpretedAnswerDto> Answers,
    string? Clarification,
    Guid? AiTraceId = null)
{
    public static InterpretResultDto Disabled() => new(false, null, false, null, null, null, null, [], null);
}

public sealed class InterpretSentenceHandler : IRequestHandler<InterpretSentenceQuery, InterpretResultDto>
{
    private readonly IIntentInterpreter _interpreter;
    private readonly IOperationTemplateRepository _repo;
    private readonly AssistantUnitPolicy _policy;
    private readonly IHesabyarAuditRepository? _audit;
    private readonly IUnitOfWork? _unitOfWork;
    private readonly ICurrentUser? _currentUser;

    public InterpretSentenceHandler(IIntentInterpreter interpreter, IOperationTemplateRepository repo, AssistantUnitPolicy policy,
        IHesabyarAuditRepository? audit = null, IUnitOfWork? unitOfWork = null, ICurrentUser? currentUser = null)
    {
        _interpreter = interpreter;
        _repo = repo;
        _policy = policy;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<InterpretResultDto> Handle(InterpretSentenceQuery q, CancellationToken ct)
    {
        if (!_interpreter.IsEnabled || !_interpreter.InterpretEnabled)
            return InterpretResultDto.Disabled();

        var sentence = (q.Sentence ?? "").Trim();
        if (sentence.Length == 0 || sentence.Length > 500)
            return Fail("جمله خالی یا بیش از ۵۰۰ کاراکتر است.");

        var templates = await _policy.FilterAsync(await _repo.ListActiveWithParametersAsync(ct), q.VahedCode, ct);
        if (templates.Count == 0)
            return Fail("هیچ الگوی فعالی تعریف نشده است.");

        var run = await IntentRun.ExecuteAsync(_interpreter, sentence, templates, q.Year, ct);
        var result = run.Result;
        if (!_interpreter.TrustAnswers)
            result = result with { Answers = [], VoucherDate = null };

        if (_audit is null || _unitOfWork is null || _currentUser is null)
            return result;

        var trace = new OperationAiTrace
        {
            Id = Guid.NewGuid(),
            VahedCode = q.VahedCode,
            MaskedSentence = run.MaskedSentence,
            Provider = _interpreter.ProviderName,
            Model = _interpreter.ModelName,
            PromptVersion = IntentPromptBuilder.PromptVersion,
            ResultJson = run.StoredJson is { Length: > 20000 } ? run.StoredJson[..20000] : run.StoredJson,
            Confidence = result.Confidence,
            LatencyMs = run.LatencyMs,
            Succeeded = result.Succeeded,
            Error = run.Error ?? (result.Succeeded ? null : "unparseable"),
            CreatedBy = _currentUser.UserId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        await _audit.AddTraceAsync(trace, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return result with { AiTraceId = trace.Id };

        InterpretResultDto Fail(string message) =>
            new(true, _interpreter.ProviderName, false, message, null, null, null, [], null);
    }
}

/// <summary>
/// یک بار «فهم جمله» — مشترک بین <see cref="InterpretSentenceHandler"/> و سنجش هوش مصنوعی (فاز ۶۵)، تا سنجش دقیقاً
/// همان مسیر کاربر (پوشاندن، پرامپت، اعتبارسنجی خروجی) را بسنجد.
/// </summary>
/// <param name="MaskedSentence">جملهٔ پوشانده (برای ذخیره؛ جملهٔ خام هرگز ذخیره نمی‌شود).</param>
/// <param name="StoredJson">پاسخ خام مدل به شکل پوشانده، یا null در خطا.</param>
/// <param name="Error">«timeout» یا نام نوع استثنا؛ null اگر پاسخ رسید.</param>
public sealed record IntentRun(InterpretResultDto Result, string MaskedSentence, string? StoredJson, string? Error, int LatencyMs)
{
    public static async Task<IntentRun> ExecuteAsync(
        IIntentInterpreter interpreter, string sentence, IReadOnlyList<OperationTemplate> templates, string? year, CancellationToken ct)
    {
        // G5: جمله همیشه برای ردپا پوشانده می‌شود؛ برای ارسال به مدل فقط وقتی سرویس بخواهد (Claude همیشه، Ollama با MaskPii).
        var mask = PiiMasker.Mask(sentence);
        var sendMasked = interpreter.RequiresPiiMasking;
        var prompt = IntentPromptBuilder.Build(sendMasked ? mask.Masked : sentence, templates, year);

        string? json = null;
        string? error = null;
        var clock = Stopwatch.StartNew();
        try
        {
            json = await interpreter.CompleteJsonAsync(prompt, ct);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            error = "timeout";
        }
        catch (Exception ex)
        {
            // جزئیات خطای سرویس بیرونی (کلید، آدرس، …) نه به کاربر می‌رود و نه ذخیره می‌شود؛ فقط نوع خطا.
            error = ex.GetType().Name;
        }
        clock.Stop();

        var result = json is null
            ? new InterpretResultDto(true, interpreter.ProviderName, false, error == "timeout"
                    ? "پاسخ هوش مصنوعی دیر رسید؛ با روش معمول ادامه می‌دهیم."
                    : "سرویس هوش مصنوعی در دسترس نیست؛ با روش معمول ادامه می‌دهیم.",
                null, null, null, [], null)
            : IntentResultParser.Parse(sendMasked ? mask.Restore(json) : json, templates, interpreter.ProviderName);

        // نتیجهٔ خام مدل فقط به شکل پوشانده (نشانه‌ها برنگردانده‌شده) ذخیره می‌شود.
        var storedJson = json is null ? null : sendMasked ? json : PiiMasker.Mask(json).Masked;
        return new(result, mask.Masked, storedJson, error, (int)Math.Min(clock.ElapsedMilliseconds, int.MaxValue));
    }
}

/// <summary>ساخت دستور و فهرست الگوها برای مدل.</summary>
public static class IntentPromptBuilder
{
    /// <summary>نسخهٔ پرامپت/شِما برای ردپای هوش مصنوعی (G4) — با هر تغییر در متن دستور یا شِما بالا ببر.</summary>
    public const string PromptVersion = "intent-v2";

    public const string Schema = """
    {
      "type": "object",
      "properties": {
        "templateCode": { "type": ["string", "null"], "description": "کد الگوی انتخاب‌شده از فهرست، یا null" },
        "confidence": { "type": "number", "description": "اطمینان ۰ تا ۱" },
        "voucherDate": { "type": ["string", "null"], "description": "تاریخ سند شمسی yyyyMMdd اگر در جمله آمده، وگرنه null" },
        "answers": {
          "type": "array",
          "items": {
            "type": "object",
            "properties": {
              "key": { "type": "string" },
              "value": { "type": "string" }
            },
            "required": ["key", "value"]
          }
        },
        "clarification": { "type": ["string", "null"], "description": "یک سؤال کوتاه فارسی اگر معلوم نیست کدام الگو؛ وگرنه null" }
      },
      "required": ["templateCode", "confidence", "voucherDate", "answers", "clarification"]
    }
    """;

    public static IntentPrompt Build(string sentence, IReadOnlyList<OperationTemplate> templates, string? fiscalYear)
    {
        var todayJalali = TodayJalali();

        var system = new StringBuilder();
        system.AppendLine("You are the intent parser of a Persian accounting system (حسابیار).");
        system.AppendLine("Map the user's Persian sentence to AT MOST ONE operation template from the catalog and extract answers to that template's parameters.");
        system.AppendLine("Rules:");
        system.AppendLine("- Choose templateCode only from the catalog. If nothing fits, templateCode = null and confidence = 0.");
        system.AppendLine("- If two templates fit equally, pick none (templateCode = null) and write ONE short Persian clarification question.");
        system.AppendLine("- Only fill answers whose value is actually stated in the sentence. Never invent values. Skip unknown parameters.");
        system.AppendLine("- Amount (type Amount): integer Rials, Latin digits, no separators. «تومان/تومن» = ×10 Rials. «میلیون» = 1,000,000, «میلیارد» = 1,000,000,000, «هزار» = 1,000.");
        system.AppendLine($"- Dates: Jalali yyyyMMdd with Latin digits. Today is {todayJalali}. «دیروز» = today − 1 day. A compact 8-digit number like 14040707 is a date, not an amount.");
        system.AppendLine("- voucherDate = the date the event happened, if stated. Also give it to the template's Date parameter if it has one.");
        system.AppendLine("- Detail (طرف حساب/تفصیلی): return the name exactly as written by the user (e.g. «هجرت» or «شرکت هجرت»); do not translate or guess a code.");
        system.AppendLine("- Text (توضیح): a short, clean Persian description of the event (e.g. «خرید دارو از شرکت هجرت»). No dates, no amounts, no request/question words (میخوام، سند بزنم، ؟). Never repeat a word.");
        if (!string.IsNullOrWhiteSpace(fiscalYear))
            system.AppendLine($"- The user's fiscal year is {fiscalYear}; a date without a year belongs to that year.");
        system.AppendLine("- Placeholders like [[NID_1]], [[IBAN_1]], [[CARD_1]], [[TEL_1]] are masked private data: never interpret them as amounts or dates; if one belongs in an answer, copy it unchanged.");
        system.AppendLine("Return only the JSON object.");

        var catalog = new StringBuilder();
        AppendCatalog(catalog, templates);
        catalog.AppendLine();
        catalog.AppendLine("User sentence:");
        catalog.AppendLine(sentence);

        return new IntentPrompt(system.ToString(), catalog.ToString(), Schema);
    }

    /// <summary>امروز به شمسی yyyyMMdd.</summary>
    public static string TodayJalali()
    {
        var today = DateTime.Now;
        var pc = new PersianCalendar();
        return $"{pc.GetYear(today):0000}{pc.GetMonth(today):00}{pc.GetDayOfMonth(today):00}";
    }

    /// <summary>فهرست الگوها برای مدل (عنوان، توضیح، کلمات کلیدی، سؤال‌ها) — مشترک بین فهم جمله و دستیار گفتگو.</summary>
    public static void AppendCatalog(StringBuilder catalog, IReadOnlyList<OperationTemplate> templates)
    {
        catalog.AppendLine("Catalog of operation templates:");
        foreach (var t in templates)
        {
            catalog.AppendLine($"## code: {t.Code}");
            catalog.AppendLine($"title: {t.Title}");
            if (!string.IsNullOrWhiteSpace(t.Description)) catalog.AppendLine($"description: {t.Description}");
            if (!string.IsNullOrWhiteSpace(t.Keywords))
                catalog.AppendLine($"keywords: {string.Join(" | ", t.Keywords.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))}");
            catalog.AppendLine("parameters:");
            foreach (var p in t.Parameters.OrderBy(p => p.SortOrder))
                catalog.AppendLine($"- key: {p.Key} | type: {p.Type} | title: {p.Title} | question: {p.AskPrompt}"
                    + (p.Type == ParameterType.Choice
                        ? " | options(value=label): " + string.Join("; ", ChoiceOptions.Parse(p.OptionsJson).Select(o => $"{o.Value}={o.Label}")) + "; answer unknown only if the user says they do not know"
                        : ""));
        }
    }
}

/// <summary>پاسخ مدل ⇒ نتیجهٔ معتبر. هر چیزی که با فهرست/نوع نخواند دور ریخته می‌شود.</summary>
public static class IntentResultParser
{
    public static InterpretResultDto Parse(string json, IReadOnlyList<OperationTemplate> templates, string provider)
    {
        JsonElement root;
        try
        {
            using var doc = JsonDocument.Parse(json);
            root = doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return new(true, provider, false, "پاسخ هوش مصنوعی قابل خواندن نبود؛ با روش معمول ادامه می‌دهیم.", null, null, null, [], null);
        }
        if (root.ValueKind != JsonValueKind.Object)
            return new(true, provider, false, "پاسخ هوش مصنوعی قابل خواندن نبود.", null, null, null, [], null);

        var clarification = Str(root, "clarification");
        var confidence = root.TryGetProperty("confidence", out var c) && c.ValueKind == JsonValueKind.Number
            ? Math.Clamp(c.GetDouble(), 0, 1)
            : (double?)null;
        var voucherDate = Str(root, "voucherDate") is { } vd && IsJalali(Digits(vd)) ? Digits(vd) : null;

        var code = Str(root, "templateCode");
        var template = code is null
            ? null
            : templates.FirstOrDefault(t => string.Equals(t.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));
        if (template is null)
            return new(true, provider, true, null, null, confidence, voucherDate, [], Limit(clarification, 300));

        var answers = new List<InterpretedAnswerDto>();
        if (root.TryGetProperty("answers", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
            {
                var key = Str(item, "key");
                var value = Str(item, "value");
                if (key is null || value is null) continue;
                var p = template.Parameters.FirstOrDefault(x => string.Equals(x.Key, key.Trim(), StringComparison.OrdinalIgnoreCase));
                if (p is null || answers.Any(a => a.Key == p.Key)) continue;
                if (Normalize(p.Type, value) is { } normalized)
                    answers.Add(new InterpretedAnswerDto(p.Key, normalized, p.Type));
            }
        }

        return new(true, provider, true, null, template.Id, confidence, voucherDate, answers, Limit(clarification, 300));
    }

    private static string? Normalize(ParameterType type, string raw)
    {
        var v = raw.Trim();
        if (v.Length == 0) return null;
        switch (type)
        {
            case ParameterType.Amount:
                var digits = new string(Digits(v).Where(char.IsAsciiDigit).ToArray());
                return digits.Length is > 0 and <= 15 && long.TryParse(digits, out var n) && n > 0 ? n.ToString(CultureInfo.InvariantCulture) : null;
            case ParameterType.Date:
                var d = Digits(v).Replace("/", "").Replace("-", "");
                return IsJalali(d) ? d : null;
            case ParameterType.Detail:
                return Limit(v, 100);
            default:
                return Limit(v, 500);
        }
    }

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(p.GetString())
            ? p.GetString()
            : null;

    private static string? Limit(string? s, int max) => s is null ? null : s.Length <= max ? s : s[..max];

    private static bool IsJalali(string s)
    {
        if (s.Length != 8 || !s.All(char.IsAsciiDigit)) return false;
        var y = int.Parse(s[..4], CultureInfo.InvariantCulture);
        var m = int.Parse(s[4..6], CultureInfo.InvariantCulture);
        var day = int.Parse(s[6..], CultureInfo.InvariantCulture);
        return y is >= 1300 and <= 1499 && m is >= 1 and <= 12 && day >= 1 && day <= new PersianCalendar().GetDaysInMonth(y, m);
    }

    private static string Digits(string s)
    {
        var chars = s.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] is >= '۰' and <= '۹') chars[i] = (char)('0' + (chars[i] - '۰'));
            else if (chars[i] is >= '٠' and <= '٩') chars[i] = (char)('0' + (chars[i] - '٠'));
        }
        return new string(chars);
    }
}
