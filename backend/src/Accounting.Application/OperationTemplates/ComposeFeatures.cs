using System.Text.Json;
using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.Vouchers.Commands.Common;
using Accounting.Domain.OperationTemplates;
using MediatR;

namespace Accounting.Application.OperationTemplates;

// ═══════════════════════ سند کامل (Compose) ═══════════════════════
// پیش‌نویسِ الگو (یا سند آزاد) که کاربر همهٔ فیلدهایش را دیده/ویرایش کرده است: سرآیند، پیوست،
// ردیف‌ها با معین، تفصیلی هر سطح، مبلغ، شرح و چک. سرور همه را دوباره اعتبارسنجی می‌کند (تراز،
// معین، سطوح الزامی، گروه و قاعدهٔ B تفصیلی) و از همان VoucherWriter، اتمیک با OperationExecution، ثبت می‌کند.
//
// کلید خطاها (EngineError.ParameterKey) مسیر فیلد است تا فرم/Agent دقیقاً همان فیلد را نشان دهد یا بپرسد:
//   voucherDate · description · apendix · lines · lines[i] · lines[i].accountId · lines[i].amount
//   lines[i].description · lines[i].tafsili.{levelId} · lines[i].cheque

public sealed record ComposedTafsiliInput(Guid LevelId, Guid TafsiliId);

public sealed record ComposedLineInput(
    Guid? AccountId,
    long Debit,
    long Credit,
    string? Description,
    IReadOnlyList<ComposedTafsiliInput>? Tafsili,
    Guid? CheckId = null,
    VoucherChequeInfoInput? Cheque = null,
    // شناسه/ویژگی/فیش ردیف — همان VoucherLineExtrasService فرم سند.
    VoucherLineExtrasInput? Extras = null);

public sealed record PreviewComposedVoucherQuery(
    DateOnly? VoucherDate,
    string? Description,
    string? Apendix,
    List<ComposedLineInput> Lines,
    Guid? SystemTypeId = null) : IRequest<OperationResult>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;

    /// <summary>سال مالی جاری کاربر — از query (<c>?year=</c>)، مثل بقیهٔ API؛ هرگز از بدنه.</summary>
    [JsonIgnore]
    public string? Year { get; set; }
}

public sealed record ExecuteComposedVoucherCommand(
    DateOnly? VoucherDate,
    string? Description,
    string? Apendix,
    List<ComposedLineInput> Lines,
    Guid? SourceTemplateId,
    Guid ClientRequestId,
    Guid? SystemTypeId = null) : IRequest<OperationResult>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;

    /// <summary>سال مالی جاری کاربر — از query (<c>?year=</c>)، مثل بقیهٔ API؛ هرگز از بدنه.</summary>
    [JsonIgnore]
    public string? Year { get; set; }
}

public sealed class PreviewComposedVoucherHandler : IRequestHandler<PreviewComposedVoucherQuery, OperationResult>
{
    private readonly ComposedVoucherBuilder _builder;
    public PreviewComposedVoucherHandler(ComposedVoucherBuilder builder) => _builder = builder;

    public async Task<OperationResult> Handle(PreviewComposedVoucherQuery q, CancellationToken ct)
    {
        var r = await _builder.BuildAsync(q.VahedCode, q.VoucherDate, q.Description, q.Apendix, q.Lines, "MANUAL", q.Year, q.SystemTypeId, ct);
        return new(r.IsSuccess, r.Draft, r.Errors);
    }
}

public sealed class ExecuteComposedVoucherHandler : IRequestHandler<ExecuteComposedVoucherCommand, OperationResult>
{
    private readonly ComposedVoucherBuilder _builder;
    private readonly IOperationTemplateRepository _repo;
    private readonly IVoucherWriter _writer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ExecuteComposedVoucherHandler(ComposedVoucherBuilder builder, IOperationTemplateRepository repo,
        IVoucherWriter writer, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    { _builder = builder; _repo = repo; _writer = writer; _unitOfWork = unitOfWork; _currentUser = currentUser; }

    public async Task<OperationResult> Handle(ExecuteComposedVoucherCommand c, CancellationToken ct)
    {
        if (c.ClientRequestId == Guid.Empty)
            return new(false, null, new[] { new EngineError(EngineErrorCode.InvalidParameterValue,
                "شناسهٔ درخواست (ClientRequestId) الزامی است.") });

        var prior = await _repo.FindExecutionAsync(c.ClientRequestId, ct);
        if (prior is not null)
            return new(true, null, Array.Empty<EngineError>(), prior.VoucherId, prior.VoucherNo, WasAlreadyExecuted: true);

        // الگوی مبدأ فقط برای ردپاست؛ محتوای سند همانی است که کاربر تأیید کرده و همین‌جا اعتبارسنجی می‌شود.
        OperationTemplate? source = null;
        if (c.SourceTemplateId is Guid sourceId)
        {
            source = await _repo.GetFullAsync(sourceId, ct);
            if (source is null)
                return new(false, null, new[] { new EngineError(EngineErrorCode.TemplateNotFound, "الگوی عملیات یافت نشد.") });
        }

        var templateCode = source?.Code ?? "MANUAL";
        var r = await _builder.BuildAsync(c.VahedCode, c.VoucherDate, c.Description, c.Apendix, c.Lines, templateCode, c.Year, c.SystemTypeId, ct);
        if (!r.IsSuccess) return new(false, null, r.Errors);

        var (voucherId, voucherNo) = await _writer.CreateDraftVoucherAsync(r.Draft!, ct);

        await _repo.AddExecutionAsync(new OperationExecution
        {
            Id = Guid.NewGuid(),
            ClientRequestId = c.ClientRequestId,
            OperationTemplateId = source?.Id,
            TemplateCode = templateCode,
            VahedCode = c.VahedCode,
            InputJson = Truncate(JsonSerializer.Serialize(new { c.VoucherDate, c.Description, c.Apendix, c.Lines }), 4000),
            VoucherId = voucherId,
            VoucherNo = voucherNo,
            Channel = "Compose",
            CreatedBy = _currentUser.UserId,
            CreatedAtUtc = DateTime.UtcNow
        }, ct);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DuplicateKeyException)
        {
            var winner = await _repo.FindExecutionAsync(c.ClientRequestId, ct);
            if (winner is null) throw;
            return new(true, null, Array.Empty<EngineError>(), winner.VoucherId, winner.VoucherNo, WasAlreadyExecuted: true);
        }

        return new(true, r.Draft, Array.Empty<EngineError>(), voucherId, voucherNo);
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}

/// <summary>اعتبارسنجی و ساخت <see cref="VoucherDraft"/> از سند کاملی که کاربر وارد/ویرایش کرده است.</summary>
public sealed class ComposedVoucherBuilder
{
    private readonly ISubsidiaryAccountReader _accounts;
    private readonly IDetailReader _details;
    private readonly IVoucherLineExtrasService _extras;
    private readonly AssistantUnitPolicy _policy;

    public ComposedVoucherBuilder(ISubsidiaryAccountReader accounts, IDetailReader details, IVoucherLineExtrasService extras, AssistantUnitPolicy policy)
    { _accounts = accounts; _details = details; _extras = extras; _policy = policy; }

    public async Task<EngineResult> BuildAsync(string vahedCode, DateOnly? voucherDate, string? description,
        string? apendix, IReadOnlyList<ComposedLineInput>? lines, string sourceCode, string? fiscalYear, Guid? systemTypeId, CancellationToken ct)
    {
        var e = new List<EngineError>();
        if (string.IsNullOrWhiteSpace(vahedCode))
            return EngineResult.Fail(new EngineError(EngineErrorCode.InvalidParameterValue,
                "کاربر به هیچ واحدی متصل نیست؛ ثبت سند فقط در سطح واحد ممکن است."));

        lines ??= Array.Empty<ComposedLineInput>();
        var desc = description?.Trim() ?? "";

        if (voucherDate is null)
            e.Add(Err(EngineErrorCode.MissingParameter, "تاریخ سند مشخص نشده است.", "voucherDate", "سند به چه تاریخی ثبت شود؟"));
        else if (FiscalYearGuard.Check(voucherDate.Value, fiscalYear) is { } yearError)
            e.Add(yearError);
        else if (await _policy.PeriodLockedAsync(vahedCode, voucherDate.Value, ct) is { } locked)
            e.Add(locked);
        if (desc.Length > 250) e.Add(Err(EngineErrorCode.InvalidParameterValue, "شرح سند حداکثر ۲۵۰ کاراکتر است.", "description"));
        if ((apendix?.Length ?? 0) > 800) e.Add(Err(EngineErrorCode.InvalidParameterValue, "پیوست حداکثر ۸۰۰ کاراکتر است.", "apendix"));
        if (lines.Count < 2)
            e.Add(Err(EngineErrorCode.MissingParameter, "سند حداقل دو ردیف (بدهکار و بستانکار) لازم دارد.", "lines", "ردیف بعدی سند چیست؟"));

        var usedCheques = new HashSet<Guid>();
        for (var i = 0; i < lines.Count; i++)
        {
            var l = lines[i];
            if (l.AccountId is null)
                e.Add(Err(EngineErrorCode.MissingParameter, $"ردیف {i + 1}: حساب معین انتخاب نشده است.", $"lines[{i}].accountId", "این ردیف به کدام حساب معین مربوط است؟"));
            if (l.Debit < 0 || l.Credit < 0 || (l.Debit > 0) == (l.Credit > 0))
                e.Add(Err(EngineErrorCode.NonPositiveAmount, $"ردیف {i + 1}: دقیقاً یکی از بدهکار یا بستانکار باید مبلغ مثبت داشته باشد.", $"lines[{i}].amount", "مبلغ این ردیف چقدر است و بدهکار است یا بستانکار؟"));
            if ((l.Description?.Length ?? 0) > 200)
                e.Add(Err(EngineErrorCode.InvalidParameterValue, $"ردیف {i + 1}: شرح حداکثر ۲۰۰ کاراکتر است.", $"lines[{i}].description"));
            if (l.CheckId is Guid cid && !usedCheques.Add(cid))
                e.Add(Err(EngineErrorCode.InvalidParameterValue, $"ردیف {i + 1}: این چک در ردیف دیگری از همین سند هم آمده است.", $"lines[{i}].cheque"));
            if ((l.Tafsili ?? []).GroupBy(t => t.LevelId).Any(g => g.Count() > 1))
                e.Add(Err(EngineErrorCode.InvalidParameterValue, $"ردیف {i + 1}: برای یک سطح بیش از یک تفصیلی آمده است.", $"lines[{i}]"));
        }
        if (e.Count > 0) return EngineResult.Fail(e);

        var accounts = await _accounts.GetByIdsAsync(lines.Select(l => l.AccountId!.Value).Distinct().ToList(), ct);
        var details = await _details.GetByIdsAsync(
            lines.SelectMany(l => l.Tafsili ?? []).Select(t => t.TafsiliId).Distinct().ToList(), vahedCode, ct);

        var draftLines = new List<VoucherDraftLine>();
        for (var i = 0; i < lines.Count; i++)
        {
            var l = lines[i];
            if (!accounts.TryGetValue(l.AccountId!.Value, out var acc))
            { e.Add(Err(EngineErrorCode.AccountNotFound, $"ردیف {i + 1}: حساب معین یافت نشد.", $"lines[{i}].accountId")); continue; }
            if (!acc.IsActive)
            { e.Add(Err(EngineErrorCode.AccountInactive, $"ردیف {i + 1}: معین «{acc.Title}» غیرفعال است.", $"lines[{i}].accountId")); continue; }

            var rules = acc.DetailLevels.ToDictionary(r => r.LevelId);
            var given = (l.Tafsili ?? []).ToDictionary(t => t.LevelId, t => t.TafsiliId);
            var draftDetails = new List<VoucherDraftDetail>();

            foreach (var rule in acc.DetailLevels.Where(r => r.IsRequired && !given.ContainsKey(r.LevelId)))
                e.Add(Err(EngineErrorCode.MissingRequiredDetailLevel,
                    $"ردیف {i + 1}: تفصیلی سطح {rule.Level} برای معین «{acc.Title}» الزامی است.",
                    $"lines[{i}].tafsili.{rule.LevelId}", $"تفصیلی سطح {rule.Level} ردیف {i + 1} کدام است؟"));

            foreach (var (levelId, tafsiliId) in given)
            {
                var key = $"lines[{i}].tafsili.{levelId}";
                if (!rules.TryGetValue(levelId, out var rule))
                { e.Add(Err(EngineErrorCode.DetailLevelNotDefined, $"ردیف {i + 1}: معین «{acc.Title}» این سطح تفصیلی را ندارد.", key)); continue; }
                if (!details.TryGetValue(tafsiliId, out var det))
                { e.Add(Err(EngineErrorCode.DetailNotFound, $"ردیف {i + 1}: تفصیلی انتخاب‌شده یافت نشد.", key)); continue; }
                if (!det.IsActive)
                    e.Add(Err(EngineErrorCode.DetailInactive, $"ردیف {i + 1}: «{det.Title}» غیرفعال است.", key));
                if (!det.IsVisibleToUnit)
                    e.Add(Err(EngineErrorCode.DetailNotInUnit, $"ردیف {i + 1}: «{det.Title}» متعلق به واحد شما نیست.", key));
                else if (!det.DetailGroupIds.Overlaps(rule.DetailGroupIds))
                    e.Add(Err(EngineErrorCode.DetailWrongGroup, $"ردیف {i + 1}: «{det.Title}» برای سطح {rule.Level} معین «{acc.Title}» مجاز نیست.", key));
                draftDetails.Add(new VoucherDraftDetail(rule.Level, levelId, det.Id, det.Title));
            }

            var lineDesc = string.IsNullOrWhiteSpace(l.Description) ? desc : l.Description.Trim();
            draftLines.Add(new VoucherDraftLine(acc.Id, acc.Title, draftDetails.OrderBy(d => d.Level).ToList(),
                l.Debit, l.Credit, lineDesc, acc.Code, l.CheckId, l.Cheque, l.Extras));
        }
        if (e.Count > 0) return EngineResult.Fail(e);

        var debit = draftLines.Sum(l => l.Debit);
        var credit = draftLines.Sum(l => l.Credit);
        if (debit != credit)
            return EngineResult.Fail(Err(EngineErrorCode.Unbalanced,
                $"سند تراز نیست: بدهکار {debit:N0} ≠ بستانکار {credit:N0} (اختلاف {Math.Abs(debit - credit):N0}).", "lines",
                "اختلاف بدهکار و بستانکار در کدام ردیف است؟"));

        var draft = new VoucherDraft(vahedCode, voucherDate!.Value, desc, sourceCode, draftLines, apendix?.Trim(), systemTypeId);
        var extrasErrors = await ValidateExtrasAsync(draft, ct);
        return extrasErrors.Count > 0 ? EngineResult.Fail(extrasErrors) : EngineResult.Ok(draft);
    }

    /// <summary>
    /// شناسهٔ حساب شناسه‌دار، ویژگی و فیش هر ردیف — کلید خطا <c>lines[i].extras</c> تا فرم/Agent همان ردیف را
    /// باز کند. هم مسیر «سند کامل» و هم اجرای الگو (پس از چسباندن اطلاعات تکمیلی به ردیف‌ها) از این استفاده می‌کنند.
    /// </summary>
    public async Task<List<EngineError>> ValidateExtrasAsync(VoucherDraft draft, CancellationToken ct)
    {
        var errors = new List<EngineError>();
        var year = FiscalYearGuard.JalaliYear(draft.VoucherDate);
        for (var i = 0; i < draft.Lines.Count; i++)
        {
            var line = draft.Lines[i];
            var messages = await _extras.ValidateAsync(line.SubsidiaryAccountId, line.Details.Select(d => d.DetailId).ToList(),
                line.Debit > 0, line.Extras, draft.VahedCode, year, ct);
            errors.AddRange(messages.Select(m => Err(EngineErrorCode.InvalidParameterValue, $"ردیف {i + 1}: {m}", $"lines[{i}].extras")));
        }
        return errors;
    }

    private static EngineError Err(EngineErrorCode code, string message, string key, string? ask = null) =>
        new(code, message, key, ask);
}
