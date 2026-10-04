using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements.Approvals;
using Accounting.Application.FinancialStatements.Commands.TransitionFsRun;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.FinancialStatements.Queries.GetFsRun;

public sealed class GetFsRunQueryHandler : IRequestHandler<GetFsRunQuery, FsRunDetailDto?>
{
    private readonly IFsRunRepository _runRepository;
    private readonly IFsApprovalStepRepository _stepRepository;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly Periods.FsPeriodGuard _periodGuard;
    private readonly ICurrentUser _currentUser;

    public GetFsRunQueryHandler(
        IFsRunRepository runRepository,
        IFsApprovalStepRepository stepRepository,
        IFsUnitScopeProvider scopes,
        Periods.FsPeriodGuard periodGuard,
        ICurrentUser currentUser)
    {
        _runRepository = runRepository;
        _stepRepository = stepRepository;
        _scopes = scopes;
        _periodGuard = periodGuard;
        _currentUser = currentUser;
    }

    public async Task<FsRunDetailDto?> Handle(GetFsRunQuery request, CancellationToken cancellationToken)
    {
        var detail = await _runRepository.GetDetailAsync(request.Id, request.VahedCode, cancellationToken);

        if (detail is null)
        {
            return null;
        }

        // ح-۵ — V-11 برای نوار گردش: واحدهای دامنه با دورهٔ قفل‌نشده (فقط وقتی انتشار در پیش است).
        if (detail.Run.State == FsRunState.Approved)
        {
            detail = detail with { UnlockedUnits = await _periodGuard.GetUnlockedAsync(detail.Run.VahedCode, detail.Run.IncludeSubUnits, detail.Run.Year, cancellationToken) };
        }

        // ح-۴ — پیشرفت مراحل گردش تأیید (بدون مرحلهٔ تعریف‌شده = گردش تک‌مرحله‌ای، فهرست خالی).
        var run = detail.Run;
        var chain = FsApprovalChain.Resolve(
            await _stepRepository.GetAllAsync(run.Framework, cancellationToken),
            await _scopes.GetAsync(request.VahedCode, cancellationToken),
            run.Framework);

        if (chain.Count == 0)
        {
            return detail with { ApprovalSteps = [] };
        }

        var cycle = TransitionFsRunCommandHandler.CurrentCycleApprovals(detail.Actions);
        var byStep = cycle.Where(a => a.StepNo is not null).GroupBy(a => a.StepNo!.Value).ToDictionary(g => g.Key, g => g.Last());
        var submitter = detail.Actions.OrderBy(a => a.CreatedDate).LastOrDefault(a => a.Action == FsRunAction.Submit)?.UserId;
        var inReview = run.State == FsRunState.InReview;
        var nextStep = byStep.Count + 1;

        var steps = chain.Select((s, i) =>
        {
            var no = i + 1;
            var approved = byStep.GetValueOrDefault(no);
            var isCurrent = inReview && no == nextStep;
            var reason = isCurrent
                ? FsApprovalChain.CannotApprove(s, _currentUser.UserId, run.AddUserId, submitter, cycle.Select(a => a.UserId))
                : null;

            return new FsRunApprovalStepDto(
                no,
                s.TITLE_FA,
                FsApprovalChain.Approvers(s.APPROVER_USERIDS),
                approved?.UserId,
                approved?.CreatedDate,
                isCurrent,
                isCurrent && reason is null,
                reason);
        }).ToList();

        return detail with { ApprovalSteps = steps };
    }
}
