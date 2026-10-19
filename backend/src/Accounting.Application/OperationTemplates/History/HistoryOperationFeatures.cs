using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Accounting.Application.Commands.Vouchers.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.OperationTemplates;
using FluentValidation;
using MediatR;

namespace Accounting.Application.OperationTemplates.History;

// ═══════════════════ پیش‌نمایش و ثبت از روی سابقه ═══════════════════
// سرور الگوی موقت را از روی سابقهٔ ذخیره‌شده و تعریف فعلی کدینگ بازمی‌سازد؛ شناسهٔ حساب هرگز از کلاینت نمی‌آید.
// همان موتور، همان گاردها (سال مالی، قفل دوره، ماتریس دسترسی کدینگ، قاعدهٔ B، گارد سطح تفصیلی در VoucherWriter)،
// همان Idempotency و تصویر الگو؛ فقط ریسک «بالا» اجباری و ردیف اجرا بدون OperationTemplateId.

public sealed record PreviewHistoryOperationQuery(
    Guid PatternId,
    DateOnly? VoucherDate,
    Dictionary<string, string?> Values,
    string? Description = null,
    string? UserSentence = null,
    Dictionary<string, string?>? AnswerSources = null,
    bool AiConfirmed = false,
    bool HasAttachment = false,
    IReadOnlyList<DraftLineExtrasInput>? LineExtras = null) : IRequest<OperationResult>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;

    [JsonIgnore]
    public string? Year { get; set; }
}

public sealed record ExecuteHistoryOperationCommand(
    Guid PatternId,
    DateOnly? VoucherDate,
    Dictionary<string, string?> Values,
    Guid ClientRequestId,
    string? Description = null,
    string? UserSentence = null,
    Dictionary<string, string?>? AnswerSources = null,
    bool AiConfirmed = false,
    bool HasAttachment = false,
    Guid? AiTraceId = null,
    IReadOnlyList<DraftLineExtrasInput>? LineExtras = null) : IRequest<OperationResult>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;

    [JsonIgnore]
    public string? Year { get; set; }
}

public sealed record HistoryPlan(OperationResult? Failure, OperationTemplate? Template, Dictionary<string, string?> Values, VoucherDraft? Draft);

/// <summary>مراحل مشترک پیش‌نمایش و ثبت (تا دو مسیر از هم جدا نشوند).</summary>
public sealed class HistoryOperationPlanner
{
    private readonly IHistoryPatternStore _store;
    private readonly HistoryTemplateFactory _factory;
    private readonly AssistantUnitPolicy _policy;
    private readonly IVoucherGenerationEngine _engine;
    private readonly IAccountEntryPolicy _entryPolicy;

    public HistoryOperationPlanner(IHistoryPatternStore store, HistoryTemplateFactory factory, AssistantUnitPolicy policy,
        IVoucherGenerationEngine engine, IAccountEntryPolicy entryPolicy)
    { _store = store; _factory = factory; _policy = policy; _engine = engine; _entryPolicy = entryPolicy; }

    public async Task<HistoryPlan> PlanAsync(Guid patternId, string vahedCode, string? year, DateOnly? requestedDate,
        Dictionary<string, string?>? answers, string? description, string? userSentence, CancellationToken ct)
    {
        HistoryPlan Fail(EngineError e) => new(new OperationResult(false, null, [e]), null, new(), null);

        if (string.IsNullOrWhiteSpace(vahedCode))
            return Fail(new EngineError(EngineErrorCode.InvalidParameterValue,
                "کاربر به هیچ واحدی متصل نیست؛ ثبت عملیات فقط در سطح واحد ممکن است."));
        if (await _store.CountAsync(ct) == 0) throw new HistoryPatternsUnavailableException();

        var pattern = await _store.GetAsync(patternId, ct);
        if (pattern is null)
            return Fail(new EngineError(EngineErrorCode.TemplateNotFound, "سابقهٔ مورد نظر یافت نشد؛ شاید جدول سوابق دوباره ساخته شده است."));

        var built = await _factory.BuildAsync(pattern, new HistoryBuildContext(vahedCode, description, userSentence), ct);
        if (built.Template is not { } template)
            return new(new OperationResult(false, null,
                built.Problems.Select(p => new EngineError(EngineErrorCode.InvalidParameterValue, p)).ToList()), null, new(), null);

        var values = InputNormalizer.NormalizeAll(template, answers);
        var date = TemplateVoucherDate.Resolve(template, values, requestedDate, PreviewOperationHandler.Today());
        if (FiscalYearGuard.Check(date, year) is { } yearError) return Fail(yearError);
        if (await _policy.PeriodLockedAsync(vahedCode, date, ct) is { } locked) return Fail(locked);

        // ماتریس دسترسی کدینگ (پیش‌فرض ممنوع): BusinessRuleException ⇒ ProblemDetails، مثل ثبت دستی
        await _entryPolicy.EnsureManualEntryAllowedAsync(vahedCode, ToDateDoc(date),
            template.Lines.Select(l => (Guid?)l.SubsidiaryAccountId).ToList(), ct);

        var r = await _engine.GenerateAsync(template, vahedCode, date, values, ct);
        return r.IsSuccess
            ? new(null, template, values, r.Draft)
            : new(new OperationResult(false, null, r.Errors), template, values, null);
    }

    private static string ToDateDoc(DateOnly date)
    {
        var pc = new PersianCalendar();
        var d = date.ToDateTime(TimeOnly.MinValue);
        return $"{pc.GetYear(d):0000}{pc.GetMonth(d):00}{pc.GetDayOfMonth(d):00}";
    }

    /// <summary>
    /// ریسک همان ارزیاب قطعی، سپس «بالا» اجباری با دلیل ثابت؛ سند هرگز از صف حسابدار رد نمی‌شود
    /// (ثبت‌شده «موقت» و ریسک بالا ⇒ تأیید مدیر مالی هم لازم).
    /// </summary>
    /// <summary>چک/فیش/شناسه/ویژگی ردیف‌ها به ترتیب ردیف موتور (مثل ثبت الگو).</summary>
    public static VoucherDraft ApplyExtras(VoucherDraft draft, IReadOnlyList<DraftLineExtrasInput>? extras)
    {
        if (extras is not { Count: > 0 }) return draft;
        var lines = draft.Lines.ToList();
        foreach (var x in extras.Where(x => x.LineIndex >= 0 && x.LineIndex < lines.Count))
            lines[x.LineIndex] = lines[x.LineIndex] with { CheckId = x.CheckId, Cheque = x.Cheque, Extras = x.Extras };
        return draft with { Lines = lines };
    }

    public static async Task<RiskAssessment> ForceHighRiskAsync(OperationRiskEvaluator? risk, OperationTemplate template,
        string vahedCode, VoucherDraft draft, IReadOnlyDictionary<string, string?> values, bool hasAttachment,
        IReadOnlyDictionary<string, string?>? sources, bool aiConfirmed, CancellationToken ct)
    {
        var a = risk is null ? null
            : await risk.EvaluateAsync(new RiskRequest(template, vahedCode, draft, values, false, hasAttachment, sources, aiConfirmed), ct);

        var reasons = new List<string> { HistoryDefaults.RiskReason };
        reasons.AddRange((a?.Reasons ?? []).Where(r => r != HistoryDefaults.RiskReason));
        var warnings = new List<string> { HistoryDefaults.RiskWarning };
        warnings.AddRange((a?.Warnings ?? []).Where(w => w != HistoryDefaults.RiskWarning));
        var controls = a?.Controls ?? [new ControlDto("balanced", "دو طرف سند برابر است", draft.TotalDebit == draft.TotalCredit && draft.TotalDebit > 0)];
        var basis = a?.Basis ?? new OperationBasisDto(template.Title, 0);
        return new RiskAssessment(RiskLevel.High, reasons, warnings, controls, basis, draft.TotalDebit);
    }
}

public sealed class PreviewHistoryOperationHandler : IRequestHandler<PreviewHistoryOperationQuery, OperationResult>
{
    private readonly HistoryOperationPlanner _planner;
    private readonly OperationRiskEvaluator? _risk;

    private readonly ComposedVoucherBuilder _composer;

    public PreviewHistoryOperationHandler(HistoryOperationPlanner planner, ComposedVoucherBuilder composer, OperationRiskEvaluator? risk = null)
    { _planner = planner; _composer = composer; _risk = risk; }

    public async Task<OperationResult> Handle(PreviewHistoryOperationQuery q, CancellationToken ct)
    {
        var plan = await _planner.PlanAsync(q.PatternId, q.VahedCode, q.Year, q.VoucherDate, q.Values, q.Description, q.UserSentence, ct);
        if (plan.Failure is not null) return plan.Failure;

        var draft = HistoryOperationPlanner.ApplyExtras(plan.Draft!, q.LineExtras);
        var extrasErrors = await _composer.ValidateExtrasAsync(draft, ct);
        if (extrasErrors.Count > 0) return new(false, draft, extrasErrors);
        var a = await HistoryOperationPlanner.ForceHighRiskAsync(_risk, plan.Template!, q.VahedCode, draft, plan.Values,
            q.HasAttachment, q.AnswerSources, q.AiConfirmed, ct);
        return new(true, draft, Array.Empty<EngineError>(), PlainText: draft.PlainText ?? draft.Description,
            Warnings: a.Warnings, Risk: a.ToDto(), Controls: a.Controls, Basis: a.Basis);
    }
}

public sealed class ExecuteHistoryOperationHandler : IRequestHandler<ExecuteHistoryOperationCommand, OperationResult>
{
    private readonly IOperationTemplateRepository _repo;
    private readonly HistoryOperationPlanner _planner;
    private readonly ComposedVoucherBuilder _composer;
    private readonly IVoucherWriter _writer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IHesabyarAuditRepository? _audit;
    private readonly OperationRiskEvaluator? _risk;

    public ExecuteHistoryOperationHandler(IOperationTemplateRepository repo, HistoryOperationPlanner planner,
        ComposedVoucherBuilder composer, IVoucherWriter writer, IUnitOfWork unitOfWork, ICurrentUser currentUser,
        IHesabyarAuditRepository? audit = null, OperationRiskEvaluator? risk = null)
    { _repo = repo; _planner = planner; _composer = composer; _writer = writer; _unitOfWork = unitOfWork; _currentUser = currentUser; _audit = audit; _risk = risk; }

    public async Task<OperationResult> Handle(ExecuteHistoryOperationCommand c, CancellationToken ct)
    {
        if (c.ClientRequestId == Guid.Empty)
            return new(false, null, [new EngineError(EngineErrorCode.InvalidParameterValue, "شناسهٔ درخواست (ClientRequestId) الزامی است.")]);

        // Idempotency: کلیک دوباره ⇒ همان سند قبلی
        var prior = await _repo.FindExecutionAsync(c.ClientRequestId, ct);
        if (prior is not null) return AlreadyExecuted(prior);

        var plan = await _planner.PlanAsync(c.PatternId, c.VahedCode, c.Year, c.VoucherDate, c.Values, c.Description, c.UserSentence, ct);
        if (plan.Failure is not null) return plan.Failure;

        var template = plan.Template!;
        var draft = HistoryOperationPlanner.ApplyExtras(plan.Draft!, c.LineExtras);
        var extrasErrors = await _composer.ValidateExtrasAsync(draft, ct);
        if (extrasErrors.Count > 0) return new(false, draft, extrasErrors);

        var assessment = await HistoryOperationPlanner.ForceHighRiskAsync(_risk, template, c.VahedCode, draft, plan.Values,
            c.HasAttachment, c.AnswerSources, c.AiConfirmed, ct);

        var (voucherId, voucherNo) = await _writer.CreateDraftVoucherAsync(draft, ct);

        Guid? aiTraceId = null;
        if (c.AiTraceId is { } traceId && _audit is not null
            && await _audit.FindTraceAsync(traceId, ct) is { } trace
            && trace.VahedCode == c.VahedCode && trace.CreatedBy == _currentUser.UserId)
            aiTraceId = trace.Id;

        var (snapshotJson, snapshotHash) = TemplateSnapshot.Create(template);
        var execution = new OperationExecution
        {
            Id = Guid.NewGuid(),
            ClientRequestId = c.ClientRequestId,
            OperationTemplateId = null,
            TemplateCode = HistoryDefaults.TemplateCode(c.PatternId),
            VahedCode = c.VahedCode,
            InputJson = Truncate(JsonSerializer.Serialize(plan.Values), 4000),
            VoucherId = voucherId,
            VoucherNo = voucherNo,
            Channel = HistoryDefaults.Channel,
            CreatedBy = _currentUser.UserId,
            CreatedAtUtc = DateTime.UtcNow,
            TemplateSnapshot = snapshotJson,
            TemplateHash = snapshotHash,
            AiTraceId = aiTraceId,
        };
        ExecutionOrigin.Apply(execution, c.UserSentence ?? c.Description, c.AnswerSources, c.AiConfirmed, assessment, draft.TotalDebit);
        await _repo.AddExecutionAsync(execution, ct);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DuplicateKeyException)
        {
            // دو کلیک هم‌زمان: UK روی CLIENT_REQUEST_ID دومی را رد کرد و کل تراکنش (سند هم) برگشت.
            var winner = await _repo.FindExecutionAsync(c.ClientRequestId, ct);
            if (winner is null) throw;
            return AlreadyExecuted(winner);
        }

        return new(true, draft, Array.Empty<EngineError>(), voucherId, voucherNo,
            PlainText: draft.PlainText ?? draft.Description, Warnings: assessment.Warnings,
            Risk: assessment.ToDto(), Controls: assessment.Controls, Basis: assessment.Basis);
    }

    private static OperationResult AlreadyExecuted(OperationExecution e) =>
        new(true, null, Array.Empty<EngineError>(), e.VoucherId, e.VoucherNo, WasAlreadyExecuted: true);

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}

public sealed class PreviewHistoryOperationValidator : AbstractValidator<PreviewHistoryOperationQuery>
{
    public PreviewHistoryOperationValidator()
    {
        RuleFor(x => x.AnswerSources).Must(s => ExecutionOrigin.Validate(null, s) is null)
            .WithMessage(x => ExecutionOrigin.Validate(null, x.AnswerSources));
        RuleFor(x => x.Description).MaximumLength(250).WithMessage("شرح حداکثر ۲۵۰ نویسه است.");
    }
}

public sealed class ExecuteHistoryOperationValidator : AbstractValidator<ExecuteHistoryOperationCommand>
{
    public ExecuteHistoryOperationValidator()
    {
        RuleFor(x => x).Must(x => ExecutionOrigin.Validate(x.UserSentence, x.AnswerSources) is null)
            .WithMessage(x => ExecutionOrigin.Validate(x.UserSentence, x.AnswerSources));
        RuleFor(x => x.Description).MaximumLength(250).WithMessage("شرح حداکثر ۲۵۰ نویسه است.");
    }
}
