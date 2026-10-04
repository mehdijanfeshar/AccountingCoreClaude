using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.FinancialStatements.Queries;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.FinancialStatements.Collaboration;

// ح-۳ (docs/fs-module.md §۱۳) — «نظر» روی ردیف/کنترل/اجرا و ارجاع کنترل ناموفق به مسئول با مهلت (سند منبع
// §۱۰: «هر نتیجه ناموفق به مسئول واحد ارجاع، مهلت و گفت‌وگو دارد»؛ §۱۲-۳: «ثبت نظر روی ردیف»). اجرای
// واحد دیگر = ۴۰۴. Snapshot دست نمی‌خورد: نظرها جدول جدا دارند و ارجاع فقط ستون‌های ارجاعِ کنترل را عوض می‌کند.

/// <summary><c>GET api/fs/runs/{id}/comments</c></summary>
public sealed record GetFsRunCommentsQuery(Guid RunId) : IRequest<IReadOnlyList<FsRunCommentDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary><c>POST api/fs/runs/{id}/comments</c> — <paramref name="RowId"/> و <paramref name="CheckId"/> هر دو خالی = نظر روی کل اجرا.</summary>
public sealed record AddFsRunCommentCommand(Guid RunId, Guid? RowId, Guid? CheckId, string Body) : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary><c>POST api/fs/runs/{id}/comments/{commentId}/delete</c> — فقط نویسنده.</summary>
public sealed record DeleteFsRunCommentCommand(Guid RunId, Guid CommentId) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>
/// <c>POST api/fs/runs/{id}/checks/{checkId}/assign</c> — ارجاع کنترل <b>ناموفق</b> به کد کاربری مسئول با مهلت
/// (شمسی YYYYMMDD، اختیاری). ارجاع دوباره مسئول را عوض و وضعیت را «باز» می‌کند؛ هر ارجاع یک «نظر» هم ثبت می‌کند.
/// </summary>
public sealed record AssignFsRunCheckCommand(Guid RunId, Guid CheckId, string AssigneeUserId, string? AssigneeName, string? DueDate, string? Note)
    : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary><c>POST api/fs/runs/{id}/checks/{checkId}/resolve</c> — ارجاع «رفع‌شده»؛ رفع واقعی با تهیهٔ دوبارهٔ صورت‌هاست.</summary>
public sealed record ResolveFsRunCheckCommand(Guid RunId, Guid CheckId, string? Note) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class AddFsRunCommentCommandValidator : AbstractValidator<AddFsRunCommentCommand>
{
    public AddFsRunCommentCommandValidator()
    {
        RuleFor(x => x.RunId).NotEqual(Guid.Empty);
        RuleFor(x => x.Body).NotEmpty().WithMessage("متن نظر لازم است.").MaximumLength(2000);
    }
}

public sealed class AssignFsRunCheckCommandValidator : AbstractValidator<AssignFsRunCheckCommand>
{
    public AssignFsRunCheckCommandValidator()
    {
        RuleFor(x => x.AssigneeUserId).NotEmpty().WithMessage("کد کاربری مسئول لازم است.").MaximumLength(10);
        RuleFor(x => x.AssigneeName).MaximumLength(200);
        RuleFor(x => x.DueDate)
            .Matches("^1[34][0-9]{2}(0[1-9]|1[0-2])(0[1-9]|[12][0-9]|3[01])$").WithMessage("مهلت باید تاریخ شمسی هشت‌رقمی باشد.")
            .When(x => !string.IsNullOrEmpty(x.DueDate));
        RuleFor(x => x.Note).MaximumLength(1500);
    }
}

public sealed class ResolveFsRunCheckCommandValidator : AbstractValidator<ResolveFsRunCheckCommand>
{
    public ResolveFsRunCheckCommandValidator()
    {
        RuleFor(x => x.Note).MaximumLength(1500);
    }
}

public sealed class FsRunCollaborationHandlers :
    IRequestHandler<GetFsRunCommentsQuery, IReadOnlyList<FsRunCommentDto>>,
    IRequestHandler<AddFsRunCommentCommand, Guid>,
    IRequestHandler<DeleteFsRunCommentCommand>,
    IRequestHandler<AssignFsRunCheckCommand>,
    IRequestHandler<ResolveFsRunCheckCommand>
{
    private readonly IFsRunRepository _runs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public FsRunCollaborationHandlers(IFsRunRepository runs, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _runs = runs;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<FsRunCommentDto>> Handle(GetFsRunCommentsQuery request, CancellationToken cancellationToken)
    {
        await RequireRunAsync(request.RunId, request.VahedCode, cancellationToken);
        var user = _currentUser.UserId;

        return (await _runs.GetCommentsAsync(request.RunId, request.VahedCode, cancellationToken))
            .Select(c => new FsRunCommentDto(c.ID, c.ROW_ID, c.CHECK_ID, c.BODY, c.ADDUSERID, c.CREATEDDATE, c.ADDUSERID == user))
            .ToList();
    }

    public async Task<Guid> Handle(AddFsRunCommentCommand request, CancellationToken cancellationToken)
    {
        var run = await RequireRunAsync(request.RunId, request.VahedCode, cancellationToken);

        if (request.CheckId is { } checkId)
        {
            _ = await _runs.GetCheckForUpdateAsync(run.ID, checkId, request.VahedCode, cancellationToken)
                ?? throw new NotFoundException("FsRunCheck", checkId);
        }

        return await AddCommentAsync(run, request.RowId, request.CheckId, request.Body.Trim(), cancellationToken);
    }

    public async Task Handle(DeleteFsRunCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _runs.GetCommentForUpdateAsync(request.RunId, request.CommentId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("FsRunComment", request.CommentId);

        if (comment.ADDUSERID != _currentUser.UserId)
        {
            throw new FsAccessDeniedException("فقط نویسندهٔ نظر می‌تواند آن را حذف کند.");
        }

        comment.ISDELETED = true;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(AssignFsRunCheckCommand request, CancellationToken cancellationToken)
    {
        var run = await RequireRunAsync(request.RunId, request.VahedCode, cancellationToken);
        var check = await _runs.GetCheckForUpdateAsync(run.ID, request.CheckId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("FsRunCheck", request.CheckId);

        if (check.PASSED)
        {
            throw new FsTemplateConflictException("فقط کنترل ناموفق ارجاع می‌شود.");
        }

        var assignee = request.AssigneeUserId.Trim();
        check.ASSIGNEE_USERID = assignee;
        check.ASSIGNEE_NAME = string.IsNullOrWhiteSpace(request.AssigneeName) ? null : request.AssigneeName.Trim();
        check.DUE_DATE = string.IsNullOrEmpty(request.DueDate) ? null : request.DueDate;
        check.ASSIGN_STATE = FsCheckAssignState.Open;
        check.ASSIGNED_BY = _currentUser.UserId;

        var due = check.DUE_DATE is { } d ? $"، مهلت {d[..4]}/{d[4..6]}/{d[6..]}" : string.Empty;
        var body = $"ارجاع به {check.ASSIGNEE_NAME ?? assignee} ({assignee}){due}"
            + (string.IsNullOrWhiteSpace(request.Note) ? string.Empty : $": {request.Note.Trim()}");

        await AddCommentAsync(run, null, check.ID, body, cancellationToken);
    }

    public async Task Handle(ResolveFsRunCheckCommand request, CancellationToken cancellationToken)
    {
        var run = await RequireRunAsync(request.RunId, request.VahedCode, cancellationToken);
        var check = await _runs.GetCheckForUpdateAsync(run.ID, request.CheckId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("FsRunCheck", request.CheckId);

        if (check.ASSIGN_STATE != FsCheckAssignState.Open)
        {
            throw new FsTemplateConflictException("این کنترل ارجاع باز ندارد.");
        }

        check.ASSIGN_STATE = FsCheckAssignState.Resolved;
        var body = "رفع شد" + (string.IsNullOrWhiteSpace(request.Note) ? string.Empty : $": {request.Note.Trim()}");

        await AddCommentAsync(run, null, check.ID, body, cancellationToken);
    }

    private async Task<TB_FS_RUN> RequireRunAsync(Guid runId, string vahedCode, CancellationToken cancellationToken)
        => await _runs.GetForUpdateAsync(runId, vahedCode, cancellationToken) ?? throw new NotFoundException("FsRun", runId);

    private async Task<Guid> AddCommentAsync(TB_FS_RUN run, Guid? rowId, Guid? checkId, string body, CancellationToken cancellationToken)
    {
        var comment = new TB_FS_RUN_COMMENT
        {
            ID = Guid.NewGuid(),
            RUN_ID = run.ID,
            VAHEDCODE = run.VAHEDCODE,
            ROW_ID = rowId,
            CHECK_ID = checkId,
            BODY = body.Length > 2000 ? body[..2000] : body,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
        };

        await _runs.AddCommentAsync(comment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return comment.ID;
    }
}
