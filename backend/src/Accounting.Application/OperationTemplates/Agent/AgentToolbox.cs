using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Queries.AccountCodes.GetTafsiliLevelItems;
using Accounting.Application.Reports.AccountReview;
using Accounting.Application.Reports.AccountReview.GetAccountReview;
using Accounting.Domain.OperationTemplates;
using MediatR;

namespace Accounting.Application.OperationTemplates.Agent;

// ═══════════════════════ ابزارهای دستیار گفتگو (فاز ۶۵) ═══════════════════════
//
// همه فقط‌خواندنی‌اند و از همان Query های سیستم (با همان رفتارهای واحد/نقش MediatR) می‌گذرند:
//   search_counterparty     — جستجوی تفصیلی با نام برای یک سؤال طرف حساب الگو (قاعدهٔ B واحد)
//   get_counterparty_balance — ماندهٔ یک طرف حساب در معین همان سؤال (مرور حساب‌ها)
//   get_account_balance     — ماندهٔ معین با نام یا کد (مرور حساب‌ها)
//   preview_voucher         — پیش‌نمایش سند از الگو (همان PreviewOperationQuery)؛ نتیجه پیشنهاد سند برای کاربر است
// «ثبت» ابزار ندارد — کاربر پیشنهاد را در صفحهٔ حسابیار مرور و ثبت می‌کند.

/// <summary>پیشنهاد سند حاصل آخرین پیش‌نمایش موفق — برای کارت «بررسی و ثبت» فرانت.</summary>
public sealed record AgentProposalDto(
    Guid TemplateId,
    string TemplateCode,
    string TemplateTitle,
    string? VoucherDate,
    IReadOnlyList<AgentProposalAnswerDto> Answers,
    OperationResult Preview,
    Guid? AiTraceId = null);

/// <param name="Value">مقدار آمادهٔ سرور: مبلغ ریال، تاریخ شمسی yyyyMMdd، تفصیلی = شناسه، گزینه = مقدار، متن.</param>
/// <param name="Display">نمایش برای کاربر (نام طرف حساب، مبلغ با جداکننده، …).</param>
public sealed record AgentProposalAnswerDto(string Key, string Title, ParameterType Type, string Value, string Display);

/// <summary>یک قدم کار دستیار برای نمایش («جستجوی طرف حساب «هجرت»»).</summary>
public sealed record AgentStepDto(string Tool, string Label, bool Succeeded);

/// <summary>وضعیت یک پیام کاربر در حین اجرای ابزارها.</summary>
public sealed class AgentToolContext
{
    public required string VahedCode { get; init; }
    public string? Year { get; init; }
    public required IReadOnlyList<OperationTemplate> Templates { get; init; }

    /// <summary>ابزارهای دادهٔ حساب (نام طرف حساب، مانده) برای مدل فعال‌اند.</summary>
    public bool DataTools { get; init; }

    public AgentProposalDto? Proposal { get; set; }
    public List<AgentStepDto> Steps { get; } = [];

    /// <summary>برچسب تفصیلی‌هایی که در همین نوبت پیدا شدند (شناسه ⇒ نام).</summary>
    public Dictionary<Guid, string> DetailLabels { get; } = [];
}

public sealed class AgentToolbox
{
    public const string SearchCounterparty = "search_counterparty";
    public const string CounterpartyBalance = "get_counterparty_balance";
    public const string AccountBalance = "get_account_balance";
    public const string PreviewVoucher = "preview_voucher";

    private static readonly JsonSerializerOptions Json = AgentJson.Options;

    private readonly ISender _mediator;
    private readonly ISubsidiaryAccountReader _accounts;
    private readonly IDetailReader _details;

    public AgentToolbox(ISender mediator, ISubsidiaryAccountReader accounts, IDetailReader details)
    {
        _mediator = mediator;
        _accounts = accounts;
        _details = details;
    }

    public static IReadOnlyList<AgentToolSpec> Specs(bool dataTools)
    {
        var list = new List<AgentToolSpec>();
        if (dataTools)
        {
            list.Add(new(SearchCounterparty,
                "Search the counterparties (تفصیلی) allowed for one Detail parameter of a template, by name or code. Returns at most 5 items with id, code and title.",
                """
                {"type":"object","properties":{
                  "template_code":{"type":"string"},
                  "parameter_key":{"type":"string","description":"key of a Detail parameter of that template"},
                  "name":{"type":"string","description":"name or code exactly as the user wrote it"}},
                 "required":["template_code","parameter_key","name"]}
                """));
            list.Add(new(CounterpartyBalance,
                "Balance (مانده) of one counterparty in the account used by a Detail parameter of a template, for the current fiscal year.",
                """
                {"type":"object","properties":{
                  "template_code":{"type":"string"},
                  "parameter_key":{"type":"string"},
                  "counterparty_id":{"type":"string","description":"id returned by search_counterparty"}},
                 "required":["template_code","parameter_key","counterparty_id"]}
                """));
            list.Add(new(AccountBalance,
                "Balance (مانده) of subsidiary accounts (معین) of the user's unit whose title contains the text or whose code starts with it, for the current fiscal year. At most 5 accounts.",
                """
                {"type":"object","properties":{"query":{"type":"string","description":"account title words (e.g. «صندوق») or code"}},"required":["query"]}
                """));
        }
        list.Add(new(PreviewVoucher,
            "Build a preview (draft, not saved) of the voucher of one template from the answers. Returns success with a plain description, total and risk, or errors naming the parameter to ask about. Call again after any change.",
            """
            {"type":"object","properties":{
              "template_code":{"type":"string"},
              "voucher_date":{"type":["string","null"],"description":"Jalali yyyyMMdd, or null for today"},
              "answers":{"type":"array","items":{"type":"object","properties":{
                  "key":{"type":"string"},
                  "value":{"type":"string","description":"Amount: Rials digits; Date: Jalali yyyyMMdd; Detail: counterparty id (or the name if no search tool); Choice: option value; Text: text"}},
                "required":["key","value"]}}},
             "required":["template_code","answers"]}
            """));
        return list;
    }

    public async Task<(string ResultJson, AgentStepDto Step)> RunAsync(AgentToolCall call, AgentToolContext ctx, CancellationToken ct)
    {
        JsonObject args;
        try
        {
            args = JsonNode.Parse(string.IsNullOrWhiteSpace(call.ArgumentsJson) ? "{}" : call.ArgumentsJson) as JsonObject ?? [];
        }
        catch (JsonException)
        {
            return Error(call.Name, "ورودی ابزار JSON معتبر نیست.", "ابزار");
        }

        try
        {
            return call.Name switch
            {
                SearchCounterparty when ctx.DataTools => await SearchAsync(args, ctx, ct),
                CounterpartyBalance when ctx.DataTools => await CounterpartyBalanceAsync(args, ctx, ct),
                AccountBalance when ctx.DataTools => await AccountBalanceAsync(args, ctx, ct),
                PreviewVoucher => await PreviewAsync(args, ctx, ct),
                _ => Error(call.Name, $"ابزار «{call.Name}» وجود ندارد.", "ابزار ناشناخته"),
            };
        }
        catch (RoleAccessDeniedException)
        {
            return Error(call.Name, "نقش کاربر اجازهٔ دیدن این اطلاعات را ندارد.", Label(call.Name));
        }
        catch (Exception ex) when (ex is NotFoundException or BusinessRuleException or FluentValidation.ValidationException)
        {
            return Error(call.Name, ex.Message, Label(call.Name));
        }
    }

    // ─────────── search_counterparty ───────────

    private async Task<(string, AgentStepDto)> SearchAsync(JsonObject args, AgentToolContext ctx, CancellationToken ct)
    {
        var name = Str(args, "name");
        var label = $"جستجوی طرف حساب «{name}»";
        if (string.IsNullOrWhiteSpace(name)) return Error(SearchCounterparty, "نام خالی است.", label);
        var (binding, error) = await BindingAsync(args, ctx, ct);
        if (binding is null) return Error(SearchCounterparty, error!, label);

        var page = await _mediator.Send(new GetTafsiliLevelItemsQuery(binding.Value.AccountId, binding.Value.LevelId, name.Trim(), 1, 5), ct);
        foreach (var i in page.Items) ctx.DetailLabels[i.Id] = i.Label;
        var result = new
        {
            total = page.TotalCount,
            items = page.Items.Select(i => new { id = i.Id, code = i.TafsiliCode, title = i.Label }),
        };
        return Ok(SearchCounterparty, result, label);
    }

    // ─────────── get_counterparty_balance ───────────

    private async Task<(string, AgentStepDto)> CounterpartyBalanceAsync(JsonObject args, AgentToolContext ctx, CancellationToken ct)
    {
        var label = "ماندهٔ طرف حساب";
        if (!Guid.TryParse(Str(args, "counterparty_id"), out var detailId))
            return Error(CounterpartyBalance, "شناسهٔ طرف حساب نامعتبر است؛ اول search_counterparty را صدا بزن.", label);
        var (binding, error) = await BindingAsync(args, ctx, ct);
        if (binding is null) return Error(CounterpartyBalance, error!, label);

        var detail = (await _details.GetByIdsAsync([detailId], ctx.VahedCode, ct)).GetValueOrDefault(detailId);
        if (detail is null || !detail.IsVisibleToUnit)
            return Error(CounterpartyBalance, "این طرف حساب برای واحد کاربر پیدا نشد.", label);
        label = $"ماندهٔ «{detail.Title}»";

        var level = (AccountReviewLevel)((int)AccountReviewLevel.Moin + binding.Value.Level);
        var review = await _mediator.Send(new GetAccountReviewQuery(Year(ctx), level,
            [new AccountReviewScopeItem { Level = AccountReviewLevel.Moin, Code = binding.Value.AccountCode }],
            null, null, null, null, null, null), ct);
        var row = review.Rows.FirstOrDefault(r => r.Code == detail.Code);
        var balance = row is null ? 0 : row.DebtorBalance - row.CreditorBalance;
        return Ok(CounterpartyBalance, new
        {
            account = binding.Value.AccountTitle,
            counterparty = detail.Title,
            year = Year(ctx),
            debit = row?.Debtor ?? 0,
            credit = row?.Creditor ?? 0,
            balance = Math.Abs(balance),
            side = balance > 0 ? "بدهکار" : balance < 0 ? "بستانکار" : "بی‌مانده",
        }, label);
    }

    // ─────────── get_account_balance ───────────

    private async Task<(string, AgentStepDto)> AccountBalanceAsync(JsonObject args, AgentToolContext ctx, CancellationToken ct)
    {
        var query = Str(args, "query")?.Trim();
        var label = $"ماندهٔ حساب «{query}»";
        if (string.IsNullOrWhiteSpace(query)) return Error(AccountBalance, "متن جستجو خالی است.", label);

        var review = await _mediator.Send(new GetAccountReviewQuery(Year(ctx), AccountReviewLevel.Moin, null,
            null, null, null, null, null, null), ct);
        var digits = Digits(query);
        var words = Normalize(query).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var hits = review.Rows
            .Where(r => (digits.All(char.IsAsciiDigit) && r.Code.StartsWith(digits, StringComparison.Ordinal))
                        || words.All(w => Normalize(r.Name).Contains(w, StringComparison.Ordinal)))
            .Take(5)
            .Select(r =>
            {
                var b = r.DebtorBalance - r.CreditorBalance;
                return new
                {
                    code = r.Code, title = r.Name, debit = r.Debtor, credit = r.Creditor,
                    balance = Math.Abs(b), side = b > 0 ? "بدهکار" : b < 0 ? "بستانکار" : "بی‌مانده",
                };
            }).ToList();
        return Ok(AccountBalance, new { year = Year(ctx), accounts = hits }, label);
    }

    // ─────────── preview_voucher ───────────

    private async Task<(string, AgentStepDto)> PreviewAsync(JsonObject args, AgentToolContext ctx, CancellationToken ct)
    {
        var template = FindTemplate(ctx, Str(args, "template_code"));
        if (template is null)
            return Error(PreviewVoucher, "کد الگو در فهرست نیست.", "پیش‌نمایش سند");
        var label = $"پیش‌نمایش «{template.Title}»";

        DateOnly? voucherDate = null;
        var rawDate = Str(args, "voucher_date");
        if (!string.IsNullOrWhiteSpace(rawDate))
        {
            if (JalaliToDate(rawDate) is not { } d) return Error(PreviewVoucher, "تاریخ سند باید شمسی yyyyMMdd باشد.", label);
            voucherDate = d;
        }

        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var answers = new List<AgentProposalAnswerDto>();
        var problems = new List<object>();
        if (args["answers"] is JsonArray arr)
        {
            foreach (var item in arr.OfType<JsonObject>())
            {
                var key = Str(item, "key");
                var raw = Str(item, "value")?.Trim();
                var p = template.Parameters.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
                if (p is null || string.IsNullOrEmpty(raw) || values.ContainsKey(p.Key)) continue;

                var (value, display, problem) = await AnswerAsync(template, p, raw, ctx, ct);
                if (problem is not null)
                {
                    problems.Add(new { parameter_key = p.Key, message = problem, ask = p.AskPrompt });
                    continue;
                }
                values[p.Key] = value!.Value.Engine;
                answers.Add(new AgentProposalAnswerDto(p.Key, p.Title, p.Type, value.Value.Proposal, display!));
            }
        }
        if (problems.Count > 0)
            return Ok(PreviewVoucher, new { success = false, errors = problems }, label, succeeded: false);

        var sources = values.Keys.ToDictionary(k => k, _ => (string?)"ai", StringComparer.OrdinalIgnoreCase);
        var r = await _mediator.Send(new PreviewOperationQuery(template.Id, voucherDate, values, sources) { Year = ctx.Year }, ct);
        if (!r.Success || r.Draft is null)
        {
            return Ok(PreviewVoucher, new
            {
                success = false,
                errors = r.Errors.Select(e => new { parameter_key = e.ParameterKey, message = e.Message, ask = e.AskPrompt }),
            }, label, succeeded: false);
        }

        // تفصیلی‌هایی که پیش‌نویس با نام کامل برگرداند برای نمایش جواب‌ها
        foreach (var d in r.Draft.Lines.SelectMany(l => l.Details)) ctx.DetailLabels.TryAdd(d.DetailId, d.DetailTitle);
        answers = answers.Select(a => a.Type == ParameterType.Detail && Guid.TryParse(a.Value, out var id) && ctx.DetailLabels.TryGetValue(id, out var l)
            ? a with { Display = l } : a).ToList();

        ctx.Proposal = new AgentProposalDto(template.Id, template.Code, template.Title,
            DateToJalali(r.Draft.VoucherDate), answers, r);

        return Ok(PreviewVoucher, new
        {
            success = true,
            plain_text = r.PlainText ?? r.Draft.PlainText ?? r.Draft.Description,
            voucher_date = DateToJalali(r.Draft.VoucherDate),
            total_rials = r.Draft.TotalDebit,
            risk = r.Risk?.Level,
            warnings = r.Warnings ?? [],
            lines = r.Draft.Lines.Select(l => new
            {
                account = l.SubsidiaryAccountTitle,
                counterparty = ctx.DataTools ? string.Join("، ", l.Details.Select(d => d.DetailTitle)) : null,
                debit = l.Debit,
                credit = l.Credit,
            }),
        }, label);
    }

    private async Task<((string Engine, string Proposal)? Value, string? Display, string? Problem)> AnswerAsync(
        OperationTemplate t, TemplateParameter p, string raw, AgentToolContext ctx, CancellationToken ct)
    {
        switch (p.Type)
        {
            case ParameterType.Amount:
                var digits = Digits(raw).Replace(",", "").Replace("٬", "").Replace(" ", "");
                return long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var amount) && amount > 0
                    ? ((digits, digits), amount.ToString("N0", CultureInfo.InvariantCulture) + " ریال", null)
                    : (null, null, "مبلغ باید عدد مثبت به ریال باشد.");
            case ParameterType.Date:
                var jalali = Digits(raw).Replace("/", "").Replace("-", "");
                return JalaliToDate(jalali) is { } d
                    ? ((d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), jalali), $"{jalali[..4]}/{jalali[4..6]}/{jalali[6..]}", null)
                    : (null, null, "تاریخ باید شمسی yyyyMMdd باشد.");
            case ParameterType.Choice:
                var options = ChoiceOptions.Parse(p.OptionsJson);
                if (string.Equals(raw, ChoiceOptions.Unknown, StringComparison.OrdinalIgnoreCase))
                    return ((ChoiceOptions.Unknown, ChoiceOptions.Unknown), ChoiceOptions.UnknownLabel, null);
                var option = options.FirstOrDefault(o => string.Equals(o.Value, raw, StringComparison.OrdinalIgnoreCase)
                                                         || string.Equals(o.Label, raw, StringComparison.Ordinal));
                return option is null
                    ? (null, null, "گزینه در فهرست نیست: " + string.Join("، ", options.Select(o => $"{o.Value}={o.Label}")))
                    : ((option.Value, option.Value), option.Label, null);
            case ParameterType.Detail:
                if (Guid.TryParse(raw, out var id))
                    return ((id.ToString(), id.ToString()), ctx.DetailLabels.GetValueOrDefault(id, raw), null);
                // نام به‌جای شناسه (سرویس بیرونی بدون ابزار جستجو): فقط یک نتیجهٔ یکتا پذیرفته می‌شود.
                var binding = Binding(t, p.Key);
                if (binding is null) return (null, null, "این سؤال به ردیفی از الگو وصل نیست.");
                var account = (await _accounts.GetByIdsAsync([binding.Value.AccountId], ct)).GetValueOrDefault(binding.Value.AccountId);
                var levelId = account?.DetailLevels.FirstOrDefault(l => l.Level == binding.Value.Level)?.LevelId;
                if (levelId is null) return (null, null, "سطح تفصیلی این سؤال پیدا نشد.");
                var page = await _mediator.Send(new GetTafsiliLevelItemsQuery(binding.Value.AccountId, levelId.Value, raw, 1, 2), ct);
                if (page.Items.Count == 1)
                {
                    var hit = page.Items[0];
                    ctx.DetailLabels[hit.Id] = hit.Label;
                    return ((hit.Id.ToString(), hit.Id.ToString()), hit.Label, null);
                }
                return (null, null, page.Items.Count == 0
                    ? $"طرف حسابی با نام «{raw}» پیدا نشد؛ از کاربر نام دقیق‌تر را بپرس."
                    : $"بیش از یک طرف حساب با نام «{raw}» هست؛ از کاربر نام کامل‌تر را بپرس.");
            default:
                return ((raw, raw), raw, null);
        }
    }

    // ─────────── کمکی ───────────

    private async Task<((Guid AccountId, Guid LevelId, int Level, string AccountCode, string AccountTitle)? Binding, string? Error)> BindingAsync(
        JsonObject args, AgentToolContext ctx, CancellationToken ct)
    {
        var template = FindTemplate(ctx, Str(args, "template_code"));
        if (template is null) return (null, "کد الگو در فهرست نیست.");
        var key = Str(args, "parameter_key");
        var p = template.Parameters.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
        if (p is null || p.Type != ParameterType.Detail) return (null, "این کلید، سؤال طرف حساب این الگو نیست.");
        var binding = Binding(template, p.Key);
        if (binding is null) return (null, "این سؤال به ردیفی از الگو وصل نیست.");
        var account = (await _accounts.GetByIdsAsync([binding.Value.AccountId], ct)).GetValueOrDefault(binding.Value.AccountId);
        var levelId = account?.DetailLevels.FirstOrDefault(l => l.Level == binding.Value.Level)?.LevelId;
        if (account is null || levelId is null) return (null, "حساب یا سطح تفصیلی این سؤال پیدا نشد.");
        return ((account.Id, levelId.Value, binding.Value.Level, account.Code, account.Title), null);
    }

    private static (Guid AccountId, int Level)? Binding(OperationTemplate t, string key)
    {
        foreach (var line in t.Lines.OrderBy(l => l.SortOrder))
            foreach (var d in line.Details)
                if (string.Equals(d.ParameterKey, key, StringComparison.OrdinalIgnoreCase))
                    return (line.SubsidiaryAccountId, d.Level);
        return null;
    }

    private static OperationTemplate? FindTemplate(AgentToolContext ctx, string? code) =>
        code is null ? null : ctx.Templates.FirstOrDefault(t => string.Equals(t.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));

    private static string Year(AgentToolContext ctx) =>
        !string.IsNullOrWhiteSpace(ctx.Year) ? ctx.Year! : IntentPromptBuilder.TodayJalali()[..4];

    private static string Label(string tool) => tool switch
    {
        SearchCounterparty => "جستجوی طرف حساب",
        CounterpartyBalance => "ماندهٔ طرف حساب",
        AccountBalance => "ماندهٔ حساب",
        PreviewVoucher => "پیش‌نمایش سند",
        _ => tool,
    };

    private static (string, AgentStepDto) Ok(string tool, object result, string label, bool succeeded = true) =>
        (JsonSerializer.Serialize(result, Json), new AgentStepDto(tool, label, succeeded));

    private static (string, AgentStepDto) Error(string tool, string message, string label) =>
        (JsonSerializer.Serialize(new { error = message }, Json), new AgentStepDto(tool, label, false));

    private static string? Str(JsonObject o, string name) =>
        o.TryGetPropertyValue(name, out var n) && n is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;

    public static string Digits(string s)
    {
        var chars = s.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] is >= '۰' and <= '۹') chars[i] = (char)('0' + (chars[i] - '۰'));
            else if (chars[i] is >= '٠' and <= '٩') chars[i] = (char)('0' + (chars[i] - '٠'));
        }
        return new string(chars).Trim();
    }

    /// <summary>عربی ⇒ فارسی (ی، ک)، بدون نیم‌فاصله، کوچک.</summary>
    public static string Normalize(string s) =>
        s.Replace('ي', 'ی').Replace('ك', 'ک').Replace("‌", " ").Trim().ToLowerInvariant();

    public static DateOnly? JalaliToDate(string? jalali)
    {
        var s = Digits(jalali ?? "").Replace("/", "").Replace("-", "");
        if (s.Length != 8 || !s.All(char.IsAsciiDigit)) return null;
        var y = int.Parse(s[..4], CultureInfo.InvariantCulture);
        var m = int.Parse(s[4..6], CultureInfo.InvariantCulture);
        var d = int.Parse(s[6..], CultureInfo.InvariantCulture);
        var pc = new PersianCalendar();
        if (y is < 1300 or > 1499 || m is < 1 or > 12 || d < 1 || d > pc.GetDaysInMonth(y, m)) return null;
        return DateOnly.FromDateTime(pc.ToDateTime(y, m, d, 0, 0, 0, 0));
    }

    public static string DateToJalali(DateOnly date)
    {
        var pc = new PersianCalendar();
        var dt = date.ToDateTime(TimeOnly.MinValue);
        return $"{pc.GetYear(dt):0000}{pc.GetMonth(dt):00}{pc.GetDayOfMonth(dt):00}";
    }
}
