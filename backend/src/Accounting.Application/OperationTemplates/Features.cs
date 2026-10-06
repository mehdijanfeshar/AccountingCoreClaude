using System.Text.Json;
using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.OperationTemplates;
using MediatR;

namespace Accounting.Application.OperationTemplates;

// نقش‌ها: ماژول «OperationTemplates» در RoleAuthorizationBehavior — Queryها برای همهٔ نقش‌ها،
// Commandها برای OperationWriters. تعریف الگو علاوه بر آن فقط «مدیر ستاد» (چک در هندلر).

// ═══════════════════════ Preview (Query) ═══════════════════════
// بدون هیچ نوشتنی؛ فرانت یا Agent بارها می‌تواند صدایش بزند.

public sealed record PreviewOperationQuery(
    Guid TemplateId,
    DateOnly? VoucherDate,
    Dictionary<string, string?> Values) : IRequest<OperationResult>, IVahedScopedQuery
{
    /// <summary>از هدر X-Vahed-Code / واحد کاربر، به دست VahedScopeBehavior — هرگز از بدنه.</summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;

    /// <summary>سال مالی جاری کاربر — از query (<c>?year=</c>)، مثل بقیهٔ API؛ هرگز از بدنه.</summary>
    [JsonIgnore]
    public string? Year { get; set; }
}

public sealed record OperationResult(
    bool Success,
    VoucherDraft? Draft,
    IReadOnlyList<EngineError> Errors,
    Guid? VoucherId = null,
    string? VoucherNo = null,
    bool WasAlreadyExecuted = false);

public sealed class PreviewOperationHandler : IRequestHandler<PreviewOperationQuery, OperationResult>
{
    private readonly IOperationTemplateRepository _repo;
    private readonly IVoucherGenerationEngine _engine;
    private readonly AssistantUnitPolicy _policy;

    public PreviewOperationHandler(IOperationTemplateRepository repo, IVoucherGenerationEngine engine, AssistantUnitPolicy policy)
    { _repo = repo; _engine = engine; _policy = policy; }

    public async Task<OperationResult> Handle(PreviewOperationQuery q, CancellationToken ct)
    {
        var (template, error) = await OperationGuards.LoadAsync(_repo, q.VahedCode, q.TemplateId, ct);
        if (error is not null) return new(false, null, new[] { error });

        var values = InputNormalizer.NormalizeAll(template!, q.Values);
        var date = TemplateVoucherDate.Resolve(template!, values, q.VoucherDate, Today());
        if (FiscalYearGuard.Check(date, q.Year) is { } yearError) return new(false, null, new[] { yearError });
        if (await _policy.TemplateNotAllowedAsync(template!, q.VahedCode, ct) is { } notAllowed) return new(false, null, new[] { notAllowed });
        if (await _policy.PeriodLockedAsync(q.VahedCode, date, ct) is { } locked) return new(false, null, new[] { locked });
        var r = await _engine.GenerateAsync(template!, q.VahedCode, date, values, ct);
        return new(r.IsSuccess, r.Draft, r.Errors);
    }

    internal static DateOnly Today() => DateOnly.FromDateTime(DateTime.Now);
}

// ═══════════════════════ Execute (Command) ═══════════════════════
// پیش‌نمایشِ ارسالی فرانت هرگز قبول نمی‌شود: سند دوباره از الگو ساخته می‌شود.

/// <summary>
/// اطلاعاتی که فقط هنگام «ثبت» برای یک ردیف پیش‌نویس الگو گرفته می‌شود: چک یا چک صوری (ردیف بانک بستانکار)،
/// فیش (ردیف بانک بدهکار)، شناسه و ویژگی. <see cref="LineIndex"/> = ترتیب ردیف در پیش‌نمایش موتور.
/// </summary>
public sealed record DraftLineExtrasInput(
    int LineIndex,
    Guid? CheckId = null,
    Vouchers.Commands.Common.VoucherChequeInfoInput? Cheque = null,
    Vouchers.Commands.Common.VoucherLineExtrasInput? Extras = null);

public sealed record ExecuteOperationCommand(
    Guid TemplateId,
    DateOnly? VoucherDate,
    Dictionary<string, string?> Values,
    Guid ClientRequestId,
    IReadOnlyList<DraftLineExtrasInput>? LineExtras = null) : IRequest<OperationResult>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;

    /// <summary>سال مالی جاری کاربر — از query (<c>?year=</c>)، مثل بقیهٔ API؛ هرگز از بدنه.</summary>
    [JsonIgnore]
    public string? Year { get; set; }
}

public sealed class ExecuteOperationHandler : IRequestHandler<ExecuteOperationCommand, OperationResult>
{
    private readonly IOperationTemplateRepository _repo;
    private readonly IVoucherGenerationEngine _engine;
    private readonly ComposedVoucherBuilder _composer;
    private readonly AssistantUnitPolicy _policy;
    private readonly IVoucherWriter _writer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ExecuteOperationHandler(IOperationTemplateRepository repo, IVoucherGenerationEngine engine,
        ComposedVoucherBuilder composer, IVoucherWriter writer, IUnitOfWork unitOfWork, ICurrentUser currentUser, AssistantUnitPolicy policy)
    { _repo = repo; _engine = engine; _composer = composer; _writer = writer; _unitOfWork = unitOfWork; _currentUser = currentUser; _policy = policy; }

    public async Task<OperationResult> Handle(ExecuteOperationCommand c, CancellationToken ct)
    {
        if (c.ClientRequestId == Guid.Empty)
            return new(false, null, new[] { new EngineError(EngineErrorCode.InvalidParameterValue,
                "شناسهٔ درخواست (ClientRequestId) الزامی است.") });

        // Idempotency: کلیک دوباره ⇐ همان سند قبلی
        var prior = await _repo.FindExecutionAsync(c.ClientRequestId, ct);
        if (prior is not null) return AlreadyExecuted(prior);

        var (template, error) = await OperationGuards.LoadAsync(_repo, c.VahedCode, c.TemplateId, ct);
        if (error is not null) return new(false, null, new[] { error });

        var values = InputNormalizer.NormalizeAll(template!, c.Values);
        var date = TemplateVoucherDate.Resolve(template!, values, c.VoucherDate, PreviewOperationHandler.Today());
        if (FiscalYearGuard.Check(date, c.Year) is { } yearError) return new(false, null, new[] { yearError });
        if (await _policy.TemplateNotAllowedAsync(template!, c.VahedCode, ct) is { } notAllowed) return new(false, null, new[] { notAllowed });
        if (await _policy.PeriodLockedAsync(c.VahedCode, date, ct) is { } locked) return new(false, null, new[] { locked });
        var r = await _engine.GenerateAsync(template!, c.VahedCode, date, values, ct);
        if (!r.IsSuccess) return new(false, null, r.Errors);

        // چک/فیش/شناسه/ویژگی که کاربر هنگام «ثبت» برای ردیف‌های پیش‌نویس وارد کرد — به ترتیب ردیف موتور.
        var draft = r.Draft!;
        if (c.LineExtras is { Count: > 0 } lineExtras)
        {
            var lines = draft.Lines.ToList();
            foreach (var x in lineExtras.Where(x => x.LineIndex >= 0 && x.LineIndex < lines.Count))
                lines[x.LineIndex] = lines[x.LineIndex] with { CheckId = x.CheckId, Cheque = x.Cheque, Extras = x.Extras };
            draft = draft with { Lines = lines };
        }
        var extrasErrors = await _composer.ValidateExtrasAsync(draft, ct);
        if (extrasErrors.Count > 0) return new(false, draft, extrasErrors);

        // writer فقط stage می‌کند؛ سند و ردپای اجرا با یک SaveChanges (یک تراکنش) ثبت می‌شوند.
        var (voucherId, voucherNo) = await _writer.CreateDraftVoucherAsync(draft, ct);

        await _repo.AddExecutionAsync(new OperationExecution
        {
            Id = Guid.NewGuid(),
            ClientRequestId = c.ClientRequestId,
            OperationTemplateId = template!.Id,
            TemplateCode = template.Code,
            VahedCode = c.VahedCode,
            InputJson = JsonSerializer.Serialize(values),
            VoucherId = voucherId,
            VoucherNo = voucherNo,
            Channel = "Form",
            CreatedBy = _currentUser.UserId,
            CreatedAtUtc = DateTime.UtcNow
        }, ct);

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

        return new(true, draft, Array.Empty<EngineError>(), voucherId, voucherNo);
    }

    private static OperationResult AlreadyExecuted(OperationExecution e) =>
        new(true, null, Array.Empty<EngineError>(), e.VoucherId, e.VoucherNo, WasAlreadyExecuted: true);
}

internal static class OperationGuards
{
    public static async Task<(OperationTemplate? T, EngineError? Error)> LoadAsync(
        IOperationTemplateRepository repo, string vahedCode, Guid templateId, CancellationToken ct)
    {
        // واحد فقط از توکن/هدر کاربر می‌آید، نه از ورودی درخواست
        if (string.IsNullOrWhiteSpace(vahedCode))
            return (null, new EngineError(EngineErrorCode.InvalidParameterValue,
                "کاربر به هیچ واحدی متصل نیست؛ ثبت عملیات فقط در سطح واحد ممکن است."));

        var t = await repo.GetFullAsync(templateId, ct);
        if (t is null)
            return (null, new EngineError(EngineErrorCode.TemplateNotFound, "الگوی عملیات یافت نشد."));
        return (t, null);
    }
}

// ═══════════════════════ List (Query) ═══════════════════════
// فرانت از همین خروجی فرم را پویا می‌سازد؛ Agent هم فهرست ابزارها را.

/// <summary>الگوهای فعال که برای نوع واحد کاربر مجازند.</summary>
public sealed record ListOperationTemplatesQuery : IRequest<IReadOnlyList<TemplateSummaryDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record TemplateSummaryDto(Guid Id, string Code, string Title, string Description,
    IReadOnlyList<TemplateParameterDto> Parameters, string? Keywords = null);

// PickerAccountId/PickerLevel فقط در خروجی فهرست: معین و سطح (LEVEL_CODE) اولین ردیفی که این پارامتر
// تفصیلی را مصرف می‌کند؛ فرانت با account-codes/{id}/tafsili-levels/{levelId}/items (همان قاعدهٔ B)
// فهرست انتخاب را می‌سازد.
public sealed record TemplateParameterDto(string Key, string Title, ParameterType Type,
    Guid? DetailGroupId, bool IsRequired, string AskPrompt, int SortOrder,
    Guid? PickerAccountId = null, int? PickerLevel = null);

public sealed class ListOperationTemplatesHandler
    : IRequestHandler<ListOperationTemplatesQuery, IReadOnlyList<TemplateSummaryDto>>
{
    private readonly IOperationTemplateRepository _repo;
    private readonly AssistantUnitPolicy _policy;

    public ListOperationTemplatesHandler(IOperationTemplateRepository repo, AssistantUnitPolicy policy)
    { _repo = repo; _policy = policy; }

    public async Task<IReadOnlyList<TemplateSummaryDto>> Handle(ListOperationTemplatesQuery q, CancellationToken ct)
    {
        var list = await _policy.FilterAsync(await _repo.ListActiveWithParametersAsync(ct), q.VahedCode, ct);
        return list.Select(t => new TemplateSummaryDto(t.Id, t.Code, t.Title, t.Description,
            t.Parameters.OrderBy(p => p.SortOrder)
             .Select(p =>
             {
                 var binding = p.Type == ParameterType.Detail ? FindBinding(t, p.Key) : null;
                 return new TemplateParameterDto(p.Key, p.Title, p.Type, p.DetailGroupId,
                     p.IsRequired, p.AskPrompt, p.SortOrder, binding?.AccountId, binding?.Level);
             }).ToList(), t.Keywords)).ToList();
    }

    private static (Guid AccountId, int Level)? FindBinding(OperationTemplate t, string key)
    {
        foreach (var line in t.Lines.OrderBy(l => l.SortOrder))
            foreach (var d in line.Details)
                if (string.Equals(d.ParameterKey, key, StringComparison.OrdinalIgnoreCase))
                    return (line.SubsidiaryAccountId, d.Level);
        return null;
    }
}

// ═══════════════════════ Create (Command) — فقط ستاد ═══════════════════════

public sealed record CreateOperationTemplateCommand(
    string Code, string Title, string Description, string VoucherDescriptionPattern,
    List<TemplateParameterDto> Parameters,
    List<TemplateLineDto> Lines,
    Guid? SystemTypeId = null,
    string? Keywords = null,
    IReadOnlyList<string>? AllowedVahedTypes = null) : IRequest<CreateTemplateResult>;

public sealed record TemplateLineDto(LineSide Side, Guid SubsidiaryAccountId, string? AmountParameterKey,
    decimal Percent, bool IsBalancingLine, string? DescriptionPattern, int SortOrder,
    List<TemplateLineDetailDto> Details);

public sealed record TemplateLineDetailDto(int Level, string? ParameterKey, Guid? FixedDetailId);

public sealed record CreateTemplateResult(bool Success, Guid? Id, IReadOnlyList<string> Errors);

public sealed class CreateOperationTemplateHandler
    : IRequestHandler<CreateOperationTemplateCommand, CreateTemplateResult>
{
    private readonly IOperationTemplateRepository _repo;
    private readonly TemplateDefinitionValidator _validator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateOperationTemplateHandler(IOperationTemplateRepository repo,
        TemplateDefinitionValidator validator, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    { _repo = repo; _validator = validator; _unitOfWork = unitOfWork; _currentUser = currentUser; }

    public async Task<CreateTemplateResult> Handle(CreateOperationTemplateCommand c, CancellationToken ct)
    {
        // RoleAuthorizationBehavior فقط OperationWriters را گذرانده؛ الگو سراسری است ⇒ فقط مدیر ستاد.
        if (!_currentUser.IsInRole(AppRoles.SetadAdmin))
            throw new RoleAccessDeniedException("تعریف الگوی عملیات فقط با نقش «مدیر ستاد» ممکن است.");

        var t = TemplateMapping.Build(Guid.NewGuid(), c.Code, c.Title, c.Description, c.VoucherDescriptionPattern,
            c.Parameters, c.Lines, c.SystemTypeId, c.Keywords, c.AllowedVahedTypes);
        t.CreatedBy = _currentUser.UserId;
        t.CreatedAtUtc = DateTime.UtcNow;

        var errors = (await _validator.ValidateAsync(t, ct)).ToList();
        if (errors.Count == 0 && await _repo.CodeExistsAsync(t.Code, ct))
            errors.Add($"کد الگو «{t.Code}» قبلاً ثبت شده است.");
        if (errors.Count > 0) return new(false, null, errors);

        await _repo.AddAsync(t, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return new(true, t.Id, Array.Empty<string>());
    }
}
