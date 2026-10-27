using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.OperationTemplates;
using FluentValidation;
using MediatR;

namespace Accounting.Application.OperationTemplates.Agent;

// ═══════════════════════ حسابیار فاز ۶۵ — سنجش هوش مصنوعی (DDL 088) ═══════════════════════
//
// نمونهٔ طلایی: جمله + الگو و جواب‌های درست (دستی، یا از اجرای واقعی). اجرای سایه: جملهٔ اسنادی که کاربر بدون
// هوش مصنوعی ثبت کرد به مدل داده می‌شود و برداشت مدل با آنچه واقعاً ثبت شد مقایسه می‌شود — بی‌آنکه کاربری
// چیزی ببیند. هر دو همان مسیر «فهم جمله» کاربر را می‌روند (IntentRun). فقط صاحب قابلیت ai.evaluate.

public interface IAiEvaluationRepository
{
    Task<IReadOnlyList<AiGoldenCase>> ListGoldenAsync(bool activeOnly, CancellationToken ct);

    /// <summary>نمونهٔ حذف‌نشده (tracked) یا null.</summary>
    Task<AiGoldenCase?> FindGoldenAsync(Guid id, CancellationToken ct);

    Task AddGoldenAsync(AiGoldenCase golden, CancellationToken ct);

    /// <summary>اجراهایی که قبلاً نمونهٔ طلایی شده‌اند (حتی اگر حذف شده باشند، دوباره ساخته نمی‌شوند).</summary>
    Task<IReadOnlySet<Guid>> GoldenSourceExecutionIdsAsync(CancellationToken ct);

    Task AddRunAsync(AiEvalRun run, IReadOnlyList<AiEvalItem> items, CancellationToken ct);
    Task<IReadOnlyList<AiEvalRun>> ListRunsAsync(int take, CancellationToken ct);
    Task<AiEvalRun?> FindRunAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<AiEvalItem>> ListItemsAsync(Guid runId, CancellationToken ct);

    /// <summary>
    /// اجراهای الگو با جملهٔ کاربر و بدون هوش مصنوعی (AI_TRACE_ID خالی)، سند حذف‌نشده، جدیدترین اول.
    /// <paramref name="vahedCode"/> null = همهٔ واحدها.
    /// </summary>
    Task<IReadOnlyList<ShadowSourceRow>> ListShadowSourcesAsync(string? vahedCode, int take, CancellationToken ct);
}

public sealed record ShadowSourceRow(Guid ExecutionId, string VahedCode, string Sentence, string TemplateCode, string InputJson);

internal static class AiEvalAccess
{
    public static async Task EnsureAsync(ICurrentUser user, IHeadquartersAccessService? hq, CancellationToken ct)
    {
        var allowed = hq is null ? user.IsInRole(AppRoles.SetadAdmin) : await hq.HasAbilityAsync(AbilityCatalog.AiEvaluate, ct);
        if (!allowed)
            throw new RoleAccessDeniedException("سنجش هوش مصنوعی فقط برای کاربر ستاد مرکزی یا نقشی که این قابلیت را دارد ممکن است.");
    }
}

// ─────────── امتیازدهی ───────────

public sealed record AiEvalDiffDto(string Key, string? Expected, string? Actual);

public sealed record AiEvalScore(bool TemplateOk, int AnswersExpected, int AnswersOk, IReadOnlyList<AiEvalDiffDto> Diffs);

/// <summary>
/// مقایسهٔ برداشت مدل با جواب درست. الگو: کد یکسان (هر دو خالی هم درست). جواب‌ها فقط وقتی الگو درست است شمرده
/// می‌شوند: مبلغ/تاریخ/گزینه برابر دقیق؛ تفصیلی = یکی شامل دیگری پس از یکسان‌سازی حروف؛ متن آزاد شمرده نمی‌شود.
/// </summary>
public static class AiEvalScorer
{
    public static AiEvalScore Score(InterpretResultDto actual, string? actualTemplateCode, string? expectedTemplateCode,
        IReadOnlyDictionary<string, string> expected, OperationTemplate? template)
    {
        var templateOk = string.Equals(Clean(expectedTemplateCode), Clean(actualTemplateCode), StringComparison.OrdinalIgnoreCase);
        var diffs = new List<AiEvalDiffDto>();
        if (!templateOk)
            diffs.Add(new("template", expectedTemplateCode, actualTemplateCode));

        var scored = expected.Where(kv => TypeOf(template, kv.Key) is not (null or ParameterType.Text)).ToList();
        var ok = 0;
        if (templateOk)
        {
            var answers = actual.Answers.ToDictionary(a => a.Key, a => a.Value, StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in scored)
            {
                var got = answers.GetValueOrDefault(key);
                if (Matches(TypeOf(template, key)!.Value, value, got)) ok++;
                else diffs.Add(new(key, value, got));
            }
        }
        return new AiEvalScore(templateOk, scored.Count, ok, diffs);
    }

    private static bool Matches(ParameterType type, string expected, string? actual)
    {
        if (string.IsNullOrWhiteSpace(actual)) return false;
        if (type != ParameterType.Detail)
            return string.Equals(AgentToolbox.Digits(expected).Trim(), AgentToolbox.Digits(actual).Trim(), StringComparison.OrdinalIgnoreCase);
        var e = AgentToolbox.Normalize(expected);
        var a = AgentToolbox.Normalize(actual);
        return a.Length >= 2 && (e.Contains(a, StringComparison.Ordinal) || a.Contains(e, StringComparison.Ordinal));
    }

    private static ParameterType? TypeOf(OperationTemplate? t, string key) =>
        t?.Parameters.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase))?.Type;

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

/// <summary>جواب‌های درست از ورودی یک اجرای واقعی: مبلغ، تاریخ (به شمسی)، گزینه، و تفصیلی (به نام).</summary>
public static class AiEvalExpected
{
    private static readonly JsonSerializerOptions Json = AgentJson.Options;

    public static Dictionary<string, string> FromExecution(OperationTemplate t, string inputJson, IReadOnlyDictionary<Guid, DetailInfo> details)
    {
        var values = ListMyRecentOperationsHandler.ParseValues(inputJson);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in t.Parameters)
        {
            if (!values.TryGetValue(p.Key, out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
            switch (p.Type)
            {
                case ParameterType.Amount:
                case ParameterType.Choice:
                    result[p.Key] = raw.Trim();
                    break;
                case ParameterType.Date when DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d):
                    result[p.Key] = AgentToolbox.DateToJalali(d);
                    break;
                case ParameterType.Detail when Guid.TryParse(raw, out var id) && details.TryGetValue(id, out var info):
                    result[p.Key] = info.Title;
                    break;
            }
        }
        return result;
    }

    public static IReadOnlyList<Guid> DetailIds(OperationTemplate t, string inputJson)
    {
        var values = ListMyRecentOperationsHandler.ParseValues(inputJson);
        return t.Parameters.Where(p => p.Type == ParameterType.Detail)
            .Select(p => values.TryGetValue(p.Key, out var v) && Guid.TryParse(v, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty).Distinct().ToList();
    }

    public static string? Serialize(IReadOnlyDictionary<string, string> answers) =>
        answers.Count == 0 ? null : JsonSerializer.Serialize(answers, Json);

    public static IReadOnlyDictionary<string, string> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, string>();
        try
        {
            return new Dictionary<string, string>(JsonSerializer.Deserialize<Dictionary<string, string>>(json, Json) ?? [], StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
    }
}

// ─────────── نمونه‌های طلایی ───────────

public sealed record AiGoldenCaseDto(
    Guid Id, string Sentence, string? ExpectedTemplateCode, IReadOnlyDictionary<string, string> ExpectedAnswers,
    string? Note, bool IsActive, Guid? SourceExecutionId, DateTime CreatedAtUtc);

public sealed record ListAiGoldenCasesQuery : IRequest<IReadOnlyList<AiGoldenCaseDto>>;

public sealed class ListAiGoldenCasesHandler : IRequestHandler<ListAiGoldenCasesQuery, IReadOnlyList<AiGoldenCaseDto>>
{
    private readonly IAiEvaluationRepository _repo;
    private readonly ICurrentUser _currentUser;
    private readonly IHeadquartersAccessService? _hq;

    public ListAiGoldenCasesHandler(IAiEvaluationRepository repo, ICurrentUser currentUser, IHeadquartersAccessService? hq = null)
    {
        _repo = repo;
        _currentUser = currentUser;
        _hq = hq;
    }

    public async Task<IReadOnlyList<AiGoldenCaseDto>> Handle(ListAiGoldenCasesQuery q, CancellationToken ct)
    {
        await AiEvalAccess.EnsureAsync(_currentUser, _hq, ct);
        return (await _repo.ListGoldenAsync(false, ct)).Select(ToDto).ToList();
    }

    internal static AiGoldenCaseDto ToDto(AiGoldenCase g) =>
        new(g.Id, g.Sentence, g.ExpectedTemplateCode, AiEvalExpected.Parse(g.ExpectedAnswersJson), g.Note, g.IsActive, g.SourceExecutionId, g.CreatedAtUtc);
}

/// <param name="Id">null = نمونهٔ تازه.</param>
/// <param name="ExpectedTemplateCode">خالی = هیچ الگویی نباید انتخاب شود.</param>
public sealed record SaveAiGoldenCaseCommand(
    Guid? Id, string Sentence, string? ExpectedTemplateCode, Dictionary<string, string>? ExpectedAnswers, string? Note, bool IsActive = true)
    : IRequest<AiGoldenCaseDto>;

public sealed class SaveAiGoldenCaseValidator : AbstractValidator<SaveAiGoldenCaseCommand>
{
    public SaveAiGoldenCaseValidator()
    {
        RuleFor(x => x.Sentence).NotEmpty().WithMessage("جمله خالی است.").MaximumLength(1000).WithMessage("جمله حداکثر ۱۰۰۰ نویسه است.");
        RuleFor(x => x.Note).MaximumLength(500).WithMessage("یادداشت حداکثر ۵۰۰ نویسه است.");
        RuleFor(x => x.ExpectedAnswers).Must(a => a is null || a.Count <= 30).WithMessage("حداکثر ۳۰ جواب.");
    }
}

public sealed class SaveAiGoldenCaseHandler : IRequestHandler<SaveAiGoldenCaseCommand, AiGoldenCaseDto>
{
    private readonly IAiEvaluationRepository _repo;
    private readonly IOperationTemplateRepository _templates;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IHeadquartersAccessService? _hq;

    public SaveAiGoldenCaseHandler(IAiEvaluationRepository repo, IOperationTemplateRepository templates, IUnitOfWork unitOfWork,
        ICurrentUser currentUser, IHeadquartersAccessService? hq = null)
    {
        _repo = repo;
        _templates = templates;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _hq = hq;
    }

    public async Task<AiGoldenCaseDto> Handle(SaveAiGoldenCaseCommand c, CancellationToken ct)
    {
        await AiEvalAccess.EnsureAsync(_currentUser, _hq, ct);

        var code = string.IsNullOrWhiteSpace(c.ExpectedTemplateCode) ? null : c.ExpectedTemplateCode.Trim();
        var answers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (code is not null)
        {
            var template = (await _templates.ListActiveWithParametersAsync(ct))
                .FirstOrDefault(t => string.Equals(t.Code, code, StringComparison.OrdinalIgnoreCase))
                ?? throw new BusinessRuleException($"الگوی فعالی با کد «{code}» نیست.");
            code = template.Code;
            foreach (var (key, value) in c.ExpectedAnswers ?? [])
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                var p = template.Parameters.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase))
                    ?? throw new BusinessRuleException($"الگوی «{template.Title}» سؤالی با کلید «{key}» ندارد.");
                answers[p.Key] = value.Trim();
            }
        }

        AiGoldenCase golden;
        if (c.Id is { } id)
        {
            golden = await _repo.FindGoldenAsync(id, ct) ?? throw new NotFoundException("AiGoldenCase", id);
        }
        else
        {
            golden = new AiGoldenCase { Id = Guid.NewGuid(), CreatedBy = _currentUser.UserId, CreatedAtUtc = DateTime.UtcNow };
            await _repo.AddGoldenAsync(golden, ct);
        }
        golden.Sentence = c.Sentence.Trim();
        golden.ExpectedTemplateCode = code;
        golden.ExpectedAnswersJson = AiEvalExpected.Serialize(answers);
        golden.Note = string.IsNullOrWhiteSpace(c.Note) ? null : c.Note.Trim();
        golden.IsActive = c.IsActive;

        await _unitOfWork.SaveChangesAsync(ct);
        return ListAiGoldenCasesHandler.ToDto(golden);
    }
}

public sealed record DeleteAiGoldenCaseCommand(Guid Id) : IRequest<Unit>;

public sealed class DeleteAiGoldenCaseHandler : IRequestHandler<DeleteAiGoldenCaseCommand, Unit>
{
    private readonly IAiEvaluationRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IHeadquartersAccessService? _hq;

    public DeleteAiGoldenCaseHandler(IAiEvaluationRepository repo, IUnitOfWork unitOfWork, ICurrentUser currentUser, IHeadquartersAccessService? hq = null)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _hq = hq;
    }

    public async Task<Unit> Handle(DeleteAiGoldenCaseCommand c, CancellationToken ct)
    {
        await AiEvalAccess.EnsureAsync(_currentUser, _hq, ct);
        var golden = await _repo.FindGoldenAsync(c.Id, ct) ?? throw new NotFoundException("AiGoldenCase", c.Id);
        golden.IsDeleted = true;
        await _unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

/// <summary>
/// ساخت نمونهٔ طلایی از اسناد واقعی حسابیار که جملهٔ کاربر دارند (واحد جاری، یا همهٔ واحدها). جواب درست = آنچه
/// کاربر واقعاً ثبت کرد. ⚠️ پیش از تکیه بر نمونه‌ها مرورشان کنید: کاربر ممکن است جمله‌ای نوشته و چیز دیگری ثبت کرده باشد.
/// </summary>
public sealed record ImportAiGoldenCasesCommand(int Take = 50, bool AllUnits = false) : IRequest<int>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class ImportAiGoldenCasesHandler : IRequestHandler<ImportAiGoldenCasesCommand, int>
{
    private readonly IAiEvaluationRepository _repo;
    private readonly IOperationTemplateRepository _templates;
    private readonly IDetailReader _details;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IHeadquartersAccessService? _hq;

    public ImportAiGoldenCasesHandler(IAiEvaluationRepository repo, IOperationTemplateRepository templates, IDetailReader details,
        IUnitOfWork unitOfWork, ICurrentUser currentUser, IHeadquartersAccessService? hq = null)
    {
        _repo = repo;
        _templates = templates;
        _details = details;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _hq = hq;
    }

    public async Task<int> Handle(ImportAiGoldenCasesCommand c, CancellationToken ct)
    {
        await AiEvalAccess.EnsureAsync(_currentUser, _hq, ct);
        var take = Math.Clamp(c.Take, 1, 200);
        var known = await _repo.GoldenSourceExecutionIdsAsync(ct);
        var templates = (await _templates.ListActiveWithParametersAsync(ct)).ToDictionary(t => t.Code, StringComparer.OrdinalIgnoreCase);
        var sources = await _repo.ListShadowSourcesAsync(c.AllUnits ? null : c.VahedCode, take * 3, ct);

        var added = 0;
        foreach (var s in sources.Where(s => !known.Contains(s.ExecutionId)))
        {
            if (added >= take) break;
            if (!templates.TryGetValue(s.TemplateCode, out var t)) continue;
            var details = await _details.GetByIdsAsync(AiEvalExpected.DetailIds(t, s.InputJson), s.VahedCode, ct);
            var expected = AiEvalExpected.FromExecution(t, s.InputJson, details);
            await _repo.AddGoldenAsync(new AiGoldenCase
            {
                Id = Guid.NewGuid(),
                Sentence = s.Sentence.Length <= 1000 ? s.Sentence : s.Sentence[..1000],
                ExpectedTemplateCode = t.Code,
                ExpectedAnswersJson = AiEvalExpected.Serialize(expected),
                Note = $"از سند ثبت‌شدهٔ واحد {s.VahedCode}",
                SourceExecutionId = s.ExecutionId,
                IsActive = true,
                CreatedBy = _currentUser.UserId,
                CreatedAtUtc = DateTime.UtcNow,
            }, ct);
            added++;
        }
        if (added > 0) await _unitOfWork.SaveChangesAsync(ct);
        return added;
    }
}

// ─────────── اجرای سنجش ───────────

public sealed record AiEvalRunDto(
    Guid Id, string Kind, string Provider, string? Model, string PromptVersion, int Total, int TemplateHits,
    int AnswerTotal, int AnswerHits, int Failures, int AvgLatencyMs, DateTime CreatedAtUtc, string CreatedBy);

public sealed record AiEvalItemDto(
    Guid Id, Guid? GoldenCaseId, Guid? ExecutionId, string MaskedSentence, string? ExpectedTemplateCode, string? ActualTemplateCode,
    bool TemplateOk, int AnswersExpected, int AnswersOk, IReadOnlyList<AiEvalDiffDto> Diffs, int LatencyMs, string? Error);

public sealed record AiEvalRunDetailDto(AiEvalRunDto Run, IReadOnlyList<AiEvalItemDto> Items);

/// <param name="Kind"><c>golden</c> یا <c>shadow</c>.</param>
/// <param name="Take">حداکثر جمله‌ها (۱ تا ۵۰)؛ اجرا پس از حدود ۹۰ ثانیه متوقف می‌شود.</param>
/// <param name="AllUnits">فقط اجرای سایه: جمله‌های همهٔ واحدها به‌جای واحد جاری.</param>
public sealed record RunAiEvaluationCommand(string Kind, int Take = 20, bool AllUnits = false) : IRequest<AiEvalRunDetailDto>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;

    [JsonIgnore]
    public string? Year { get; set; }
}

public sealed class RunAiEvaluationValidator : AbstractValidator<RunAiEvaluationCommand>
{
    public RunAiEvaluationValidator() =>
        RuleFor(x => x.Kind).Must(k => k is "golden" or "shadow").WithMessage("نوع سنجش باید golden یا shadow باشد.");
}

public sealed class RunAiEvaluationHandler : IRequestHandler<RunAiEvaluationCommand, AiEvalRunDetailDto>
{
    /// <summary>سقف زمان یک دور، زیر مهلت درخواست IIS.</summary>
    public static readonly TimeSpan TimeBudget = TimeSpan.FromSeconds(90);

    private static readonly JsonSerializerOptions Json = AgentJson.Options;

    private readonly IIntentInterpreter _interpreter;
    private readonly IAiEvaluationRepository _repo;
    private readonly IOperationTemplateRepository _templates;
    private readonly IDetailReader _details;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IHeadquartersAccessService? _hq;

    public RunAiEvaluationHandler(IIntentInterpreter interpreter, IAiEvaluationRepository repo, IOperationTemplateRepository templates,
        IDetailReader details, IUnitOfWork unitOfWork, ICurrentUser currentUser, IHeadquartersAccessService? hq = null)
    {
        _interpreter = interpreter;
        _repo = repo;
        _templates = templates;
        _details = details;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _hq = hq;
    }

    private sealed record Case(Guid? GoldenId, Guid? ExecutionId, string Sentence, string? ExpectedCode, IReadOnlyDictionary<string, string> Expected);

    public async Task<AiEvalRunDetailDto> Handle(RunAiEvaluationCommand c, CancellationToken ct)
    {
        await AiEvalAccess.EnsureAsync(_currentUser, _hq, ct);
        if (!_interpreter.IsEnabled)
            throw new BusinessRuleException("هوش مصنوعی در تنظیمات سرور خاموش است؛ سنجش ممکن نیست.");

        var take = Math.Clamp(c.Take, 1, 50);
        var kind = c.Kind == "shadow" ? AiEvalKind.Shadow : AiEvalKind.Golden;
        // همهٔ الگوهای فعال، بدون فیلتر نوع واحد — نمونه‌ها از واحدهای گوناگون‌اند.
        var templates = await _templates.ListActiveWithParametersAsync(ct);
        var byCode = templates.ToDictionary(t => t.Code, StringComparer.OrdinalIgnoreCase);

        var cases = new List<Case>();
        if (kind == AiEvalKind.Golden)
        {
            cases.AddRange((await _repo.ListGoldenAsync(true, ct)).Take(take)
                .Select(g => new Case(g.Id, null, g.Sentence, g.ExpectedTemplateCode, AiEvalExpected.Parse(g.ExpectedAnswersJson))));
        }
        else
        {
            foreach (var s in await _repo.ListShadowSourcesAsync(c.AllUnits ? null : c.VahedCode, take * 2, ct))
            {
                if (cases.Count >= take) break;
                if (!byCode.TryGetValue(s.TemplateCode, out var t)) continue;
                var details = await _details.GetByIdsAsync(AiEvalExpected.DetailIds(t, s.InputJson), s.VahedCode, ct);
                cases.Add(new Case(null, s.ExecutionId, s.Sentence, t.Code, AiEvalExpected.FromExecution(t, s.InputJson, details)));
            }
        }
        if (cases.Count == 0)
            throw new BusinessRuleException(kind == AiEvalKind.Golden
                ? "هیچ نمونهٔ طلایی فعالی نیست؛ اول نمونه بسازید."
                : "سندی با جملهٔ کاربر و بدون هوش مصنوعی برای اجرای سایه پیدا نشد.");

        var run = new AiEvalRun
        {
            Id = Guid.NewGuid(),
            Kind = kind,
            VahedCode = c.VahedCode,
            Provider = _interpreter.ProviderName,
            Model = _interpreter.ModelName,
            PromptVersion = IntentPromptBuilder.PromptVersion,
            CreatedBy = _currentUser.UserId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        var items = new List<AiEvalItem>();
        var budget = Stopwatch.StartNew();
        foreach (var k in cases)
        {
            if (budget.Elapsed > TimeBudget) break;
            var r = await IntentRun.ExecuteAsync(_interpreter, k.Sentence, templates, c.Year, ct);
            var actualCode = r.Result.TemplateId is { } tid ? templates.FirstOrDefault(t => t.Id == tid)?.Code : null;
            var template = k.ExpectedCode is null ? null : byCode.GetValueOrDefault(k.ExpectedCode);
            var score = r.Result.Succeeded
                ? AiEvalScorer.Score(r.Result, actualCode, k.ExpectedCode, k.Expected, template)
                : new AiEvalScore(false, k.Expected.Count, 0, [new("error", null, r.Error ?? "unparseable")]);
            items.Add(new AiEvalItem
            {
                Id = Guid.NewGuid(),
                RunId = run.Id,
                GoldenCaseId = k.GoldenId,
                ExecutionId = k.ExecutionId,
                MaskedSentence = r.MaskedSentence.Length <= 1000 ? r.MaskedSentence : r.MaskedSentence[..1000],
                ExpectedTemplateCode = k.ExpectedCode,
                ActualTemplateCode = actualCode,
                TemplateOk = score.TemplateOk,
                AnswersExpected = score.AnswersExpected,
                AnswersOk = score.AnswersOk,
                DiffJson = score.Diffs.Count == 0 ? null : PiiMasker.Mask(JsonSerializer.Serialize(score.Diffs, Json)).Masked,
                LatencyMs = r.LatencyMs,
                Error = r.Result.Succeeded ? null : r.Error ?? "unparseable",
            });
        }

        run.Total = items.Count;
        run.TemplateHits = items.Count(i => i.TemplateOk);
        run.AnswerTotal = items.Sum(i => i.AnswersExpected);
        run.AnswerHits = items.Sum(i => i.AnswersOk);
        run.Failures = items.Count(i => i.Error is not null);
        run.AvgLatencyMs = items.Count == 0 ? 0 : (int)items.Average(i => i.LatencyMs);
        await _repo.AddRunAsync(run, items, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new AiEvalRunDetailDto(AiEvalMapping.Run(run), items.Select(AiEvalMapping.Item).ToList());
    }
}

internal static class AiEvalMapping
{
    private static readonly JsonSerializerOptions Json = AgentJson.Options;

    public static AiEvalRunDto Run(AiEvalRun r) => new(r.Id, r.Kind == AiEvalKind.Shadow ? "shadow" : "golden", r.Provider, r.Model,
        r.PromptVersion, r.Total, r.TemplateHits, r.AnswerTotal, r.AnswerHits, r.Failures, r.AvgLatencyMs, r.CreatedAtUtc, r.CreatedBy);

    public static AiEvalItemDto Item(AiEvalItem i) => new(i.Id, i.GoldenCaseId, i.ExecutionId, i.MaskedSentence, i.ExpectedTemplateCode,
        i.ActualTemplateCode, i.TemplateOk, i.AnswersExpected, i.AnswersOk, Diffs(i.DiffJson), i.LatencyMs, i.Error);

    private static IReadOnlyList<AiEvalDiffDto> Diffs(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<AiEvalDiffDto>>(json, Json) ?? []; }
        catch (JsonException) { return []; }
    }
}

public sealed record ListAiEvalRunsQuery : IRequest<IReadOnlyList<AiEvalRunDto>>;

public sealed class ListAiEvalRunsHandler : IRequestHandler<ListAiEvalRunsQuery, IReadOnlyList<AiEvalRunDto>>
{
    private readonly IAiEvaluationRepository _repo;
    private readonly ICurrentUser _currentUser;
    private readonly IHeadquartersAccessService? _hq;

    public ListAiEvalRunsHandler(IAiEvaluationRepository repo, ICurrentUser currentUser, IHeadquartersAccessService? hq = null)
    {
        _repo = repo;
        _currentUser = currentUser;
        _hq = hq;
    }

    public async Task<IReadOnlyList<AiEvalRunDto>> Handle(ListAiEvalRunsQuery q, CancellationToken ct)
    {
        await AiEvalAccess.EnsureAsync(_currentUser, _hq, ct);
        return (await _repo.ListRunsAsync(30, ct)).Select(AiEvalMapping.Run).ToList();
    }
}

public sealed record GetAiEvalRunQuery(Guid Id) : IRequest<AiEvalRunDetailDto>;

public sealed class GetAiEvalRunHandler : IRequestHandler<GetAiEvalRunQuery, AiEvalRunDetailDto>
{
    private readonly IAiEvaluationRepository _repo;
    private readonly ICurrentUser _currentUser;
    private readonly IHeadquartersAccessService? _hq;

    public GetAiEvalRunHandler(IAiEvaluationRepository repo, ICurrentUser currentUser, IHeadquartersAccessService? hq = null)
    {
        _repo = repo;
        _currentUser = currentUser;
        _hq = hq;
    }

    public async Task<AiEvalRunDetailDto> Handle(GetAiEvalRunQuery q, CancellationToken ct)
    {
        await AiEvalAccess.EnsureAsync(_currentUser, _hq, ct);
        var run = await _repo.FindRunAsync(q.Id, ct) ?? throw new NotFoundException("AiEvalRun", q.Id);
        return new AiEvalRunDetailDto(AiEvalMapping.Run(run), (await _repo.ListItemsAsync(run.Id, ct)).Select(AiEvalMapping.Item).ToList());
    }
}
