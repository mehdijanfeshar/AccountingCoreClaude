using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements.Approvals;
using Accounting.Application.FinancialStatements.Queries;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.TransitionFsRun;

public sealed class TransitionFsRunCommandHandler : IRequestHandler<TransitionFsRunCommand, FsRunState>
{
    private readonly IFsRunRepository _runRepository;
    private readonly IFsApprovalStepRepository _stepRepository;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly Periods.FsPeriodGuard _periodGuard;
    private readonly IFsNarrativeRepository _narratives;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public TransitionFsRunCommandHandler(
        IFsRunRepository runRepository,
        IFsApprovalStepRepository stepRepository,
        IFsUnitScopeProvider scopes,
        Periods.FsPeriodGuard periodGuard,
        IFsNarrativeRepository narratives,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _runRepository = runRepository;
        _stepRepository = stepRepository;
        _scopes = scopes;
        _periodGuard = periodGuard;
        _narratives = narratives;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    /// <summary>ح-۴ — اقدام‌های «تأیید» دور جاری بازبینی (پس از آخرین ارسال).</summary>
    internal static IReadOnlyList<FsRunActionDto> CurrentCycleApprovals(IReadOnlyList<FsRunActionDto> actions)
    {
        var ordered = actions.OrderBy(a => a.CreatedDate).ToList();
        var lastSubmit = ordered.FindLastIndex(a => a.Action == FsRunAction.Submit);
        return ordered.Skip(lastSubmit + 1).Where(a => a.Action == FsRunAction.Approve).ToList();
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
        int? stepNo = null;

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
                run.APPROVAL_STEP = 0;
                break;

            case FsRunAction.Approve:
                Require(from, FsRunState.InReview);
                var submitter = detail.Actions.OrderBy(a => a.CreatedDate).LastOrDefault(a => a.Action == FsRunAction.Submit)?.UserId;

                // ح-۴ — زنجیرهٔ مراحل؛ بدون مرحله = تأیید تک‌مرحله‌ای ۴۵-ه.
                var chain = FsApprovalChain.Resolve(
                    await _stepRepository.GetAllAsync(run.FRAMEWORK, cancellationToken),
                    await _scopes.GetAsync(request.VahedCode, cancellationToken),
                    run.FRAMEWORK);
                var step = run.APPROVAL_STEP < chain.Count ? chain[run.APPROVAL_STEP] : null;
                var reason = FsApprovalChain.CannotApprove(
                    step, user, run.ADDUSERID, submitter, CurrentCycleApprovals(detail.Actions).Select(a => a.UserId));

                if (reason is not null)
                {
                    throw new FsAccessDeniedException(reason);
                }

                if (step is not null)
                {
                    stepNo = run.APPROVAL_STEP + 1;
                    run.APPROVAL_STEP = stepNo.Value;
                }

                to = step is null || run.APPROVAL_STEP >= chain.Count ? FsRunState.Approved : FsRunState.InReview;
                break;

            case FsRunAction.Return:
                if (from is not (FsRunState.InReview or FsRunState.Approved))
                {
                    throw Conflict(from);
                }

                to = FsRunState.Draft;
                run.APPROVAL_STEP = 0;
                break;

            case FsRunAction.Publish:
                Require(from, FsRunState.Approved);

                if (user == run.ADDUSERID)
                {
                    throw new FsAccessDeniedException("تهیه‌کنندهٔ صورت‌ها نمی‌تواند همان اجرا را منتشر کند (تفکیک وظایف).");
                }

                // ح-۵ — V-11: همهٔ واحدهای دامنه باید دورهٔ قفل‌شده داشته باشند.
                var unlocked = await _periodGuard.GetUnlockedAsync(run.VAHEDCODE, run.INCLUDE_SUBUNITS, run.YEAR, cancellationToken);

                if (unlocked.Count > 0)
                {
                    throw new FsTemplateConflictException(
                        $"V-11: دورهٔ {unlocked.Count} واحد در دامنهٔ این صورت‌ها قفل نیست ({string.Join("، ", unlocked.Take(5))}{(unlocked.Count > 5 ? "، …" : string.Empty)}); "
                        + "از صفحهٔ «بستن دوره» دوره را ببندید و قفل کنید.");
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

                // ح-۶ — متن یادداشت‌های توضیحی هنگام انتشار ثابت می‌شود.
                if ((await _narratives.GetRunNarrativesAsync(run.ID, run.VAHEDCODE, cancellationToken)).Count == 0)
                {
                    var live = await _narratives.GetSetAsync(run.VAHEDCODE, run.FRAMEWORK, run.YEAR, false, cancellationToken);
                    await _narratives.AddRunNarrativesAsync(live.Select(n => new TB_FS_RUN_NARRATIVE
                    {
                        ID = Guid.NewGuid(),
                        RUN_ID = run.ID,
                        VAHEDCODE = run.VAHEDCODE,
                        NARRATIVE_ID = n.ID,
                        ORDER_NO = n.ORDER_NO,
                        TITLE_FA = n.TITLE_FA,
                        LINKED_TEMPLATE_CODE = n.LINKED_TEMPLATE_CODE,
                        CONTENT_JSON = n.CONTENT_JSON,
                        VERSION_NO = n.VERSION_NO,
                    }), cancellationToken);
                }

                to = FsRunState.Published;
                break;

            default:
                throw new FsTemplateConflictException("اقدام نامعتبر است.");
        }

        run.STATE = to;
        run.CHANGEUSERID = user;
        run.UPDATEDDATE = now;
        await _runRepository.AddActionAsync(NewAction(run, request.Action, from, to, user, request.Comments?.Trim(), now, stepNo), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return to;
    }

    private static TB_FS_RUN_ACTION NewAction(
        TB_FS_RUN run, FsRunAction action, FsRunState from, FsRunState to, string user, string? comments, DateTime now, int? stepNo = null) => new()
    {
        STEP_NO = stepNo,
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
