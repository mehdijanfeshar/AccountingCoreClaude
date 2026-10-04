using Accounting.Application.FinancialStatements.Access;
using System.Globalization;
using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.FinancialStatements.Narratives;

// ح-۶ (docs/fs-module.md §۱۳) — یادداشت‌های توضیحی متنی (سند منبع §۹): هر واحد برای هر مجموعه و سال فهرست
// یادداشت متنی دارد (ویرایشگر Tiptap، متغیرهای {{...}}، مسئول، وضعیت، تاریخچهٔ نسخه، انتقال از سال قبل).
// متن واحد هدر — تفکیک واحد مثل اجراها: یادداشت واحد دیگر = ۴۰۴.

public sealed record FsNarrativeDto(
    Guid Id,
    int OrderNo,
    string TitleFa,
    string? LinkedTemplateCode,
    string? ContentJson,
    FsNarrativeState State,
    string? ResponsibleUserId,
    string? ReviewComment,
    int VersionNo,
    string LastEditedBy,
    DateTime LastEditedDate);

public sealed record FsNarrativeVersionDto(int VersionNo, string TitleFa, string? ContentJson, string AddUserId, DateTime CreatedDate);

/// <summary>یادداشت متنی در نمایش یک اجرا. <paramref name="IsSnapshot"/> = کپی زمان انتشار (ثابت).</summary>
public sealed record FsRunNarrativeDto(Guid Id, int OrderNo, string TitleFa, string? LinkedTemplateCode, string? ContentJson, FsNarrativeState? State, bool IsSnapshot);

/// <summary><c>GET api/fs/narratives?framework=&amp;year=</c></summary>
public sealed record GetFsNarrativesQuery(FsFramework Framework, string Year) : IRequest<IReadOnlyList<FsNarrativeDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary><c>GET api/fs/narratives/{id}/versions</c></summary>
public sealed record GetFsNarrativeVersionsQuery(Guid Id) : IRequest<IReadOnlyList<FsNarrativeVersionDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary><c>GET api/fs/runs/{id}/narratives</c> — کپی انتشار، وگرنه متن جاری همان واحد/مجموعه/سال.</summary>
public sealed record GetFsRunNarrativesQuery(Guid RunId) : IRequest<IReadOnlyList<FsRunNarrativeDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary><c>POST api/fs/narratives</c> — یادداشت تازه در انتهای فهرست.</summary>
[FsRequires(FsOperation.Prepare)]
public sealed record CreateFsNarrativeCommand(FsFramework Framework, string Year, string TitleFa, string? LinkedTemplateCode, string? ResponsibleUserId)
    : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>
/// <c>POST api/fs/narratives/{id}/update</c> — ذخیرهٔ متن و مشخصات؛ هر ذخیره یک نسخهٔ تازه. فقط در «پیش‌نویس» یا
/// «نیازمند اصلاح» (در بازبینی/تأییدشده = ۴۰۹؛ اول برگشت دهید).
/// </summary>
[FsRequires(FsOperation.Prepare)]
public sealed record SaveFsNarrativeCommand(Guid Id, string TitleFa, string? LinkedTemplateCode, string? ResponsibleUserId, string? ContentJson)
    : IRequest<int>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary><c>POST api/fs/narratives/{id}/delete</c> — حذف نرم (نه در بازبینی/تأییدشده).</summary>
[FsRequires(FsOperation.Prepare)]
public sealed record DeleteFsNarrativeCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary><c>POST api/fs/narratives/reorder</c> — <paramref name="Ids"/> = همهٔ یادداشت‌های مجموعه به ترتیب تازه.</summary>
[FsRequires(FsOperation.Prepare)]
public sealed record ReorderFsNarrativesCommand(FsFramework Framework, string Year, IReadOnlyList<Guid> Ids) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public enum FsNarrativeAction
{
    Submit = 1,
    Approve = 2,
    Return = 3,
}

/// <summary>
/// <c>POST api/fs/narratives/{id}/transitions</c> — ارسال (پیش‌نویس/نیازمند اصلاح ⇒ در بازبینی)، تأیید (در بازبینی ⇒
/// تأییدشده؛ آخرین ویرایشگر نمی‌تواند)، برگشت با دلیل (در بازبینی/تأییدشده ⇒ نیازمند اصلاح).
/// </summary>
[FsRequires(FsOperation.Prepare)]
public sealed record TransitionFsNarrativeCommand(Guid Id, FsNarrativeAction Action, string? Comment) : IRequest<FsNarrativeState>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>
/// <c>POST api/fs/narratives/roll-forward</c> — کپی یادداشت‌های سال قبل همین واحد و مجموعه به سال <paramref name="Year"/>
/// (پیش‌نویس، نسخهٔ ۱). اگر سال مقصد یادداشت دارد ۴۰۹. پاسخ = تعداد.
/// </summary>
[FsRequires(FsOperation.Prepare)]
public sealed record RollForwardFsNarrativesCommand(FsFramework Framework, string Year) : IRequest<int>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

internal static class FsNarrativeRules
{
    public const string YearPattern = "^1[34][0-9]{2}$";

    public static void Fields<T>(
        AbstractValidator<T> v,
        System.Linq.Expressions.Expression<Func<T, string>> title,
        System.Linq.Expressions.Expression<Func<T, string?>> linked,
        System.Linq.Expressions.Expression<Func<T, string?>> responsible)
    {
        v.RuleFor(title).NotEmpty().WithMessage("عنوان یادداشت لازم است.").MaximumLength(500);
        v.RuleFor(linked).MaximumLength(50);
        v.RuleFor(responsible).MaximumLength(10);
    }

    public static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

public sealed class CreateFsNarrativeCommandValidator : AbstractValidator<CreateFsNarrativeCommand>
{
    public CreateFsNarrativeCommandValidator()
    {
        RuleFor(x => x.Framework).IsInEnum();
        RuleFor(x => x.Year).Matches(FsNarrativeRules.YearPattern).WithMessage("سال مالی چهاررقمی شمسی.");
        FsNarrativeRules.Fields(this, x => x.TitleFa, x => x.LinkedTemplateCode, x => x.ResponsibleUserId);
    }
}

public sealed class SaveFsNarrativeCommandValidator : AbstractValidator<SaveFsNarrativeCommand>
{
    public SaveFsNarrativeCommandValidator()
    {
        FsNarrativeRules.Fields(this, x => x.TitleFa, x => x.LinkedTemplateCode, x => x.ResponsibleUserId);
        RuleFor(x => x.ContentJson).MaximumLength(1_000_000);
    }
}

public sealed class TransitionFsNarrativeCommandValidator : AbstractValidator<TransitionFsNarrativeCommand>
{
    public TransitionFsNarrativeCommandValidator()
    {
        RuleFor(x => x.Action).IsInEnum();
        RuleFor(x => x.Comment).MaximumLength(1000);
        RuleFor(x => x.Comment).NotEmpty().When(x => x.Action == FsNarrativeAction.Return).WithMessage("دلیل برگشت لازم است.");
    }
}

public sealed class RollForwardFsNarrativesCommandValidator : AbstractValidator<RollForwardFsNarrativesCommand>
{
    public RollForwardFsNarrativesCommandValidator()
    {
        RuleFor(x => x.Framework).IsInEnum();
        RuleFor(x => x.Year).Matches(FsNarrativeRules.YearPattern).WithMessage("سال مالی چهاررقمی شمسی.");
    }
}

public sealed class FsNarrativeHandlers :
    IRequestHandler<GetFsNarrativesQuery, IReadOnlyList<FsNarrativeDto>>,
    IRequestHandler<GetFsNarrativeVersionsQuery, IReadOnlyList<FsNarrativeVersionDto>>,
    IRequestHandler<GetFsRunNarrativesQuery, IReadOnlyList<FsRunNarrativeDto>>,
    IRequestHandler<CreateFsNarrativeCommand, Guid>,
    IRequestHandler<SaveFsNarrativeCommand, int>,
    IRequestHandler<DeleteFsNarrativeCommand>,
    IRequestHandler<ReorderFsNarrativesCommand>,
    IRequestHandler<TransitionFsNarrativeCommand, FsNarrativeState>,
    IRequestHandler<RollForwardFsNarrativesCommand, int>
{
    private readonly IFsNarrativeRepository _repository;
    private readonly IFsRunRepository _runs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public FsNarrativeHandlers(IFsNarrativeRepository repository, IFsRunRepository runs, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _repository = repository;
        _runs = runs;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<FsNarrativeDto>> Handle(GetFsNarrativesQuery request, CancellationToken cancellationToken)
        => (await _repository.GetSetAsync(request.VahedCode, request.Framework, request.Year, false, cancellationToken)).Select(ToDto).ToList();

    public async Task<IReadOnlyList<FsNarrativeVersionDto>> Handle(GetFsNarrativeVersionsQuery request, CancellationToken cancellationToken)
    {
        await LoadAsync(request.Id, request.VahedCode, cancellationToken);
        return (await _repository.GetVersionsAsync(request.Id, cancellationToken))
            .Select(v => new FsNarrativeVersionDto(v.VERSION_NO, v.TITLE_FA, v.CONTENT_JSON, v.ADDUSERID, v.CREATEDDATE))
            .ToList();
    }

    public async Task<IReadOnlyList<FsRunNarrativeDto>> Handle(GetFsRunNarrativesQuery request, CancellationToken cancellationToken)
    {
        var run = await _runs.GetForUpdateAsync(request.RunId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("FsRun", request.RunId);
        var snapshot = await _repository.GetRunNarrativesAsync(run.ID, run.VAHEDCODE, cancellationToken);

        if (snapshot.Count > 0)
        {
            return snapshot.Select(n => new FsRunNarrativeDto(n.NARRATIVE_ID, n.ORDER_NO, n.TITLE_FA, n.LINKED_TEMPLATE_CODE, n.CONTENT_JSON, null, true)).ToList();
        }

        return (await _repository.GetSetAsync(run.VAHEDCODE, run.FRAMEWORK, run.YEAR, false, cancellationToken))
            .Select(n => new FsRunNarrativeDto(n.ID, n.ORDER_NO, n.TITLE_FA, n.LINKED_TEMPLATE_CODE, n.CONTENT_JSON, n.STATE, false))
            .ToList();
    }

    public async Task<Guid> Handle(CreateFsNarrativeCommand request, CancellationToken cancellationToken)
    {
        var set = await _repository.GetSetAsync(request.VahedCode, request.Framework, request.Year, false, cancellationToken);
        var now = DateTime.UtcNow;
        var narrative = new TB_FS_NARRATIVE
        {
            ID = Guid.NewGuid(),
            VAHEDCODE = request.VahedCode,
            FRAMEWORK = request.Framework,
            YEAR = request.Year,
            ORDER_NO = set.Count == 0 ? 10 : set.Max(n => n.ORDER_NO) + 10,
            TITLE_FA = request.TitleFa.Trim(),
            LINKED_TEMPLATE_CODE = FsNarrativeRules.Trim(request.LinkedTemplateCode),
            STATE = FsNarrativeState.Draft,
            RESPONSIBLE_USERID = FsNarrativeRules.Trim(request.ResponsibleUserId),
            VERSION_NO = 1,
            CREATEDDATE = now,
            ADDUSERID = _currentUser.UserId,
        };

        await _repository.AddAsync(narrative, cancellationToken);
        await _repository.AddVersionAsync(NewVersion(narrative, now), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return narrative.ID;
    }

    public async Task<int> Handle(SaveFsNarrativeCommand request, CancellationToken cancellationToken)
    {
        var n = await LoadAsync(request.Id, request.VahedCode, cancellationToken);
        EnsureEditable(n);

        var now = DateTime.UtcNow;
        n.TITLE_FA = request.TitleFa.Trim();
        n.LINKED_TEMPLATE_CODE = FsNarrativeRules.Trim(request.LinkedTemplateCode);
        n.RESPONSIBLE_USERID = FsNarrativeRules.Trim(request.ResponsibleUserId);
        n.CONTENT_JSON = string.IsNullOrWhiteSpace(request.ContentJson) ? null : request.ContentJson;
        n.VERSION_NO++;
        n.CHANGEUSERID = _currentUser.UserId;
        n.UPDATEDDATE = now;

        await _repository.AddVersionAsync(NewVersion(n, now), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return n.VERSION_NO;
    }

    public async Task Handle(DeleteFsNarrativeCommand request, CancellationToken cancellationToken)
    {
        var n = await LoadAsync(request.Id, request.VahedCode, cancellationToken);
        EnsureEditable(n);

        n.ISDELETED = true;
        n.CHANGEUSERID = _currentUser.UserId;
        n.UPDATEDDATE = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(ReorderFsNarrativesCommand request, CancellationToken cancellationToken)
    {
        var set = await _repository.GetSetAsync(request.VahedCode, request.Framework, request.Year, true, cancellationToken);
        var byId = set.ToDictionary(n => n.ID);

        if (request.Ids.Count != set.Count || request.Ids.Distinct().Count() != set.Count || request.Ids.Any(id => !byId.ContainsKey(id)))
        {
            throw new FsTemplateConflictException("فهرست ترتیب باید دقیقاً همهٔ یادداشت‌های این مجموعه را داشته باشد.");
        }

        for (var i = 0; i < request.Ids.Count; i++)
        {
            byId[request.Ids[i]].ORDER_NO = (i + 1) * 10;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<FsNarrativeState> Handle(TransitionFsNarrativeCommand request, CancellationToken cancellationToken)
    {
        var n = await LoadAsync(request.Id, request.VahedCode, cancellationToken);
        var user = _currentUser.UserId;

        switch (request.Action)
        {
            case FsNarrativeAction.Submit when n.STATE is FsNarrativeState.Draft or FsNarrativeState.NeedsRevision:
                n.STATE = FsNarrativeState.InReview;
                n.REVIEW_COMMENT = null;
                break;

            case FsNarrativeAction.Approve when n.STATE == FsNarrativeState.InReview:
                if ((n.CHANGEUSERID ?? n.ADDUSERID) == user)
                {
                    throw new FsAccessDeniedException("آخرین ویرایشگر متن نمی‌تواند همان یادداشت را تأیید کند.");
                }

                n.STATE = FsNarrativeState.Approved;
                break;

            case FsNarrativeAction.Return when n.STATE is FsNarrativeState.InReview or FsNarrativeState.Approved:
                n.STATE = FsNarrativeState.NeedsRevision;
                n.REVIEW_COMMENT = request.Comment?.Trim();
                break;

            default:
                throw new FsTemplateConflictException("این اقدام در وضعیت فعلی یادداشت ممکن نیست.");
        }

        n.UPDATEDDATE = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return n.STATE;
    }

    public async Task<int> Handle(RollForwardFsNarrativesCommand request, CancellationToken cancellationToken)
    {
        var target = await _repository.GetSetAsync(request.VahedCode, request.Framework, request.Year, false, cancellationToken);

        if (target.Count > 0)
        {
            throw new FsTemplateConflictException("سال مقصد از قبل یادداشت توضیحی دارد؛ انتقال فقط به سال خالی ممکن است.");
        }

        var prior = (int.Parse(request.Year, CultureInfo.InvariantCulture) - 1).ToString(CultureInfo.InvariantCulture);
        var source = await _repository.GetSetAsync(request.VahedCode, request.Framework, prior, false, cancellationToken);

        if (source.Count == 0)
        {
            throw new FsTemplateConflictException($"سال {prior} برای این مجموعه یادداشت توضیحی ندارد.");
        }

        var now = DateTime.UtcNow;

        foreach (var s in source)
        {
            var copy = new TB_FS_NARRATIVE
            {
                ID = Guid.NewGuid(),
                VAHEDCODE = request.VahedCode,
                FRAMEWORK = request.Framework,
                YEAR = request.Year,
                ORDER_NO = s.ORDER_NO,
                TITLE_FA = s.TITLE_FA,
                LINKED_TEMPLATE_CODE = s.LINKED_TEMPLATE_CODE,
                CONTENT_JSON = s.CONTENT_JSON,
                STATE = FsNarrativeState.Draft,
                RESPONSIBLE_USERID = s.RESPONSIBLE_USERID,
                VERSION_NO = 1,
                CREATEDDATE = now,
                ADDUSERID = _currentUser.UserId,
            };

            await _repository.AddAsync(copy, cancellationToken);
            await _repository.AddVersionAsync(NewVersion(copy, now), cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return source.Count;
    }

    private async Task<TB_FS_NARRATIVE> LoadAsync(Guid id, string vahedCode, CancellationToken cancellationToken)
    {
        var n = await _repository.GetForUpdateAsync(id, cancellationToken);
        return n is not null && n.VAHEDCODE == vahedCode ? n : throw new NotFoundException("FsNarrative", id);
    }

    private static void EnsureEditable(TB_FS_NARRATIVE n)
    {
        if (n.STATE is FsNarrativeState.InReview or FsNarrativeState.Approved)
        {
            throw new FsTemplateConflictException("یادداشت در بازبینی یا تأییدشده است؛ برای تغییر، اول آن را برگشت دهید.");
        }
    }

    private TB_FS_NARRATIVE_VERSION NewVersion(TB_FS_NARRATIVE n, DateTime now) => new()
    {
        ID = Guid.NewGuid(),
        NARRATIVE_ID = n.ID,
        VERSION_NO = n.VERSION_NO,
        TITLE_FA = n.TITLE_FA,
        CONTENT_JSON = n.CONTENT_JSON,
        ADDUSERID = _currentUser.UserId,
        CREATEDDATE = now,
    };

    private static FsNarrativeDto ToDto(TB_FS_NARRATIVE n) => new(
        n.ID, n.ORDER_NO, n.TITLE_FA, n.LINKED_TEMPLATE_CODE, n.CONTENT_JSON, n.STATE, n.RESPONSIBLE_USERID, n.REVIEW_COMMENT,
        n.VERSION_NO, n.CHANGEUSERID ?? n.ADDUSERID, n.UPDATEDDATE ?? n.CREATEDDATE);
}

/// <summary><c>GET api/fs/runs/{id}/narratives.docx?divisor=</c> — فایل Word یادداشت‌های اجرا.</summary>
public sealed record GetFsRunNarrativesDocxQuery(Guid RunId, decimal Divisor) : IRequest<Queries.FsFileDto?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class GetFsRunNarrativesDocxQueryHandler : IRequestHandler<GetFsRunNarrativesDocxQuery, Queries.FsFileDto?>
{
    private readonly IFsRunRepository _runs;
    private readonly IMediator _mediator;
    private readonly IFsDocxExporter _exporter;

    public GetFsRunNarrativesDocxQueryHandler(IFsRunRepository runs, IMediator mediator, IFsDocxExporter exporter)
    {
        _runs = runs;
        _mediator = mediator;
        _exporter = exporter;
    }

    public async Task<Queries.FsFileDto?> Handle(GetFsRunNarrativesDocxQuery request, CancellationToken cancellationToken)
    {
        var detail = await _runs.GetDetailAsync(request.RunId, request.VahedCode, cancellationToken);

        if (detail is null)
        {
            return null;
        }

        var narratives = await _mediator.Send(new GetFsRunNarrativesQuery(request.RunId) { VahedCode = request.VahedCode }, cancellationToken);
        var divisor = request.Divisor is 1 or 1_000 or 1_000_000 or 1_000_000_000 ? request.Divisor : 1_000_000;
        var unitLabel = divisor switch
        {
            1 => "ریال",
            1_000 => "هزار ریال",
            1_000_000 => "میلیون ریال",
            _ => "میلیارد ریال",
        };

        return new Queries.FsFileDto(
            $"FS-{detail.Run.RunNo}-notes.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _exporter.ExportNarratives(detail, narratives, divisor, unitLabel));
    }
}
