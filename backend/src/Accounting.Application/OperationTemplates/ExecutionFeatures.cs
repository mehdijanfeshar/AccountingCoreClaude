using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.OperationTemplates;
using MediatR;

namespace Accounting.Application.OperationTemplates;

// ═══════════════════════ ردپای حسابیار: اخیرها، تاریخچه، آمار، آزمایش الگو ═══════════════════════
// همه از TB_OP_EXECUTION (بدون DDL تازه). فقط خواندن.

// ─────────── «عملیات‌های اخیر من» و «تکرار» ───────────

/// <param name="Value">قالب جواب گفتگو: مبلغ ریال، تفصیلی Guid، تاریخ شمسی <c>yyyyMMdd</c>، متن.</param>
/// <param name="Label">برای تفصیلی عنوان آن؛ بقیه را فرانت قالب‌بندی می‌کند.</param>
public sealed record RecentAnswerDto(string Key, ParameterType Type, string Value, string? Label);

public sealed record RecentOperationDto(
    Guid ExecutionId, Guid TemplateId, string TemplateTitle, string VoucherNo, Guid VoucherId,
    DateTime CreatedAtUtc, IReadOnlyList<RecentAnswerDto> Answers);

/// <summary>آخرین اجرای هر الگو توسط کاربر جاری در واحدش (فقط مسیر الگو، نه سند دستی) — برای «تکرار».</summary>
public sealed record ListMyRecentOperationsQuery(int Take = 8) : IRequest<IReadOnlyList<RecentOperationDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class ListMyRecentOperationsHandler : IRequestHandler<ListMyRecentOperationsQuery, IReadOnlyList<RecentOperationDto>>
{
    private readonly IOperationTemplateRepository _repo;
    private readonly IDetailReader _details;
    private readonly ICurrentUser _currentUser;
    private readonly AssistantUnitPolicy _policy;

    public ListMyRecentOperationsHandler(IOperationTemplateRepository repo, IDetailReader details, ICurrentUser currentUser, AssistantUnitPolicy policy)
    { _repo = repo; _details = details; _currentUser = currentUser; _policy = policy; }

    public async Task<IReadOnlyList<RecentOperationDto>> Handle(ListMyRecentOperationsQuery q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q.VahedCode) || string.IsNullOrWhiteSpace(_currentUser.UserId))
            return [];

        var (executions, _) = await _repo.ListExecutionsAsync(
            new ExecutionFilter(q.VahedCode, _currentUser.UserId, null, "Form", null, null, 0, 60), ct);
        var templates = (await _policy.FilterAsync(await _repo.ListActiveWithParametersAsync(ct), q.VahedCode, ct))
            .ToDictionary(t => t.Id);

        var latest = executions
            .Where(e => e.OperationTemplateId is { } id && templates.ContainsKey(id))
            .GroupBy(e => e.OperationTemplateId!.Value)
            .Select(g => g.First())
            .Take(Math.Clamp(q.Take, 1, 20))
            .ToList();

        var parsed = latest.Select(e => (Execution: e, Template: templates[e.OperationTemplateId!.Value], Values: ParseValues(e.InputJson))).ToList();
        var detailIds = parsed
            .SelectMany(p => p.Template.Parameters.Where(x => x.Type == ParameterType.Detail)
                .Select(x => p.Values.TryGetValue(x.Key, out var v) && Guid.TryParse(v, out var g) ? g : Guid.Empty))
            .Where(g => g != Guid.Empty).Distinct().ToList();
        var details = await _details.GetByIdsAsync(detailIds, q.VahedCode, ct);

        return parsed.Select(p => new RecentOperationDto(
            p.Execution.Id, p.Template.Id, p.Template.Title, p.Execution.VoucherNo, p.Execution.VoucherId, p.Execution.CreatedAtUtc,
            p.Template.Parameters.OrderBy(x => x.SortOrder)
                .Select(x => ToAnswer(x, p.Values, details))
                .Where(a => a is not null)
                .Select(a => a!)
                .ToList()))
            .ToList();
    }

    private static Dictionary<string, string?> ParseValues(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string?>>(json) is { } d
                ? new Dictionary<string, string?>(d, StringComparer.OrdinalIgnoreCase)
                : new(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static RecentAnswerDto? ToAnswer(TemplateParameter p, Dictionary<string, string?> values, IReadOnlyDictionary<Guid, DetailInfo> details)
    {
        if (!values.TryGetValue(p.Key, out var raw) || string.IsNullOrWhiteSpace(raw))
            return null;
        switch (p.Type)
        {
            case ParameterType.Detail:
                // تفصیلی که دیگر فعال/قابل دید نیست تکرار نمی‌شود — دوباره پرسیده می‌شود.
                return Guid.TryParse(raw, out var id) && details.TryGetValue(id, out var d) && d.IsActive && d.IsVisibleToUnit
                    ? new RecentAnswerDto(p.Key, p.Type, raw, d.Title)
                    : null;
            case ParameterType.Date:
                return DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                    ? new RecentAnswerDto(p.Key, p.Type, FiscalYearGuard.Jalali(date).Replace("/", ""), null)
                    : null;
            default:
                return new RecentAnswerDto(p.Key, p.Type, raw, null);
        }
    }
}

// ─────────── صفحهٔ «سندهای حسابیار» ───────────

public sealed record ExecutionRowDto(
    Guid Id, Guid? TemplateId, string TemplateCode, string? TemplateTitle, string Channel,
    Guid VoucherId, string VoucherNo, string CreatedBy, DateTime CreatedAtUtc, string VahedCode);

public sealed record ExecutionPageDto(IReadOnlyList<ExecutionRowDto> Items, int Total, int Page, int PageSize);

/// <param name="FromDate">شمسی <c>yyyyMMdd</c> (شامل).</param>
/// <param name="ToDate">شمسی <c>yyyyMMdd</c> (شامل).</param>
/// <param name="MineOnly">فقط سندهایی که کاربر جاری با حسابیار زده است.</param>
public sealed record ListOperationExecutionsQuery(
    string? FromDate, string? ToDate, Guid? TemplateId, string? Channel, bool MineOnly, int Page = 1, int PageSize = 20)
    : IRequest<ExecutionPageDto>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class ListOperationExecutionsHandler : IRequestHandler<ListOperationExecutionsQuery, ExecutionPageDto>
{
    private readonly IOperationTemplateRepository _repo;
    private readonly ICurrentUser _currentUser;

    public ListOperationExecutionsHandler(IOperationTemplateRepository repo, ICurrentUser currentUser)
    { _repo = repo; _currentUser = currentUser; }

    public async Task<ExecutionPageDto> Handle(ListOperationExecutionsQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 5, 100);
        var channel = q.Channel is "Form" or "Compose" or "Agent" ? q.Channel : null;
        var (items, total) = await _repo.ListExecutionsAsync(new ExecutionFilter(
            q.VahedCode, q.MineOnly ? _currentUser.UserId : null, q.TemplateId, channel,
            JalaliToUtc(q.FromDate), JalaliToUtc(q.ToDate)?.AddDays(1), (page - 1) * size, size), ct);

        var titles = (await _repo.ListAllAsync(ct)).ToDictionary(t => t.Id, t => t.Title);
        return new ExecutionPageDto(
            items.Select(e => new ExecutionRowDto(e.Id, e.OperationTemplateId, e.TemplateCode,
                e.OperationTemplateId is { } id && titles.TryGetValue(id, out var title) ? title : null,
                e.Channel, e.VoucherId, e.VoucherNo, e.CreatedBy, e.CreatedAtUtc, e.VahedCode)).ToList(),
            total, page, size);
    }

    private static DateTime? JalaliToUtc(string? yyyymmdd)
    {
        if (string.IsNullOrWhiteSpace(yyyymmdd)) return null;
        var s = yyyymmdd.Replace("/", "").Trim();
        if (s.Length != 8 || !s.All(char.IsAsciiDigit)) return null;
        try
        {
            var local = new PersianCalendar().ToDateTime(int.Parse(s[..4]), int.Parse(s[4..6]), int.Parse(s[6..]), 0, 0, 0, 0);
            return DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime();
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}

// ─────────── آمار استفادهٔ الگوها (صفحهٔ الگوها) ───────────

public sealed record TemplateUsageDto(Guid TemplateId, int Count, DateTime LastUsedUtc);

/// <summary>مدیر ستاد: همهٔ واحدها؛ دیگران: واحد خودشان.</summary>
public sealed record OperationTemplateUsageQuery : IRequest<IReadOnlyList<TemplateUsageDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class OperationTemplateUsageHandler : IRequestHandler<OperationTemplateUsageQuery, IReadOnlyList<TemplateUsageDto>>
{
    private readonly IOperationTemplateRepository _repo;
    private readonly ICurrentUser _currentUser;

    public OperationTemplateUsageHandler(IOperationTemplateRepository repo, ICurrentUser currentUser)
    { _repo = repo; _currentUser = currentUser; }

    public async Task<IReadOnlyList<TemplateUsageDto>> Handle(OperationTemplateUsageQuery q, CancellationToken ct)
    {
        var scope = _currentUser.IsInRole(AppRoles.SetadAdmin) ? null : q.VahedCode;
        return (await _repo.UsageAsync(scope, ct)).Select(r => new TemplateUsageDto(r.TemplateId, r.Count, r.LastUsedUtc)).ToList();
    }
}

// ─────────── «آزمایش الگو» — بدون ذخیرهٔ الگو و بدون ثبت سند ───────────

/// <summary>
/// تعریف الگو (همان بدنهٔ ذخیره) + جواب‌های نمونه ⇒ پیش‌نویس سند از همان موتور، در واحد جاری. چیزی نوشته نمی‌شود.
/// خطای تعریف ⇒ خطاهای عمومی؛ خطای جواب‌ها ⇒ با کلید همان سؤال.
/// </summary>
public sealed record TestOperationTemplateQuery(
    string Code, string Title, string Description, string VoucherDescriptionPattern,
    List<TemplateParameterDto> Parameters, List<TemplateLineDto> Lines,
    Guid? SystemTypeId, string? Keywords,
    DateOnly? VoucherDate, Dictionary<string, string?> Values) : IRequest<OperationResult>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class TestOperationTemplateHandler : IRequestHandler<TestOperationTemplateQuery, OperationResult>
{
    private readonly TemplateDefinitionValidator _validator;
    private readonly IVoucherGenerationEngine _engine;

    public TestOperationTemplateHandler(TemplateDefinitionValidator validator, IVoucherGenerationEngine engine)
    { _validator = validator; _engine = engine; }

    public async Task<OperationResult> Handle(TestOperationTemplateQuery q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q.VahedCode))
            return new(false, null, [new EngineError(EngineErrorCode.InvalidParameterValue, "کاربر به هیچ واحدی متصل نیست.")]);

        var t = TemplateMapping.Build(Guid.NewGuid(), q.Code, q.Title, q.Description, q.VoucherDescriptionPattern,
            q.Parameters, q.Lines, q.SystemTypeId, q.Keywords);
        var definitionErrors = await _validator.ValidateAsync(t, ct);
        if (definitionErrors.Count > 0)
            return new(false, null, definitionErrors.Select(m => new EngineError(EngineErrorCode.InvalidParameterValue, m)).ToList());

        var values = InputNormalizer.NormalizeAll(t, q.Values);
        var date = TemplateVoucherDate.Resolve(t, values, q.VoucherDate, PreviewOperationHandler.Today());
        var r = await _engine.GenerateAsync(t, q.VahedCode, date, values, ct);
        return new(r.IsSuccess, r.Draft, r.Errors);
    }
}
