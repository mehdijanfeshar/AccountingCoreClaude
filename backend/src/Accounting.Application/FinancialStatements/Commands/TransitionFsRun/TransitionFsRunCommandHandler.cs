using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.TransitionFsRun;

public sealed class TransitionFsRunCommandHandler : IRequestHandler<TransitionFsRunCommand, FsRunState>
{
    private readonly IFsRunRepository _runRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public TransitionFsRunCommandHandler(IFsRunRepository runRepository, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _runRepository = runRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<FsRunState> Handle(TransitionFsRunCommand request, CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("FsRun", request.Id);
        var detail = await _runRepository.GetDetailAsync(request.Id, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("FsRun", request.Id);

        var user = _currentUser.UserId;
        var now = DateTime.UtcNow;
        var from = run.STATE;

        FsRunState to;

        switch (request.Action)
        {
            case FsRunAction.Submit:
                Require(from, FsRunState.Draft);

                if (run.USES_DRAFT)
                {
                    throw new FsTemplateConflictException("اجرای آزمایشی (با قالب پیش‌نویس) قابل ارسال نیست؛ قالب‌ها را فعال کنید و صورت‌ها را دوباره تهیه کنید.");
                }

                var blocking = detail.Checks.Where(c => c.Severity == FsCheckSeverity.Blocking && !c.Passed).ToList();

                if (blocking.Count > 0)
                {
                    throw new FsTemplateConflictException(
                        $"{blocking.Count} کنترل مسدودکننده ناموفق است ({string.Join("، ", blocking.Select(c => c.Code).Distinct())}); پیش از ارسال رفع کنید.");
                }

                to = FsRunState.InReview;
                break;

            case FsRunAction.Approve:
                Require(from, FsRunState.InReview);
                var submitter = detail.Actions.LastOrDefault(a => a.Action == FsRunAction.Submit)?.UserId;

                if (user == run.ADDUSERID || user == submitter)
                {
                    throw new FsAccessDeniedException("تهیه‌کننده یا ارسال‌کنندهٔ صورت‌ها نمی‌تواند همان اجرا را تأیید کند (تفکیک وظایف).");
                }

                to = FsRunState.Approved;
                break;

            case FsRunAction.Return:
                if (from is not (FsRunState.InReview or FsRunState.Approved))
                {
                    throw Conflict(from);
                }

                to = FsRunState.Draft;
                break;

            case FsRunAction.Publish:
                Require(from, FsRunState.Approved);

                if (user == run.ADDUSERID)
                {
                    throw new FsAccessDeniedException("تهیه‌کنندهٔ صورت‌ها نمی‌تواند همان اجرا را منتشر کند (تفکیک وظایف).");
                }

                foreach (var old in await _runRepository.GetPublishedForUpdateAsync(
                             run.VAHEDCODE, run.FRAMEWORK, run.YEAR, run.TO_MONTH, run.INCLUDE_SUBUNITS, cancellationToken))
                {
                    if (old.ID == run.ID)
                    {
                        continue;
                    }

                    old.STATE = FsRunState.Superseded;
                    old.CHANGEUSERID = user;
                    old.UPDATEDDATE = now;
                    await _runRepository.AddActionAsync(NewAction(old, FsRunAction.Supersede, FsRunState.Published, FsRunState.Superseded, user, $"جایگزین با انتشار اجرای شمارهٔ {run.RUN_NO}", now), cancellationToken);
                }

                to = FsRunState.Published;
                break;

            default:
                throw new FsTemplateConflictException("اقدام نامعتبر است.");
        }

        run.STATE = to;
        run.CHANGEUSERID = user;
        run.UPDATEDDATE = now;
        await _runRepository.AddActionAsync(NewAction(run, request.Action, from, to, user, request.Comments?.Trim(), now), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return to;
    }

    private static TB_FS_RUN_ACTION NewAction(TB_FS_RUN run, FsRunAction action, FsRunState from, FsRunState to, string user, string? comments, DateTime now) => new()
    {
        ID = Guid.NewGuid(),
        RUN_ID = run.ID,
        VAHEDCODE = run.VAHEDCODE,
        ACTION = action,
        FROM_STATE = from,
        TO_STATE = to,
        USERID = user,
        COMMENTS = string.IsNullOrWhiteSpace(comments) ? null : comments,
        CREATEDDATE = now,
    };

    private static void Require(FsRunState actual, FsRunState required)
    {
        if (actual != required)
        {
            throw Conflict(actual);
        }
    }

    private static FsTemplateConflictException Conflict(FsRunState state)
        => new($"اجرا در وضعیت «{Label(state)}» است و این اقدام روی آن ممکن نیست.");

    internal static string Label(FsRunState state) => state switch
    {
        FsRunState.Draft => "پیش‌نویس",
        FsRunState.InReview => "در بازبینی",
        FsRunState.Approved => "تأییدشده",
        FsRunState.Published => "منتشرشده",
        FsRunState.Superseded => "جایگزین‌شده",
        _ => "نامشخص",
    };
}
